using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using BunDotNet.Cli.Features.Wrapper;
using Spectre.Console.Cli;

namespace BunDotNet.Cli.Features.Upgrade;

[Description("Upgrades Bun to the latest version.")]
public class UpgradeCommand : Command<UpgradeCommand.Settings>
{
    [SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
    public class Settings : DirectorySettings { }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        Console.WriteLine("todo: implement upgrade command");
        return 0;
    }
}
