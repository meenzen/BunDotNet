using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Humanizer;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BunDotNet.Cli;

[Description("Removes all Bun versions except the latest one.")]
public class CleanupCommand : AsyncCommand<CleanupCommand.Settings>
{
    [SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
    public class Settings : PathSettings { }

    protected override async Task<int> ExecuteAsync(
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
