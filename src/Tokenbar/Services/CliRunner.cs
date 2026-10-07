using System.Diagnostics;
using System.Text;

namespace Tokenbar.Services;

public sealed record CliResult(int ExitCode, string Stdout, string Stderr)
{
    public bool Ok => ExitCode == 0;

    public string Message => (string.IsNullOrWhiteSpace(Stderr) ? Stdout : Stderr).Trim();
}

/// <summary>Runs the bundled <c>codexbar</c> CLI without a console window.</summary>
public static class CliRunner
{
    public static string? Path { get; set; }

    public static async Task<CliResult> RunAsync(IEnumerable<string> args, string? stdin = null, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        var exe = Path ?? throw new InvalidOperationException("没有找到 codexbar CLI");
        var psi = new ProcessStartInfo(exe)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin is not null,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        psi.ArgumentList.Add("--no-color");

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"无法启动 {exe}");
        if (stdin is not null)
        {
            // Secrets go through stdin so they never appear in the process command line.
            await process.StandardInput.WriteAsync(stdin);
            process.StandardInput.Close();
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout ?? TimeSpan.FromSeconds(30));
        var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
            if (ct.IsCancellationRequested) throw;
            throw new TimeoutException("codexbar CLI 超时");
        }
        return new CliResult(process.ExitCode, await stdoutTask, await stderrTask);
    }
}
