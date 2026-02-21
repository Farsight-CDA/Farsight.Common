using Farsight.Common.Diagnostics;
using Farsight.Common.Startup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Farsight.Common;

[Generator]
public class ApplicationConfigurationGenerator : IIncrementalGenerator
{
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

        context.RegisterSourceOutput(combined,
            static (spc, source) => Execute(source.Left, source.Right, spc));
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

        return new SingletonModel(
            symbol,
            injectedFields.ToImmutable(),
            diagnostics.ToImmutable(),
            isStartup
        );
    }

    private static void Execute(ImmutableArray<ConfigOptionModel> configOptions, ImmutableArray<SingletonModel> singletons, SourceProductionContext context)
    {
        var registrations = new StringBuilder();

        foreach(var classOption in configOptions.Distinct())
        {
            string configSection = String.IsNullOrWhiteSpace(classOption.SectionName)
                ? "builder.Configuration"
                : $"""builder.Configuration.GetSection("{classOption.SectionName}")""";

            registrations.AppendLine(
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
            registrations.AppendLine(
                $"""
                builder.Services.AddSingleton<{serviceName}>();
                """
            );

            if(!singleton.IsStartup)
            {
                registrations.AppendLine(
                    $"""
                    builder.Services.AddSingleton<Singleton, {serviceName}>(provider => provider.GetService<{serviceName}>());
                    """);
            }

            GeneratePaddingConstructor(singleton, context);
        }

        if(registrations.Length == 0)
        {
            return;
        }

        string source = $$"""
            using System.Runtime.CompilerServices;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;
            using Microsoft.Extensions.Configuration;
            using Farsight.Common;

            namespace Farsight.Common.Generated;
            internal static class FarsightCommonInitializer
            {
                [ModuleInitializer]
                internal static void Initialize()
                {
                    {{nameof(FarsightCommonRegistry)}}.{{nameof(FarsightCommonRegistry.Register)}}(builder =>
                    {
            {{CodeUtils.Indent(registrations.ToString(), 12)}}
                    });
                }
            }
            """;

        context.AddSource("FarsightCommonInitializer.g.cs", SourceText.From(source, Encoding.UTF8));
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
}
