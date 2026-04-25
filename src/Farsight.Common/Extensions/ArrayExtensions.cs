using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Farsight.Common.Extensions;

/// <summary>
/// Common extensions used in Farsight applications.
/// </summary>
public static partial class Extensions
{
    extension<T>(T[] array)
    {
        /// <summary>
        /// Creates an <see cref="ImmutableArray{T}"/> wrapper around the given array without copying it.
        /// </summary>
        /// <returns>An immutable array backed by the source array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ImmutableArray<T> AsImmutable()
            => ImmutableCollectionsMarshal.AsImmutableArray(array);
    }
}
