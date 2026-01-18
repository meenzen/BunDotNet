using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace BunDotNet.Cli;

[Description("Lists installed Bun versions.")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class VersionsCommand : AsyncCommand<VersionsCommand.Settings>
{
    [SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
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
