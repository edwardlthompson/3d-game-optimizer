using System.Text.RegularExpressions;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

internal static partial class PathRedactor
{
    public static string Redact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var redacted = value;
        var user = Environment.UserName;
        if (!string.IsNullOrWhiteSpace(user))
        {
            redacted = redacted.Replace(user, "REDACTED_USER", StringComparison.OrdinalIgnoreCase);
        }

        return UsersPathRegex().Replace(redacted, @"Users\REDACTED_USER");
    }

    [GeneratedRegex(@"Users\\[^\\]+", RegexOptions.IgnoreCase)]
    private static partial Regex UsersPathRegex();
}
