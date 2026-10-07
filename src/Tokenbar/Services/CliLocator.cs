namespace Tokenbar.Services;

internal static class CliLocator
{
    private const string ExeName = "codexbar.exe";

    /// <summary>Bundled CLI first, then a local vendor build (dev), then PATH.</summary>
    public static string? Find()
    {
        var baseDir = AppContext.BaseDirectory;
        var candidates = new List<string> { Path.Combine(baseDir, "cli", ExeName), Path.Combine(baseDir, ExeName) };

        for (var dir = new DirectoryInfo(baseDir); dir is not null; dir = dir.Parent)
        {
            var vendor = Path.Combine(dir.FullName, "vendor", "Win-CodexBar", "target", "release", ExeName);
            if (File.Exists(vendor)) { candidates.Add(vendor); break; }
        }

        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        candidates.AddRange(path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => Path.Combine(p.Trim(), ExeName)));

        return candidates.FirstOrDefault(File.Exists);
    }
}
