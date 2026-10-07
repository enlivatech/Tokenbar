using System.Text.Json;

namespace Tokenbar.Services;

public sealed record CredentialStatus(string Provider, bool AcceptsApiKey, bool HasApiKey, bool HasCookie);

/// <summary>Reads and writes provider credentials through <c>codexbar config</c>.</summary>
public static class CliConfig
{
    public static async Task<Dictionary<string, CredentialStatus>> GetCredentialsAsync()
    {
        var result = await CliRunner.RunAsync(["config", "credentials", "--json"]);
        if (!result.Ok) throw new InvalidOperationException(result.Message);
        using var doc = JsonDocument.Parse(result.Stdout);
        return doc.RootElement.EnumerateArray().Select(e => new CredentialStatus(
                e.GetProperty("provider").GetString()!,
                e.GetProperty("acceptsApiKey").GetBoolean(),
                e.GetProperty("hasApiKey").GetBoolean(),
                e.GetProperty("hasCookie").GetBoolean()))
            .ToDictionary(s => s.Provider);
    }

    public static Task<CliResult> SetApiKeyAsync(string provider, string key) =>
        CliRunner.RunAsync(["config", "set-api-key", provider, "--stdin"], stdin: key);

    public static Task<CliResult> RemoveApiKeyAsync(string provider) =>
        CliRunner.RunAsync(["config", "remove-api-key", provider]);

    public static Task<CliResult> SetCookieAsync(string provider, string cookie) =>
        CliRunner.RunAsync(["config", "set-cookie", provider, "--stdin"], stdin: cookie);

    public static Task<CliResult> RemoveCookieAsync(string provider) =>
        CliRunner.RunAsync(["config", "remove-cookie", provider]);

    public static async Task<bool> GetClaudeCredentialsAllowedAsync()
    {
        var result = await CliRunner.RunAsync(["config", "claude-code-credentials", "status", "--json"]);
        using var doc = JsonDocument.Parse(result.Stdout);
        return doc.RootElement.GetProperty("allowed").GetBoolean();
    }

    public static Task<CliResult> SetClaudeCredentialsAllowedAsync(bool allowed) =>
        CliRunner.RunAsync(["config", "claude-code-credentials", allowed ? "allow" : "deny"]);
}
