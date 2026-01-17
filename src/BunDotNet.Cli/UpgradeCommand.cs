using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace BunDotNet.Cli;

[Description("Upgrades Bun to the latest version.")]
public class UpgradeCommand : AsyncCommand<UpgradeCommand.Settings>
{
    [SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
    public class Settings : PathSettings { }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        await BunInstaller.UpgradeAsync(settings.Path, cancellationToken);
        return 0;
    }
}
