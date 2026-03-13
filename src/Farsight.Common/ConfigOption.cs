namespace Farsight.Common;

/// <summary>
/// Marks a class as a configuration option model to be bound by generated registrations.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ConfigOptionAttribute : Attribute
{
    /// <summary>
    /// Gets or sets an optional configuration section name to bind from.
    /// </summary>
    public string? SectionName { get; set; }

    /// <summary>
    /// Gets or sets whether binding should fail when configuration contains keys that do not map to the target schema.
    /// When omitted, generated registrations enable this automatically for section-bound options and leave root-bound options lenient.
    /// </summary>
    public bool ErrorOnUnknownConfiguration { get; set; }

    /// <summary>
    /// Gets or sets whether non-public properties should participate in configuration binding.
    /// </summary>
    public bool BindNonPublicProperties { get; set; }
}

/// <summary>
/// Marks a class as a configuration option model and associates a FluentValidation validator with it.
/// </summary>
/// <typeparam name="TValidator">The validator type to instantiate during options validation.</typeparam>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ConfigOptionAttribute<TValidator> : ConfigOptionAttribute
    where TValidator : FluentValidation.IValidator, new()
{
}
