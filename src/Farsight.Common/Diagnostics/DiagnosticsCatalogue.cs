using Microsoft.CodeAnalysis;

namespace Farsight.Common.Diagnostics;

internal static class DiagnosticsCatalogue
{
    public static readonly DiagnosticDescriptor SingletonClassMustBePartial = new DiagnosticDescriptor(
        id: "FC001",
        title: "Singleton class must be partial",
        messageFormat: "The class '{0}' inherits from Singleton and must be declared as partial to support constructor injection generation",
        category: RuleCategories.USAGE,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor InjectedFieldMustBePrivate = new DiagnosticDescriptor(
        id: "FC101",
        title: "Injected field must be private",
        messageFormat: "The field '{0}' in class '{1}' is marked with InjectAttribute but is not private",
        category: RuleCategories.USAGE,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor InjectedFieldMustBeReadonly = new DiagnosticDescriptor(
        id: "FC102",
        title: "Injected field must be readonly",
        messageFormat: "The field '{0}' in class '{1}' is marked with InjectAttribute but is not readonly",
        category: RuleCategories.USAGE,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor InjectedFieldMustNotHaveInitializer = new DiagnosticDescriptor(
        id: "FC103",
        title: "Injected field must not have an initializer",
        messageFormat: "The field '{0}' in class '{1}' is marked with InjectAttribute and must not have a value assigned to it in its declaration",
        category: RuleCategories.USAGE,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor ServiceTypeMustBeInterface = new DiagnosticDescriptor(
        id: "FC201",
        title: "ServiceType must specify an interface",
        messageFormat: "The service type '{0}' on class '{1}' must be an interface",
        category: RuleCategories.USAGE,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor ServiceTypeNotImplemented = new DiagnosticDescriptor(
        id: "FC202",
        title: "ServiceType must be implemented by singleton",
        messageFormat: "The class '{0}' must implement the interface '{1}' specified by ServiceTypeAttribute",
        category: RuleCategories.USAGE,
        DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}
