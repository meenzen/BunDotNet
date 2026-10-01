using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BunDotNet;

/// <summary>
/// Resolves the token used to authenticate GitHub API requests, which raises the rate limit from 60 to 5000 requests
/// per hour.
/// </summary>
internal static class GitHubToken
{
    /// <summary>
    /// Set to <c>0</c>, <c>false</c>, <c>no</c> or <c>off</c> to disable automatic token discovery.
    /// </summary>
    internal const string AuthVariable = "BUNDOTNET_GITHUB_AUTH";

    /// <summary>
    /// Environment variables that are checked for a token, in order of precedence.
    /// </summary>
    internal static readonly string[] TokenVariables = ["BUNDOTNET_GITHUB_TOKEN", "GH_TOKEN", "GITHUB_TOKEN"];

    private static readonly string[] DisabledValues = ["0", "false", "no", "off"];
    private static readonly TimeSpan GhTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Resolves a GitHub token. Precedence: the explicit token, the <see cref="TokenVariables"/> environment
    /// variables, and finally the token of the GitHub CLI (<c>gh auth token</c>).
    /// </summary>
    /// <returns>The token, or null if none is available.</returns>
    internal static async Task<string?> ResolveAsync(
        string? explicitToken,
        Func<string, string?>? getEnvironmentVariable = null,
        Func<CancellationToken, Task<string?>>? getGhToken = null,
        CancellationToken cancellationToken = default
    )
    {
        if (!string.IsNullOrWhiteSpace(explicitToken))
        {
            return explicitToken.Trim();
        }

        getEnvironmentVariable ??= Environment.GetEnvironmentVariable;
        var auth = getEnvironmentVariable(AuthVariable)?.Trim();
        if (DisabledValues.Contains(auth, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        foreach (var variable in TokenVariables)
        {
            var token = getEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token.Trim();
            }
        }

        getGhToken ??= GetGhTokenAsync;
        var ghToken = await getGhToken(cancellationToken);
        return string.IsNullOrWhiteSpace(ghToken) ? null : ghToken.Trim();
    }

    /// <summary>
    /// Gets the token of the GitHub CLI. Returns null if the CLI is not installed, not logged in, or does not respond.
    /// </summary>
    [SuppressMessage(
        "Security",
        "S4036:Searching OS commands in PATH is security-sensitive",
        Justification = "gh is resolved from PATH like in a shell"
    )]
    private static async Task<string?> GetGhTokenAsync(CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "gh",
            ArgumentList = { "auth", "token", "--hostname", "github.com" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        Process process;
        try
        {
            process = Process.Start(startInfo)!;
        }
        catch (Win32Exception)
        {
            // gh is not installed
            return null;
        }

        using (process)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(GhTimeout);
            try
            {
                var outputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
                var errorTask = process.StandardError.ReadToEndAsync(timeout.Token);
                await process.WaitForExitAsync(timeout.Token);
                var output = await outputTask;
                await errorTask;
                return process.ExitCode == 0 ? output.Trim() : null;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                process.Kill(entireProcessTree: true);
                return null;
            }
        }
    }
}
