namespace Farsight.Common;

/// <summary>
/// Provides helper methods for generated source text formatting.
/// </summary>
public static class CodeUtils
{
    /// <summary>
    /// Indents every non-empty line by the requested number of spaces.
    /// </summary>
    /// <param name="text">The text to indent.</param>
    /// <param name="spaces">The number of leading spaces to apply.</param>
    /// <returns>The indented text.</returns>
    public static string Indent(string text, int spaces)
    {
        string indentation = new string(' ', spaces);
        return String.Join("\n", text.Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(line => String.IsNullOrWhiteSpace(line)
                ? line
                : indentation + line
            )
        );
    }
}
