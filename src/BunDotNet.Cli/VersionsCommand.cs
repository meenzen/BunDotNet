using System.CommandLine;
using Spectre.Console;

namespace BunDotNet.Cli;

public static class VersionsCommand
{
    public static Command Create()
    {
        var path = CommonOptions.Path();
        var command = new Command("versions", "Lists installed Bun versions.") { path };
        command.SetAction(
            async (parseResult, cancellationToken) =>
            {
                var versions = await BunInstaller.ListVersionsAsync(parseResult.GetValue(path), cancellationToken);
                foreach (var version in versions.OrderBy(x => x.Metadata.Version))
                {
                    AnsiConsole.WriteLine(version.Metadata.Version.ToString());
                }

                return 0;
            }
        );
        return command;
    }
}
