using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using BunDotNet.Cli.Features.Wrapper;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BunDotNet.Cli.Features.Versions;

[Description("Lists installed Bun versions.")]
public class VersionsCommand : AsyncCommand<VersionsCommand.Settings>
{
    [SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
    public class Settings : PathSettings { }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        var versions = await BunInstaller.ListVersionsAsync(settings.Path, cancellationToken);
        foreach (var version in versions.OrderBy(x => x.Metadata.Version))
        {
            AnsiConsole.WriteLine(version.Metadata.Version.ToString());
        }

        return 0;
    }
}
