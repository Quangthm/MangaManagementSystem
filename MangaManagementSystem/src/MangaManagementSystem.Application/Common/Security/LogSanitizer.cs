using System.Text;

namespace MangaManagementSystem.Application.Common.Security;

public static class LogSanitizer
{
    private const int DefaultMaxLength = 256;
    private const string EmptyPlaceholder = "(empty)";

    public static string Sanitize(string? value, int maxLength = DefaultMaxLength)
    {
        if (string.IsNullOrEmpty(value))
        {
            return EmptyPlaceholder;
        }

        var trimmed = value.Trim();

        if (trimmed.Length == 0)
        {
            return EmptyPlaceholder;
        }

        var sanitized = new StringBuilder(trimmed.Length);

        foreach (var ch in trimmed)
        {
            if (ch is '\r' or '\n' or '\t')
            {
                sanitized.Append(' ');
            }
            else if (char.IsControl(ch))
            {
                sanitized.Append(' ');
            }
            else
            {
                sanitized.Append(ch);
            }
        }

        var result = sanitized.ToString().Trim();

        if (result.Length == 0)
        {
            return EmptyPlaceholder;
        }

        return result.Length <= maxLength ? result : result[..maxLength];
    }
}
