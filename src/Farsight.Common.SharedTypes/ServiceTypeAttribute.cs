namespace Farsight.Common;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ServiceTypeAttribute<TService> : Attribute
    where TService : class
{
}
