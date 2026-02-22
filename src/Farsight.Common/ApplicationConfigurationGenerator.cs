using Farsight.Common.Diagnostics;
using Farsight.Common.Startup;
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
                predicate: static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .Collect();

        var singletons = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetSingletonSemanticTarget(ctx))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .Collect();

        var combined = configOptions.Combine(singletons);
        var generationInput = context.CompilationProvider.Combine(combined);

        context.RegisterSourceOutput(generationInput,
            static (spc, source) => Execute(source.Left, source.Right.Left, source.Right.Right, spc));
    }

    internal record struct ConfigOptionModel(string FullName, string? SectionName);
    private static ConfigOptionModel? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax) context.Node;
        if(context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var attributeData = symbol
            .GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == typeof(ConfigOptionAttribute).FullName);

        if(attributeData is null)
        {
            return null;
        }

        string? sectionName = null;
        var sectionNameArg = attributeData.NamedArguments.FirstOrDefault(kvp => kvp.Key == nameof(ConfigOptionAttribute.SectionName));
        if(sectionNameArg.Value.Value is string s)
        {
            sectionName = s;
        }

        return new ConfigOptionModel(symbol.ToDisplayString(), sectionName);
    }

    internal record struct InjectedFieldModel(string TypeFullName, string Name);
    internal record struct SingletonModel(
        INamedTypeSymbol TypeSymbol,
        ImmutableArray<InjectedFieldModel> InjectedFields,
        ImmutableArray<ITypeSymbol> ServiceTypes,
        ImmutableArray<Diagnostic> Diagnostics,
        bool IsStartup
    );
    private static SingletonModel? GetSingletonSemanticTarget(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax) context.Node;
        if(context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var baseType = symbol.BaseType;
        bool isSingleton = false;
        bool isStartup = false;

        while(baseType is not null)
        {
            string baseTypeString = baseType.ToDisplayString();
            if(baseTypeString == typeof(Singleton).FullName)
            {
                isSingleton = true;
                break;
            }
            if(baseTypeString == typeof(FarsightStartup).FullName)
            {
                isStartup = true;
                break;
            }

            baseType = baseType.BaseType;
        }

        if(!isSingleton && !isStartup)
        {
            return null;
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var serviceTypes = ImmutableArray.CreateBuilder<ITypeSymbol>();

        if(!classDeclarationSyntax.Modifiers.Any(m => m.ValueText == "partial"))
        {
            diagnostics.Add(Diagnostic.Create(
                DiagnosticsCatalogue.PartialClassRequired,
                classDeclarationSyntax.Identifier.GetLocation(),
                [symbol.Name]
            ));
        }

        var injectedFields = ImmutableArray.CreateBuilder<InjectedFieldModel>();

        foreach(var member in symbol.GetMembers().OfType<IFieldSymbol>())
        {
            var injectAttr = member.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == typeof(InjectAttribute).FullName);

            if(injectAttr is not null)
            {
                if(member.DeclaredAccessibility != Accessibility.Private || !member.IsReadOnly)
                {
                    diagnostics.Add(Diagnostic.Create(
                        DiagnosticsCatalogue.PrivateReadonlyRequired,
                        member.Locations[0],
                        [member.Name, symbol.Name]
                    ));
                }
                else
                {
                    injectedFields.Add(new InjectedFieldModel(member.Type.ToDisplayString(), member.Name));
                }
            }
        }

        foreach(var attributeData in symbol.GetAttributes())
        {
            if(attributeData.AttributeClass is not INamedTypeSymbol { Name: nameof(ServiceTypeAttribute<object>), Arity: 1 } attributeClass
               || attributeClass.ContainingNamespace.ToDisplayString() != typeof(ServiceTypeAttribute<>).Namespace)
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

        return new SingletonModel(
            symbol,
            injectedFields.ToImmutable(),
            DistinctServiceTypes(serviceTypes),
            diagnostics.ToImmutable(),
            isStartup
        );
    }

    private static void Execute(Compilation compilation, ImmutableArray<ConfigOptionModel> configOptions, ImmutableArray<SingletonModel> singletons, SourceProductionContext context)
    {
        var optionRegistrations = new StringBuilder();
        var serviceRegistrations = new StringBuilder();

        foreach(var classOption in configOptions.Distinct())
        {
            string configSection = String.IsNullOrWhiteSpace(classOption.SectionName)
                ? "builder.Configuration"
                : $"""builder.Configuration.GetSection("{classOption.SectionName}")""";

            optionRegistrations.AppendLine(
                $$"""
                builder.Services.AddOptionsWithValidateOnStart<{{classOption.FullName}}>()
                    .Bind({{configSection}})
                    .ValidateDataAnnotations();
                builder.Services.AddSingleton<{{classOption.FullName}}>(
                    provider => provider.GetService<Microsoft.Extensions.Options.IOptions<{{classOption.FullName}}>>().Value);
                """
            );
        }

        foreach(var singleton in GetUniqueSingletons(singletons))
        {
            foreach(var diagnostic in singleton.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }

            if(singleton.Diagnostics.Length > 0)
            {
                continue;
            }

            string serviceName = singleton.TypeSymbol.ToDisplayString();
            serviceRegistrations.AppendLine(
                $"""
                builder.Services.AddSingleton<{serviceName}>();
                """
            );

            if(!singleton.IsStartup)
            {
                serviceRegistrations.AppendLine(
                    $"""
                    builder.Services.AddSingleton<Singleton, {serviceName}>(provider => provider.GetService<{serviceName}>());
                    """);
            }

            foreach(var serviceType in singleton.ServiceTypes)
            {
                string serviceTypeName = serviceType.ToDisplayString();
                serviceRegistrations.AppendLine(
                    $"""
                    builder.Services.AddSingleton<{serviceTypeName}, {serviceName}>(provider => provider.GetService<{serviceName}>());
                    """);
            }

            GeneratePaddingConstructor(singleton, context);
        }

        var registrationCalls = new StringBuilder();
        if(optionRegistrations.Length > 0)
        {
            registrationCalls.AppendLine(
                $$"""
                {{nameof(FarsightCommonRegistry)}}.{{nameof(FarsightCommonRegistry.RegisterOptions)}}(builder =>
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
                {{nameof(FarsightCommonRegistry)}}.{{nameof(FarsightCommonRegistry.RegisterServices)}}(builder =>
                {
                {{CodeUtils.Indent(serviceRegistrations.ToString(), 16)}}
                });
                """
            );
        }

        bool hasLocalRegistrations = registrationCalls.Length > 0;
        if(hasLocalRegistrations)
        {
            string registrarSource = $$"""
                using Microsoft.Extensions.DependencyInjection;
                using Microsoft.Extensions.Hosting;
                using Microsoft.Extensions.Configuration;

                [assembly: global::Farsight.Common.FarsightRegistrarAttribute<global::Farsight.Common.Generated.FarsightRegistrar>]

                namespace Farsight.Common.Generated;
                public sealed class FarsightRegistrar
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
                """;

            context.AddSource("FarsightCommonRegistrar.g.cs", SourceText.From(registrarSource, Encoding.UTF8));
        }

        var registrarCalls = new List<string>();
        if(hasLocalRegistrations)
        {
            registrarCalls.Add("global::Farsight.Common.Generated.FarsightRegistrar.Register();");
        }

        registrarCalls.AddRange(GetReferencedRegistrarCalls(compilation));

        if(registrarCalls.Count == 0)
        {
            return;
        }

        string bootstrapSource = $$"""
            using System.Runtime.CompilerServices;

            namespace Farsight.Common.Generated;
            internal static class FarsightBootstrapInitializer
            {
                [ModuleInitializer]
                internal static void Initialize()
                {
            {{CodeUtils.Indent(string.Join("\n", registrarCalls), 8)}}
                }
            }
            """;

        context.AddSource("FarsightBootstrapInitializer.g.cs", SourceText.From(bootstrapSource, Encoding.UTF8));
    }

    private static IEnumerable<string> GetReferencedRegistrarCalls(Compilation compilation)
    {
        var attributeType = compilation.GetTypeByMetadataName(typeof(FarsightRegistrarAttribute<>).FullName!);
        if(attributeType is null)
        {
            return [];
        }

        var registrarTypes = new HashSet<string>(StringComparer.Ordinal);

        foreach(var assemblySymbol in compilation.SourceModule.ReferencedAssemblySymbols)
        {
            foreach(var attribute in assemblySymbol.GetAttributes())
            {
                if(attribute.AttributeClass is not INamedTypeSymbol attributeClass
                   || !SymbolEqualityComparer.Default.Equals(attributeClass.ConstructedFrom, attributeType))
                {
                    continue;
                }

                if(attributeClass.TypeArguments.Length != 1)
                {
                    continue;
                }

                if(attributeClass.TypeArguments[0] is not INamedTypeSymbol registrarType)
                {
                    continue;
                }

                registrarTypes.Add(registrarType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        return registrarTypes
            .OrderBy(typeName => typeName, StringComparer.Ordinal)
            .Select(typeName => $"{typeName}.Register();");
    }

    private static void GeneratePaddingConstructor(SingletonModel singleton, SourceProductionContext context)
    {
        var fields = singleton.InjectedFields;

        var parametersList = new List<string>
        {
            "System.IServiceProvider provider",
            $"Microsoft.Extensions.Logging.ILogger<{singleton.TypeSymbol.Name}> logger",
            "Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime"
        };
        parametersList.AddRange(fields.Select(f => $"{f.TypeFullName} {f.Name.TrimStart('_')}"));
        string parameters = String.Join(", ", parametersList);

        var assignments = new StringBuilder();
        foreach(var field in fields)
        {
            assignments.AppendLine($"this.{field.Name} = {field.Name.TrimStart('_')};");
        }

        string source = $$"""
            namespace {{singleton.TypeSymbol.ContainingNamespace.ToDisplayString()}}
            {
                sealed partial class {{singleton.TypeSymbol.Name}}
                {
                    public {{singleton.TypeSymbol.Name}}({{parameters}}) : base(provider, logger, lifetime)
                    {
            {{CodeUtils.Indent(assignments.ToString(), 12)}}
                    }
                }
            }
            """;

        string hintName = BuildSingletonHintName(singleton.TypeSymbol);
        context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
    }

    private static IEnumerable<SingletonModel> GetUniqueSingletons(ImmutableArray<SingletonModel> singletons)
    {
        var uniqueSingletons = new Dictionary<INamedTypeSymbol, SingletonModel>(SymbolEqualityComparer.Default);

        foreach(var singleton in singletons)
        {
            if(uniqueSingletons.TryGetValue(singleton.TypeSymbol, out var existing))
            {
                var merged = existing with
                {
                    InjectedFields = existing.InjectedFields.Concat(singleton.InjectedFields).Distinct().ToImmutableArray(),
                    ServiceTypes = DistinctServiceTypes(existing.ServiceTypes.Concat(singleton.ServiceTypes)),
                    Diagnostics = existing.Diagnostics.Concat(singleton.Diagnostics).ToImmutableArray()
                };
                uniqueSingletons[singleton.TypeSymbol] = merged;
                continue;
            }

            uniqueSingletons[singleton.TypeSymbol] = singleton;
        }

        return uniqueSingletons.Values;
    }

    private static string BuildSingletonHintName(INamedTypeSymbol typeSymbol)
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
}
