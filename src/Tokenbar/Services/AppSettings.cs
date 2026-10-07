using System.Text.Json;
using Microsoft.Win32;
using Tokenbar.Models;

namespace Tokenbar.Services;

/// <summary>Tokenbar's own preferences; provider secrets live in the CLI's DPAPI-protected config.</summary>
public sealed class AppSettings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tokenbar", "settings.json");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public List<string> EnabledProviders { get; set; } = Providers.All.Select(p => p.CliName).ToList();

    public int RefreshMinutes { get; set; } = 5;

    public string SelectedProvider { get; set; } = "codex";

    public event Action? Changed;

    public bool IsEnabled(ProviderId id) => EnabledProviders.Contains(Providers.Get(id).CliName);

    public void SetEnabled(ProviderId id, bool enabled)
    {
        var name = Providers.Get(id).CliName;
        EnabledProviders.Remove(name);
        if (enabled)
        {
            // Keep the canonical order so tabs don't jump around when toggled.
            EnabledProviders.Add(name);
            EnabledProviders = Providers.All.Select(p => p.CliName).Where(EnabledProviders.Contains).ToList();
        }
        Save();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, Json));
        Changed?.Invoke();
    }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), Json) ?? new AppSettings();
        }
        catch (JsonException) { }
        return new AppSettings();
    }
}

internal static class LaunchAtLogin
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Tokenbar";

    private static string Command => $"\"{Environment.ProcessPath}\"";

    public static bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string v && v.Equals(Command, StringComparison.OrdinalIgnoreCase);
        }
    }

    public static void Set(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey);
        if (enabled) key.SetValue(ValueName, Command);
        else key.DeleteValue(ValueName, throwOnMissingValue: false);
    }
}
