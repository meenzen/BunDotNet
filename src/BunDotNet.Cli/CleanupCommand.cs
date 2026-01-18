using Humanizer;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace BunDotNet.Cli;

[Description("Removes all Bun versions except the latest one.")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class CleanupCommand : AsyncCommand<CleanupCommand.Settings>
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
        await AnsiConsole
            .Status()
            .StartAsync(
                "Cleaning up old Bun versions...",
                async ctx =>
                {
                    var result = await BunInstaller.CleanupAsync(settings.Path, cancellationToken);
                    var size = result.RemovedVersions.Sum(v => v.Metadata.SizeBytes);
                    AnsiConsole.MarkupLine($"[green]Removed {result.RemovedVersions.Count} old Bun versions.[/]");
                    AnsiConsole.WriteLine($"{size.Bytes().Humanize()} of disk space freed.");
                }
            );
        return 0;
    }
}
