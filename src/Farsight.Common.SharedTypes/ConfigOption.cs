namespace Farsight.Common;

/// <summary>
/// Marks a class as a configuration option model to be bound by generated registrations.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ConfigOptionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets an optional configuration section name to bind from.
    /// </summary>
    public string? SectionName { get; set; }
}
