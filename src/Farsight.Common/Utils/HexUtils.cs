namespace Farsight.Common.Utils;

/// <summary>
/// Provides helpers for working with hexadecimal string representations.
/// </summary>
public static class HexUtils
{
    /// <summary>
    /// Converts the provided bytes to an uppercase hexadecimal string prefixed with <c>0x</c>.
    /// </summary>
    /// <param name="data">The bytes to encode as a hexadecimal string.</param>
    /// <returns>A <c>0x</c>-prefixed hexadecimal representation of <paramref name="data"/>.</returns>
    public static string ToPrefixedHexString(ReadOnlySpan<byte> data)
        => String.Create((data.Length * 2) + 2, data, (span, state) =>
        {
            span[0] = '0';
            span[1] = 'x';
            if(!Convert.TryToHexString(state, span[2..], out _))
            {
                throw new InvalidOperationException("Failed to write hex string.");
            }
        });

    /// <summary>
    /// Converts the provided bytes to a lowercase hexadecimal string prefixed with <c>0x</c>.
    /// </summary>
    /// <param name="data">The bytes to encode as a hexadecimal string.</param>
    /// <returns>A lowercase <c>0x</c>-prefixed hexadecimal representation of <paramref name="data"/>.</returns>
    public static string ToLowerPrefixedHexString(ReadOnlySpan<byte> data)
        => String.Create((data.Length * 2) + 2, data, (span, state) =>
        {
            span[0] = '0';
            span[1] = 'x';
            if(!Convert.TryToHexStringLower(state, span[2..], out _))
            {
                throw new InvalidOperationException("Failed to write hex string.");
            }
        });
}
