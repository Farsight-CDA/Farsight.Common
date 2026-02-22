namespace Farsight.Common;

/// <summary>
/// Marks an assembly as containing a Farsight registrar type.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class FarsightRegistrarAttribute<TRegistrar> : Attribute
{
}
