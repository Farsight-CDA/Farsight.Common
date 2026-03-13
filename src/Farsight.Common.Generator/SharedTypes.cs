using Microsoft.CodeAnalysis;

namespace Farsight.Common;

internal static class SharedTypes
{
    public const string Namespace = "Farsight.Common";
    public const string StartupNamespace = Namespace + ".Startup";

    public const string ConfigOptionAttribute = Namespace + ".ConfigOptionAttribute";
    public const string GenericConfigOptionAttribute = Namespace + ".ConfigOptionAttribute`1";
    public const string FarsightRegistrarAttribute = Namespace + ".FarsightRegistrarAttribute`1";
    public const string InjectAttribute = Namespace + ".InjectAttribute";
    public const string ServiceTypeAttribute = Namespace + ".ServiceTypeAttribute`1";
    public const string Singleton = Namespace + ".Singleton";
    public const string FarsightStartup = StartupNamespace + ".FarsightStartup";

    public const string SectionNameProperty = "SectionName";
    public const string ErrorOnUnknownConfigurationProperty = "ErrorOnUnknownConfiguration";
    public const string BindNonPublicPropertiesProperty = "BindNonPublicProperties";

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
