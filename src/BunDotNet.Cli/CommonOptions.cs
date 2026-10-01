using System.CommandLine;

namespace BunDotNet.Cli;

/// <summary>
/// Options shared by multiple commands. Every command gets its own instance.
/// </summary>
public static class CommonOptions
{
    public static Option<string?> Path() => new("--path", "-p") { Description = "The Bun installation directory." };

    public static Option<string?> GitHubToken() =>
        new("--github-token")
        {
            Description =
                "The GitHub token used to check for the latest Bun version. Defaults to BUNDOTNET_GITHUB_TOKEN, "
                + "GH_TOKEN, GITHUB_TOKEN or the GitHub CLI login. Set BUNDOTNET_GITHUB_AUTH=0 to disable automatic "
                + "discovery.",
        };
}
