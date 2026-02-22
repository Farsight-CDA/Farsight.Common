namespace Farsight.Common;

/// <summary>
/// Registers a singleton under an additional service interface type.
/// </summary>
/// <typeparam name="TService">The interface service type exposed in DI.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ServiceTypeAttribute<TService> : Attribute
    where TService : class
{
}
