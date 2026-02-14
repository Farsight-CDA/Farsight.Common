using Microsoft.CodeAnalysis;

namespace Farsight.Common;

internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor PartialClassRequired = new(
        id: "FSG001",
        title: "Singleton class must be partial",
        messageFormat: "The class '{0}' inherits from Singleton and must be declared as partial to support constructor injection generation",
        category: "SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PrivateReadonlyRequired = new(
        id: "FSG002",
        title: "Injected field must be private readonly",
        messageFormat: "The field '{0}' in class '{1}' is marked with InjectAttribute but is not private readonly. Skipping processing for this class.",
        category: "SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
