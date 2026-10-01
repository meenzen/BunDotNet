using System.CommandLine;
using Humanizer;
using Spectre.Console;

namespace BunDotNet.Cli;

public static class CleanupCommand
{
    public static Command Create()
    {
        var path = CommonOptions.Path();
        var command = new Command(
            "cleanup",
            "Removes all Bun versions except the latest stable one and the canary build."
        )
        {
            path,
        };
        command.SetAction(
            async (parseResult, cancellationToken) =>
            {
                await AnsiConsole
                    .Status()
                    .StartAsync(
                        "Cleaning up old Bun versions...",
                        async _ =>
                        {
                            var result = await BunInstaller.CleanupAsync(parseResult.GetValue(path), cancellationToken);
                            var size = result.RemovedVersions.Sum(v => v.Metadata.SizeBytes);
                            AnsiConsole.MarkupLine(
                                $"[green]Removed {result.RemovedVersions.Count} old Bun versions.[/]"
                            );
                            AnsiConsole.WriteLine($"{size.Bytes().Humanize()} of disk space freed.");
                        }
                    );
                return 0;
            }
        );
        return command;
    }
}
