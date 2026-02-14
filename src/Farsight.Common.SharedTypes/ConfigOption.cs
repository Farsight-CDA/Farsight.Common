namespace Farsight.Common;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ConfigOptionAttribute : Attribute
{
    public string? SectionName { get; set; }
}
