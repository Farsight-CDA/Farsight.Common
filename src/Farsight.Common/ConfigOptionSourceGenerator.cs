using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace Farsight.Common;

[Generator]
public class ConfigOptionSourceGenerator : IIncrementalGenerator
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

    private static SingletonModel? GetSingletonSemanticTarget(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax) context.Node;
        if(context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        var baseType = symbol.BaseType;
        bool inheritsFromSingleton = false;
        while(baseType is not null)
        {
            if(baseType.ToDisplayString() == typeof(Singleton).FullName)
            {
                inheritsFromSingleton = true;
                break;
            }
            baseType = baseType.BaseType;
        }

        if(!inheritsFromSingleton)
        {
            return null;
        }

        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
        bool isValid = true;

        if(!classDeclarationSyntax.Modifiers.Any(m => m.ValueText == "partial"))
        {
            diagnostics.Add(new DiagnosticInfo(
                DiagnosticDescriptors.PartialClassRequired.Id,
                DiagnosticDescriptors.PartialClassRequired.Title.ToString(),
                DiagnosticDescriptors.PartialClassRequired.MessageFormat.ToString(),
                DiagnosticDescriptors.PartialClassRequired.Category,
                DiagnosticDescriptors.PartialClassRequired.DefaultSeverity,
                classDeclarationSyntax.Identifier.GetLocation(),
                [symbol.Name]));
            isValid = false;
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
                    diagnostics.Add(new DiagnosticInfo(
                        DiagnosticDescriptors.PrivateReadonlyRequired.Id,
                        DiagnosticDescriptors.PrivateReadonlyRequired.Title.ToString(),
                        DiagnosticDescriptors.PrivateReadonlyRequired.MessageFormat.ToString(),
                        DiagnosticDescriptors.PrivateReadonlyRequired.Category,
                        DiagnosticDescriptors.PrivateReadonlyRequired.DefaultSeverity,
                        member.Locations[0],
                        [member.Name, symbol.Name]));
                    isValid = false;
                }
                else
                {
                    injectedFields.Add(new InjectedFieldModel(member.Type.ToDisplayString(), member.Name));
                }
            }
        }

        return new SingletonModel(
            symbol.ToDisplayString(),
            symbol.Name,
            symbol.ContainingNamespace.ToDisplayString(),
            injectedFields.ToImmutable(),
            diagnostics.ToImmutable(),
            isValid
        );
    }

    private static void Execute(ImmutableArray<ConfigOptionModel> configOptions, ImmutableArray<SingletonModel> singletons, SourceProductionContext context)
    {
        var registrations = new StringBuilder();

        foreach(var classOption in configOptions.Distinct())
        {
            string fullName = classOption.FullName;
            string binding = String.IsNullOrWhiteSpace(classOption.SectionName)
                ? "builder.Configuration"
                : $"""builder.Configuration.GetSection("{classOption.SectionName}")""";

            registrations.AppendLine(Indent($"""
                builder.Services.AddOptionsWithValidateOnStart<{fullName}>()
                    .Bind({binding})
                    .ValidateDataAnnotations();
                """, 16));
        }

        foreach(var singleton in singletons.Distinct())
        {
            foreach(var diagInfo in singleton.Diagnostics)
            {
                var descriptor = new DiagnosticDescriptor(diagInfo.Id, diagInfo.Title, diagInfo.MessageFormat, diagInfo.Category, diagInfo.Severity, true);
                context.ReportDiagnostic(Diagnostic.Create(descriptor, diagInfo.Location, diagInfo.Args));
            }

            if(!singleton.IsValid)
            {
                continue;
            }

            string fullName = singleton.FullName;
            registrations.AppendLine(Indent($"""builder.Services.AddSingleton<{fullName}>();""", 16));

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
            {{registrations}}
                    });
                }
            }
            """;

        context.AddSource("FarsightCommonInitializer.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static void GeneratePaddingConstructor(SingletonModel singleton, SourceProductionContext context)
    {
        var fields = singleton.InjectedFields;
        string className = singleton.Name;

        var parametersList = new List<string>
        {
            "System.IServiceProvider provider",
            $"Microsoft.Extensions.Logging.ILogger<{className}> logger"
        };
        parametersList.AddRange(fields.Select(f => $"{f.TypeFullName} {f.Name.TrimStart('_')}"));
        string parameters = String.Join(", ", parametersList);

        var assignments = new StringBuilder();
        foreach(var field in fields)
        {
            assignments.AppendLine(Indent($"this.{field.Name} = {field.Name.TrimStart('_')};", 12));
        }

        string namespaceName = singleton.Namespace;
        string source = $$"""
            namespace {{namespaceName}}
            {
                partial class {{className}}
                {
                    public {{className}}({{parameters}}) : base(provider, logger)
                    {
            {{assignments}}
                    }
                }
            }
            """;

        context.AddSource($"{className}.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string Indent(string text, int spaces)
    {
        string indentation = new string(' ', spaces);
        return String.Join("\n", text.Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(line => String.IsNullOrWhiteSpace(line)
                ? line
                : indentation + line
            )
        );
    }
}
