using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BunDotNet.Cli;

[Description("Sets up Bun and ececutes the specified command.")]
public class WrapperCommand : AsyncCommand<WrapperCommand.Settings>
{
    public class Settings : PathSettings
    {
        [CommandOption("-v|--version")]
        [Description("The Bun version to use.")]
        [DefaultValue("latest")]
        public string Version { get; init; } = "latest";

        [CommandOption("--silent")]
        [Description("Less verbose output.")]
        [DefaultValue(false)]
        public bool Silent { get; init; }
    }

    private BunVersion? _version;

    public override ValidationResult Validate(CommandContext context, Settings settings)
    {
        try
        {
            _version = BunVersion.Parse(settings.Version);
        }
        catch (Exception e)
        {
            return ValidationResult.Error($"Invalid version '{settings.Version}': {e.Message}");
        }

        return ValidationResult.Success();
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        if (!settings.Silent)
        {
            AnsiConsole.Write(new FigletText("BunDotNet").Color(Color.DarkCyan));
        }

        if (
            context.Remaining.Raw.Any()
            && context.Remaining.Raw[0].Equals("upgrade", StringComparison.InvariantCultureIgnoreCase)
        )
        {
            Console.WriteLine("The 'bun upgrade' command is not supported when using the BunDotNet wrapper.");
            return 1;
        }

        var runtime = settings.Silent switch
        {
            true => await BunInstaller.InstallAsync(
                version: _version,
                path: settings.Path,
                cancellationToken: cancellationToken
            ),
            false => await ProgressBar.RunAsync(onProgress =>
                BunInstaller.InstallAsync(version: _version, path: settings.Path, onProgress, cancellationToken)
            ),
        };

        if (!settings.Silent)
        {
            AnsiConsole.MarkupLine($"[green]Wrapper: Executing Bun {runtime.Metadata.Version}[/]");
            AnsiConsole.WriteLine();
        }

        await runtime.RunAsync(
            args: context.Remaining.Raw.ToArray(),
            workingDirectory: Environment.CurrentDirectory,
            cancellationToken: cancellationToken
        );
        return 0;
    }
}
