using Microsoft.CodeAnalysis;

namespace Farsight.Common;

internal static class SharedTypes
{
    public const string NAMESPACE = "Farsight.Common";
    public const string STARTUP_NAMESPACE = NAMESPACE + ".Startup";

    public const string CONFIG_OPTION_ATTRIBUTE = NAMESPACE + ".ConfigOptionAttribute";
    public const string GENERIC_CONFIG_OPTION_ATTRIBUTE = NAMESPACE + ".ConfigOptionAttribute`1";
    public const string FARSIGHT_REGISTRAR_ATTRIBUTE = NAMESPACE + ".FarsightRegistrarAttribute`1";
    public const string INJECT_ATTRIBUTE = NAMESPACE + ".InjectAttribute";
    public const string SERVICE_TYPE_ATTRIBUTE = NAMESPACE + ".ServiceTypeAttribute`1";
    public const string SINGLETON = NAMESPACE + ".Singleton";
    public const string FARSIGHT_STARTUP = STARTUP_NAMESPACE + ".FarsightStartup";

    public const string SECTION_NAME_PROPERTY = "SectionName";
    public const string ERROR_ON_UNKNOWN_CONFIGURATION_PROPERTY = "ErrorOnUnknownConfiguration";
    public const string BIND_NON_PUBLIC_PROPERTIES_PROPERTY = "BindNonPublicProperties";

    public static bool HasMetadataName(INamedTypeSymbol? symbol, string metadataName)
    {
        if(symbol is null)
        {
            return false;
        }

        string namespaceName = symbol.ContainingNamespace?.ToDisplayString() ?? String.Empty;
        string fullyQualifiedMetadataName = String.IsNullOrEmpty(namespaceName)
            ? symbol.MetadataName
            : $"{namespaceName}.{symbol.MetadataName}";

        return fullyQualifiedMetadataName == metadataName;
    }
}
