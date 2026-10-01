using System.ComponentModel;
using Spectre.Console.Cli;

namespace BunDotNet.Cli;

public class GitHubSettings : PathSettings
{
    [CommandOption("--github-token")]
    [Description(
        "The GitHub token used to check for the latest Bun version. Defaults to BUNDOTNET_GITHUB_TOKEN, GH_TOKEN, "
            + "GITHUB_TOKEN or the GitHub CLI login. Set BUNDOTNET_GITHUB_AUTH=0 to disable automatic discovery."
    )]
    public string? GitHubToken { get; init; }
}
