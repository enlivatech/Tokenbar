using System.Text.RegularExpressions;
using Tokenbar.Models;

namespace Tokenbar.Services;

/// <summary>
/// Turns the CLI's raw <c>login_method</c> (e.g. "Claude (default_claude_max_5x)", "ChatGPT Plus")
/// into the short plan name CodexBar shows in the card header ("Max 5x", "Plus").
/// </summary>
internal static partial class PlanLabel
{
    public static string? Format(ProviderId provider, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var text = raw.Trim();

        var tier = TierPattern().Match(text);
        if (tier.Success)
        {
            var code = tier.Groups[1].Value.ToLowerInvariant();
            return code switch
            {
                _ when code.Contains("max_20x") => "Max 20x",
                _ when code.Contains("max_5x") => "Max 5x",
                _ when code.Contains("max") => "Max",
                _ when code.Contains("team") => "Team",
                _ when code.Contains("enterprise") => "Enterprise",
                "default_claude_ai" or "default_claude_pro" => "Pro",
                _ => null,
            };
        }

        foreach (var prefix in new[] { "ChatGPT ", "Claude ", "Cursor ", "Gemini ", "OpenCode ", "Grok ", "SuperGrok " })
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && text.Length > prefix.Length)
                return text[prefix.Length..].Trim();
        }

        // Anything that still looks like an internal identifier isn't worth showing.
        return text.Contains('_') ? null : text;
    }

    [GeneratedRegex(@"\(([a-z0-9_]+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex TierPattern();
}
