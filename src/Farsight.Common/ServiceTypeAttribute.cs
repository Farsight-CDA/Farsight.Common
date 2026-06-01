namespace Farsight.Common;

/// <summary>
/// Registers a generated service under an additional service interface type.
/// </summary>
/// <typeparam name="TService">The interface service type exposed in DI.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class ServiceTypeAttribute<TService> : Attribute
    where TService : class
{
}
