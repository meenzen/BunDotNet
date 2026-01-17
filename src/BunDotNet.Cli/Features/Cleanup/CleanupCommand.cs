using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using BunDotNet.Cli.Features.Wrapper;
using Spectre.Console.Cli;

namespace BunDotNet.Cli.Features.Cleanup;

[Description("Removes all Bun versions except the latest one.")]
public class CleanupCommand : Command<CleanupCommand.Settings>
{
    [SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
    public class Settings : DirectorySettings { }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        Console.WriteLine("todo: implement cleanup command");
        return 0;
    }
}
