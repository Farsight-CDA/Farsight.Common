namespace Farsight.Common;

/// <summary>
/// Marks a private readonly field for constructor injection in generated code.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class InjectAttribute : Attribute
{
}
