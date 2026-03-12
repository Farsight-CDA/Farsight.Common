namespace Farsight.Common;

/// <summary>
/// Represents an exception that is thrown in code paths that are supposed to be unreachable.
/// </summary>
public sealed class ImpossibleException() : Exception("Unreachable code path called")
{
}
