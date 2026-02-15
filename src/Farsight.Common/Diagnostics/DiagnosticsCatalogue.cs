using Microsoft.CodeAnalysis;

namespace Farsight.Common.Diagnostics;

internal static class DiagnosticsCatalogue
{
    public static readonly DiagnosticDescriptor PartialClassRequired = new DiagnosticDescriptor(
        id: "FC001",
        title: "Singleton class must be partial",
        messageFormat: "The class '{0}' inherits from Singleton and must be declared as partial to support constructor injection generation",
        category: RuleCategories.USAGE,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor PrivateReadonlyRequired = new DiagnosticDescriptor(
        id: "FC002",
        title: "Injected field must be private readonly",
        messageFormat: "The field '{0}' in class '{1}' is marked with InjectAttribute but is not private readonly. Skipping processing for this class.",
        category: RuleCategories.USAGE,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
