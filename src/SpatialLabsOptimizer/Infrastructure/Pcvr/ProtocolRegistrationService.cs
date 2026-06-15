using Microsoft.Win32;

namespace SpatialLabsOptimizer.Infrastructure.Pcvr;

public sealed class ProtocolRegistrationService
{
    private const string ProtocolName = "3dgo";

    public string ExecutablePath { get; }

    public ProtocolRegistrationService()
    {
        ExecutablePath = Environment.ProcessPath
            ?? Path.Combine(AppContext.BaseDirectory, "SpatialLabsOptimizer.exe");
    }

    public bool IsRegistered()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ProtocolName}");
            return key?.GetValue("URL Protocol") is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool Register()
    {
        try
        {
            using var protocolKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProtocolName}");
            protocolKey.SetValue("", $"URL:{ProtocolName} Protocol");
            protocolKey.SetValue("URL Protocol", "");

            using var defaultIcon = protocolKey.CreateSubKey("DefaultIcon");
            defaultIcon.SetValue("", $"\"{ExecutablePath}\",1");

            using var commandKey = protocolKey.CreateSubKey(@"shell\open\command");
            commandKey.SetValue("", $"\"{ExecutablePath}\" \"%1\"");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool Unregister()
    {
        try
        {
            Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{ProtocolName}", throwOnMissingSubKey: false);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static bool TryParsePlayUri(string? uri, out int appId)
    {
        appId = 0;
        if (string.IsNullOrWhiteSpace(uri))
        {
            return false;
        }

        const string prefix = "3dgo://play/";
        if (uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            var segment = uri[prefix.Length..].TrimEnd('/');
            var queryIndex = segment.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex >= 0)
            {
                segment = segment[..queryIndex];
            }

            return int.TryParse(segment, out appId) && appId > 0;
        }

        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed))
        {
            return false;
        }

        if (!string.Equals(parsed.Scheme, ProtocolName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(parsed.Host, "play", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var pathSegment = parsed.AbsolutePath.Trim('/');
        return int.TryParse(pathSegment, out appId) && appId > 0;
    }

    public static string? FindProtocolUriInCommandLine()
    {
        foreach (var arg in Environment.GetCommandLineArgs())
        {
            if (arg.StartsWith($"{ProtocolName}://", StringComparison.OrdinalIgnoreCase))
            {
                return arg;
            }
        }

        return null;
    }
}
