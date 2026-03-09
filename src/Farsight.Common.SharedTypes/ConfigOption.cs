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
