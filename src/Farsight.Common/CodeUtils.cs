namespace Farsight.Common;

public static class CodeUtils
{
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
