namespace SpatialLabsOptimizer.Infrastructure;

internal static class StartupDiagnostics
{
    public static string LogDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3d-game-optimizer",
            "logs");

    public static void Trace(string stage)
    {
        WriteToFile("startup-trace.log", stage);
    }

    public static void WriteFailure(string message)
    {
        WriteToFile("startup-failures.log", message);
    }

    private static void WriteToFile(string fileName, string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            var path = Path.Combine(LogDirectory, fileName);
            using var stream = new FileStream(
                path,
                FileMode.Append,
                FileAccess.Write,
                FileShare.ReadWrite);
            using var writer = new StreamWriter(stream);
            writer.WriteLine($"[{DateTimeOffset.Now:u}] {message}");
            writer.Flush();
        }
        catch
        {
            // Last-resort diagnostics must not throw.
        }
    }
}
