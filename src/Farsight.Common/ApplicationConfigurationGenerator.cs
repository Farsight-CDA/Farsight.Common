using Farsight.Common.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Farsight.Common;

/// <summary>
/// Discovers Farsight configuration and singleton patterns and emits registration code.
/// </summary>
[Generator]
public class ApplicationConfigurationGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Configures the incremental pipelines used by this generator.
    /// </summary>
    /// <param name="context">The generator initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var configOptions = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is TypeDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .Collect();

        var generatedServices = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetGeneratedServiceSemanticTarget(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .Collect();

        var combined = configOptions.Combine(generatedServices);
        var generationInput = context.CompilationProvider.Combine(combined);

        context.RegisterSourceOutput(generationInput,
            static (spc, source) => Execute(source.Left, source.Right.Left, source.Right.Right, spc));
    }

    internal record struct ConfigOptionModel(
        string FullName,
        string? SectionName,
        bool ErrorOnUnknownConfiguration,
        bool BindNonPublicProperties,
        string? ValidatorFullName,
        string? ValidatorHelperTypeName,
        ImmutableArray<Diagnostic> Diagnostics
    );

    private static ConfigOptionModel? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var typeDeclarationSyntax = (TypeDeclarationSyntax) context.Node;
        if(context.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var attributeData = symbol
            .GetAttributes()
            .FirstOrDefault(a => TryGetConfigOptionAttribute(a, out _));

        if(attributeData is null)
        {
            return null;
        }

        string? sectionName = null;
        var sectionNameArg = attributeData.NamedArguments.FirstOrDefault(kvp => kvp.Key == SharedTypes.SECTION_NAME_PROPERTY);
        if(sectionNameArg.Value.Value is string s)
        {
            sectionName = s;
        }

        bool errorOnUnknownConfiguration = !String.IsNullOrWhiteSpace(sectionName);
        var errorOnUnknownConfigurationArg = attributeData.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == SharedTypes.ERROR_ON_UNKNOWN_CONFIGURATION_PROPERTY);
        if(errorOnUnknownConfigurationArg.Key is not null && errorOnUnknownConfigurationArg.Value.Value is bool value)
        {
            errorOnUnknownConfiguration = value;
        }

        bool bindNonPublicProperties = false;
        var bindNonPublicPropertiesArg = attributeData.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == SharedTypes.BIND_NON_PUBLIC_PROPERTIES_PROPERTY);
        if(bindNonPublicPropertiesArg.Key is not null && bindNonPublicPropertiesArg.Value.Value is bool bindNonPublicValue)
        {
            bindNonPublicProperties = bindNonPublicValue;
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        string? validatorFullName = null;
        string? validatorHelperTypeName = null;

        if(TryGetConfigOptionAttribute(attributeData, out var validatorType))
        {
            if(validatorType is not null)
            {
                validatorFullName = validatorType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                validatorHelperTypeName = BuildConfigOptionValidatorTypeName(symbol);

            }
        }

        return new ConfigOptionModel(
            symbol.ToDisplayString(),
            sectionName,
            errorOnUnknownConfiguration,
            bindNonPublicProperties,
            validatorFullName,
            validatorHelperTypeName,
            diagnostics.ToImmutable());
    }

    internal record struct InjectedFieldModel(string TypeFullName, string Name);
    internal enum GeneratedServiceKind
    {
        Singleton,
        Transient,
        Startup
    }

    internal record struct GeneratedServiceModel(
        INamedTypeSymbol TypeSymbol,
        ImmutableArray<InjectedFieldModel> InjectedFields,
        ImmutableArray<InjectedFieldModel> BaseInjectedFields,
        ImmutableArray<ITypeSymbol> ServiceTypes,
        ImmutableArray<Diagnostic> Diagnostics,
        bool IsAbstract,
        GeneratedServiceKind Kind
    );
    private static GeneratedServiceModel? GetGeneratedServiceSemanticTarget(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax) context.Node;
        if(context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var baseType = symbol.BaseType;
        bool isSingleton = false;
        bool isTransient = false;
        bool isStartup = false;

        while(baseType is not null)
        {
            if(SharedTypes.HasMetadataName(baseType, SharedTypes.SINGLETON))
            {
                isSingleton = true;
                break;
            }
            if(SharedTypes.HasMetadataName(baseType, SharedTypes.TRANSIENT))
            {
                isTransient = true;
                break;
            }
            if(SharedTypes.HasMetadataName(baseType, SharedTypes.FARSIGHT_STARTUP))
            {
                isStartup = true;
                break;
            }

            baseType = baseType.BaseType;
        }

        if(!isSingleton && !isTransient && !isStartup)
        {
            return null;
        }

        if(!symbol.IsAbstract && symbol.TypeParameters.Length > 0)
        {
            return null;
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var serviceTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();

        if(!classDeclarationSyntax.Modifiers.Any(m => m.ValueText == "partial"))
        {
            diagnostics.Add(Diagnostic.Create(
                DiagnosticsCatalogue.ServiceClassMustBePartial,
                classDeclarationSyntax.Identifier.GetLocation(),
                [symbol.Name]
            ));
        }

        var injectedFields = GetInjectedFields(symbol, diagnostics);

        foreach(var attributeData in GetInheritedServiceTypeAttributes(symbol))
        {
            if(attributeData.AttributeClass is not INamedTypeSymbol attributeClass
               || !SharedTypes.HasMetadataName(attributeClass, SharedTypes.SERVICE_TYPE_ATTRIBUTE))
            {
                continue;
            }

            if(attributeClass.TypeArguments.Length != 1)
            {
                continue;
            }

            var serviceType = attributeClass.TypeArguments[0];
            if(serviceType.TypeKind != TypeKind.Interface)
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticsCatalogue.ServiceTypeMustBeInterface,
                    classDeclarationSyntax.Identifier.GetLocation(),
                    [serviceType.ToDisplayString(), symbol.Name]
                ));
                continue;
            }

            bool implementsServiceType = symbol.AllInterfaces
                .Any(singletonInterface => SymbolEqualityComparer.Default.Equals(singletonInterface, serviceType));

            if(!implementsServiceType)
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticsCatalogue.ServiceTypeNotImplemented,
                    classDeclarationSyntax.Identifier.GetLocation(),
                    [symbol.Name, serviceType.ToDisplayString()]
                ));
                continue;
            }

            serviceTypes.Add(serviceType);
        }

        var kind = isStartup
            ? GeneratedServiceKind.Startup
            : isTransient ? GeneratedServiceKind.Transient : GeneratedServiceKind.Singleton;

        return new GeneratedServiceModel(
            symbol,
            injectedFields,
            GetBaseInjectedFields(symbol),
            DistinctServiceTypes(serviceTypes),
            diagnostics.ToImmutable(),
            symbol.IsAbstract,
            kind
        );
    }

    private static ImmutableArray<InjectedFieldModel> GetInjectedFields(
        INamedTypeSymbol symbol,
        ImmutableArray<Diagnostic>.Builder? diagnostics = null)
    {
        var injectedFields = ImmutableArray.CreateBuilder<InjectedFieldModel>();
        foreach(var member in symbol.GetMembers().OfType<IFieldSymbol>())
        {
            var injectAttr = member.GetAttributes()
                .FirstOrDefault(a => SharedTypes.HasMetadataName(a.AttributeClass, SharedTypes.INJECT_ATTRIBUTE));

            if(injectAttr is null)
            {
                continue;
            }

            if(diagnostics is not null)
            {
                AddInjectedFieldDiagnostics(symbol, member, diagnostics);
            }

            injectedFields.Add(new InjectedFieldModel(member.Type.ToDisplayString(), member.Name));
        }

        return injectedFields.ToImmutable();
    }

    private static void AddInjectedFieldDiagnostics(
        INamedTypeSymbol symbol,
        IFieldSymbol member,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if(member.DeclaredAccessibility is not Accessibility.Private and not Accessibility.Protected)
        {
            diagnostics.Add(Diagnostic.Create(
                DiagnosticsCatalogue.InjectedFieldMustBePrivate,
                member.Locations[0],
                [member.Name, symbol.Name]
            ));
        }

        if(!member.IsReadOnly)
        {
            diagnostics.Add(Diagnostic.Create(
                DiagnosticsCatalogue.InjectedFieldMustBeReadonly,
                member.Locations[0],
                [member.Name, symbol.Name]
            ));
        }

        var fieldSyntax = member.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
        if(fieldSyntax?.Initializer is not null)
        {
            diagnostics.Add(Diagnostic.Create(
                DiagnosticsCatalogue.InjectedFieldMustNotHaveInitializer,
                fieldSyntax?.Initializer?.GetLocation() ?? member.Locations[0],
                [member.Name, symbol.Name]
            ));
        }
    }

    private static ImmutableArray<InjectedFieldModel> GetBaseInjectedFields(INamedTypeSymbol symbol)
    {
        var baseTypes = new Stack<INamedTypeSymbol>();
        for(var baseType = symbol.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if(SharedTypes.HasMetadataName(baseType, SharedTypes.SINGLETON)
               || SharedTypes.HasMetadataName(baseType, SharedTypes.TRANSIENT)
               || SharedTypes.HasMetadataName(baseType, SharedTypes.FARSIGHT_STARTUP))
            {
                break;
            }

            baseTypes.Push(baseType);
        }

        var injectedFields = new List<InjectedFieldModel>();
        while(baseTypes.Count > 0)
        {
            injectedFields.AddRange(GetInjectedFields(baseTypes.Pop()));
        }

        return [.. injectedFields];
    }

    private static IEnumerable<AttributeData> GetInheritedServiceTypeAttributes(INamedTypeSymbol symbol)
    {
        for(var current = symbol; current is not null; current = current.BaseType)
        {
            foreach(var attributeData in current.GetAttributes())
            {
                if(attributeData.AttributeClass is INamedTypeSymbol attributeClass
                   && SharedTypes.HasMetadataName(attributeClass, SharedTypes.SERVICE_TYPE_ATTRIBUTE))
                {
                    yield return attributeData;
                }
            }
        }
    }

    private static void Execute(Compilation compilation, ImmutableArray<ConfigOptionModel> configOptions, ImmutableArray<GeneratedServiceModel> generatedServices, SourceProductionContext context)
    {
        var optionRegistrations = new StringBuilder();
        var optionValidatorTypes = new StringBuilder();
        var serviceRegistrations = new StringBuilder();

        foreach(var classOption in GetUniqueConfigOptions(configOptions))
        {
            foreach(var diagnostic in classOption.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            if(classOption.Diagnostics.Length > 0)
            {
                continue;
            }

            string configSection = String.IsNullOrWhiteSpace(classOption.SectionName)
                ? "builder.Configuration"
                : $"""builder.Configuration.GetSection("{classOption.SectionName}")""";

            var binderOptionAssignments = new List<string>();
            if(classOption.ErrorOnUnknownConfiguration)
            {
                binderOptionAssignments.Add("binderOptions.ErrorOnUnknownConfiguration = true");
            }

            if(classOption.BindNonPublicProperties)
            {
                binderOptionAssignments.Add("binderOptions.BindNonPublicProperties = true");
            }

            string bindCall = binderOptionAssignments.Count > 0
                ? $".Bind({configSection}, binderOptions => {{ {String.Join("; ", binderOptionAssignments)}; }})"
                : $".Bind({configSection})";

            optionRegistrations.AppendLine(
                $$"""
                builder.Services.AddOptionsWithValidateOnStart<{{classOption.FullName}}>()
                    {{bindCall}}
                    .ValidateDataAnnotations();
                builder.Services.AddSingleton<{{classOption.FullName}}>(
                    provider => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<global::Microsoft.Extensions.Options.IOptions<{{classOption.FullName}}>>(provider).Value);
                """
            );

            if(classOption is { ValidatorFullName: not null, ValidatorHelperTypeName: not null })
            {
                optionRegistrations.AppendLine(
                    $$"""
                    builder.Services.AddSingleton<Microsoft.Extensions.Options.IValidateOptions<{{classOption.FullName}}>, Farsight.Common.Generated.{{classOption.ValidatorHelperTypeName}}>();
                    """
                );

                optionValidatorTypes.AppendLine(
                    $$"""
                    internal sealed class {{classOption.ValidatorHelperTypeName}} : global::Microsoft.Extensions.Options.IValidateOptions<{{classOption.FullName}}>
                    {
                        public global::Microsoft.Extensions.Options.ValidateOptionsResult Validate(string? name, {{classOption.FullName}} options)
                        {
                            _ = name;

                            if(options is null)
                            {
                                return global::Microsoft.Extensions.Options.ValidateOptionsResult.Skip;
                            }

                            var validationResult = new {{classOption.ValidatorFullName}}().Validate(options);
                            if(validationResult.IsValid)
                            {
                                return global::Microsoft.Extensions.Options.ValidateOptionsResult.Success;
                            }

                            var failures = new global::System.Collections.Generic.List<string>();
                            foreach(var failure in validationResult.Errors)
                            {
                                if(failure is null)
                                {
                                    continue;
                                }

                                failures.Add(
                                    global::System.String.IsNullOrWhiteSpace(failure.PropertyName)
                                        ? failure.ErrorMessage
                                        : $"{failure.PropertyName}: {failure.ErrorMessage}");
                            }

                            return failures.Count == 0
                                ? global::Microsoft.Extensions.Options.ValidateOptionsResult.Fail("FluentValidation reported a validation failure without any error details.")
                                : global::Microsoft.Extensions.Options.ValidateOptionsResult.Fail(failures);
                        }
                    }
                    """
                );
            }
        }

        foreach(var service in GetUniqueGeneratedServices(generatedServices))
        {
            foreach(var diagnostic in service.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            if(service.Diagnostics.Length > 0)
            {
                continue;
            }

            if(!service.IsAbstract)
            {
                string serviceName = service.TypeSymbol.ToDisplayString();
                string registrationMethod = service.Kind == GeneratedServiceKind.Transient
                    ? "AddTransient"
                    : "AddSingleton";
                serviceRegistrations.AppendLine(
                    $"""
                    builder.Services.{registrationMethod}<{serviceName}>();
                    """
                );

                if(service.Kind == GeneratedServiceKind.Singleton)
                {
                    serviceRegistrations.AppendLine(
                        $"""
                        builder.Services.AddSingleton<Singleton, {serviceName}>(provider => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{serviceName}>(provider));
                        """);
                }

                foreach(var serviceType in service.ServiceTypes)
                {
                    string serviceTypeName = serviceType.ToDisplayString();
                    serviceRegistrations.AppendLine(
                        $"""
                        builder.Services.{registrationMethod}<{serviceTypeName}, {serviceName}>(provider => global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{serviceName}>(provider));
                        """);
                }
            }

            GeneratePaddingConstructor(service, context);
        }

        var registrationCalls = new StringBuilder();
        if(optionRegistrations.Length > 0)
        {
            registrationCalls.AppendLine(
                $$"""
                global::Farsight.Common.FarsightCommonRegistry.RegisterOptions(builder =>
                {
                {{CodeUtils.Indent(optionRegistrations.ToString(), 16)}}
                });
                """
            );
        }

        if(serviceRegistrations.Length > 0)
        {
            registrationCalls.AppendLine(
                $$"""
                global::Farsight.Common.FarsightCommonRegistry.RegisterServices(builder =>
                {
                {{CodeUtils.Indent(serviceRegistrations.ToString(), 16)}}
                });
                """
            );
        }

        bool hasLocalRegistrations = registrationCalls.Length > 0;
        if(hasLocalRegistrations)
        {
            string localRegistrarTypeName = BuildRegistrarTypeName(compilation);
            string registrarSource = $$"""
                #nullable enable

                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;
                using Microsoft.Extensions.Configuration;

                [assembly: global::Farsight.Common.FarsightRegistrarAttribute<global::Farsight.Common.Generated.{{localRegistrarTypeName}}>]

                namespace Farsight.Common.Generated;
                public sealed class {{localRegistrarTypeName}}
                {
                    private static int _isRegistered;

                    public static void Register()
                    {
                        if(global::System.Threading.Interlocked.Exchange(ref _isRegistered, 1) == 1)
                        {
                            return;
                        }

                {{CodeUtils.Indent(registrationCalls.ToString(), 8)}}
                    }
                }
                {{(optionValidatorTypes.Length > 0 ? "\n" + optionValidatorTypes.ToString() : String.Empty)}}
                """;

            context.AddSource("FarsightCommonRegistrar.g.cs", SourceText.From(registrarSource, Encoding.UTF8));
        }

        var registrarCalls = new List<string>();
        if(hasLocalRegistrations)
        {
            registrarCalls.Add($"global::Farsight.Common.Generated.{BuildRegistrarTypeName(compilation)}.Register();");
        }

        registrarCalls.AddRange(GetReferencedRegistrarCalls(compilation));

        if(registrarCalls.Count == 0)
        {
            return;
        }

        string bootstrapSource = $$"""
            #nullable enable

            using System.Runtime.CompilerServices;

            namespace Farsight.Common.Generated;
            internal static class FarsightBootstrapInitializer
            {
                [ModuleInitializer]
                internal static void Initialize()
                {
            {{CodeUtils.Indent(String.Join("\n", registrarCalls), 8)}}
                }
            }
            """;

        context.AddSource("FarsightBootstrapInitializer.g.cs", SourceText.From(bootstrapSource, Encoding.UTF8));
    }

    private static IEnumerable<string> GetReferencedRegistrarCalls(Compilation compilation)
    {
        var registrarTypes = new HashSet<string>(StringComparer.Ordinal);

        foreach(var assemblySymbol in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            foreach(var attribute in assemblySymbol.GetAttributes())
            {
                if(attribute.AttributeClass is not INamedTypeSymbol attributeClass)
                {
                    continue;
                }

                if(!SharedTypes.HasMetadataName(attributeClass.ConstructedFrom, SharedTypes.FARSIGHT_REGISTRAR_ATTRIBUTE))
                {
                    continue;
                }

                if(TryGetRegistrarTypeFromAttribute(attributeClass, out var registrarType))
                {
                    registrarTypes.Add(registrarType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                }
            }
        }

        return registrarTypes
            .OrderBy(typeName => typeName, StringComparer.Ordinal)
            .Select(typeName => $"{typeName}.Register();");
    }

    private static bool TryGetRegistrarTypeFromAttribute(INamedTypeSymbol attributeClass, out INamedTypeSymbol registrarType)
    {
        registrarType = null!;

        if(!SharedTypes.HasMetadataName(attributeClass.ConstructedFrom, SharedTypes.FARSIGHT_REGISTRAR_ATTRIBUTE))
        {
            return false;
        }

        if(attributeClass.TypeArguments.Length == 1 && attributeClass.TypeArguments[0] is INamedTypeSymbol genericRegistrarType)
        {
            registrarType = genericRegistrarType;
            return true;
        }

        return false;
    }

    private static void GeneratePaddingConstructor(GeneratedServiceModel service, SourceProductionContext context)
    {
        string typeDeclarationName = service.TypeSymbol.ToDisplayString(new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters));
        string typeModifier = service.IsAbstract ? "abstract" : "sealed";

        var parametersList = new List<string>
        {
            "System.IServiceProvider provider",
            $"Microsoft.Extensions.Logging.ILogger<{typeDeclarationName}> logger",
            "Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime"
        };
        parametersList.AddRange(service.BaseInjectedFields.Select(f => $"{f.TypeFullName} {f.Name.TrimStart('_')}"));
        parametersList.AddRange(service.InjectedFields.Select(f => $"{f.TypeFullName} {f.Name.TrimStart('_')}"));
        string parameters = String.Join(", ", parametersList);
        string baseArguments = String.Join(", ", new[] { "provider", "logger", "lifetime" }.Concat(service.BaseInjectedFields.Select(f => f.Name.TrimStart('_'))));

        var assignments = new StringBuilder();
        foreach(var field in service.InjectedFields)
        {
            assignments.AppendLine($"this.{field.Name} = {field.Name.TrimStart('_')};");
        }

        string source = service.TypeSymbol.ContainingNamespace.IsGlobalNamespace
            ? $$"""
                #nullable enable

                {{typeModifier}} partial class {{typeDeclarationName}}
                {
                    public {{service.TypeSymbol.Name}}({{parameters}}) : base({{baseArguments}})
                    {
                {{CodeUtils.Indent(assignments.ToString(), 8)}}
                    }
                }
                """
            : $$"""
                #nullable enable

                namespace {{service.TypeSymbol.ContainingNamespace.ToDisplayString()}}
                {
                    {{typeModifier}} partial class {{typeDeclarationName}}
                    {
                        public {{service.TypeSymbol.Name}}({{parameters}}) : base({{baseArguments}})
                        {
                {{CodeUtils.Indent(assignments.ToString(), 12)}}
                        }
                    }
                }
                """;

        string hintName = BuildGeneratedServiceHintName(service.TypeSymbol);
        context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
    }

    private static IEnumerable<GeneratedServiceModel> GetUniqueGeneratedServices(ImmutableArray<GeneratedServiceModel> generatedServices)
    {
        var uniqueServices = new Dictionary<INamedTypeSymbol, GeneratedServiceModel>(SymbolEqualityComparer.Default);

        foreach(var service in generatedServices)
        {
            if(uniqueServices.TryGetValue(service.TypeSymbol, out var existing))
            {
                var merged = existing with
                {
                    InjectedFields = [.. existing.InjectedFields.Concat(service.InjectedFields).Distinct()],
                    BaseInjectedFields = [.. existing.BaseInjectedFields.Concat(service.BaseInjectedFields).Distinct()],
                    ServiceTypes = DistinctServiceTypes(existing.ServiceTypes.Concat(service.ServiceTypes)),
                    Diagnostics = [.. existing.Diagnostics, .. service.Diagnostics]
                };
                uniqueServices[service.TypeSymbol] = merged;
                continue;
            }

            uniqueServices[service.TypeSymbol] = service;
        }

        return uniqueServices.Values;
    }

    private static IEnumerable<ConfigOptionModel> GetUniqueConfigOptions(ImmutableArray<ConfigOptionModel> configOptions)
    {
        var uniqueConfigOptions = new Dictionary<string, ConfigOptionModel>(StringComparer.Ordinal);

        foreach(var configOption in configOptions)
        {
            if(uniqueConfigOptions.TryGetValue(configOption.FullName, out var existing))
            {
                uniqueConfigOptions[configOption.FullName] = existing with
                {
                    Diagnostics = [.. existing.Diagnostics, .. configOption.Diagnostics]
                };
                continue;
            }

            uniqueConfigOptions[configOption.FullName] = configOption;
        }

        return uniqueConfigOptions.Values;
    }

    private static string BuildGeneratedServiceHintName(INamedTypeSymbol typeSymbol)
    {
        string typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var hintNameBuilder = new StringBuilder(typeName.Length + 5);

        foreach(char character in typeName)
        {
            hintNameBuilder.Append(Char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        }

        hintNameBuilder.Append(".g.cs");
        return hintNameBuilder.ToString();
    }

    private static string BuildConfigOptionValidatorTypeName(INamedTypeSymbol typeSymbol)
    {
        string typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var typeNameBuilder = new StringBuilder("FarsightConfigValidator_");

        foreach(char character in typeName)
        {
            typeNameBuilder.Append(Char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        }

        return typeNameBuilder.ToString();
    }

    private static bool TryGetConfigOptionAttribute(AttributeData attributeData, out INamedTypeSymbol? validatorType)
    {
        validatorType = null;

        if(attributeData.AttributeClass is not INamedTypeSymbol attributeClass)
        {
            return false;
        }

        if(!SharedTypes.HasMetadataName(attributeClass, SharedTypes.CONFIG_OPTION_ATTRIBUTE)
           && !SharedTypes.HasMetadataName(attributeClass, SharedTypes.GENERIC_CONFIG_OPTION_ATTRIBUTE))
        {
            return false;
        }

        if(attributeClass.Arity == 0)
        {
            return true;
        }

        if(attributeClass is { Arity: 1, TypeArguments.Length: 1 }
           && attributeClass.TypeArguments[0] is INamedTypeSymbol namedValidatorType)
        {
            validatorType = namedValidatorType;
            return true;
        }

        return false;
    }

    private static ImmutableArray<ITypeSymbol> DistinctServiceTypes(IEnumerable<ITypeSymbol> serviceTypes)
    {
        var uniqueServiceTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var serviceTypeBuilder = ImmutableArray.CreateBuilder<ITypeSymbol>();

        foreach(var serviceType in serviceTypes)
        {
            if(uniqueServiceTypes.Add(serviceType))
            {
                serviceTypeBuilder.Add(serviceType);
            }
        }

        return serviceTypeBuilder.ToImmutable();
    }

    private static string BuildRegistrarTypeName(Compilation compilation)
    {
        string assemblyName = compilation.AssemblyName ?? "UnknownAssembly";
        var typeNameBuilder = new StringBuilder("FarsightRegistrar_");

        foreach(char character in assemblyName)
        {
            typeNameBuilder.Append(Char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        }

        return typeNameBuilder.ToString();
    }
}
