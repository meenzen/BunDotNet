using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

namespace BunDotNet.Cli;

[Description("Sets up Bun and executes the specified command.")]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class WrapperCommand : AsyncCommand<WrapperCommand.Settings>
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class Settings : PathSettings
    {
        [CommandOption("-v|--version")]
        [Description("The Bun version to use.")]
        [DefaultValue("latest")]
        public string Version { get; init; } = "latest";
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
        AnsiConsole.Write(new FigletText("BunDotNet").Color(Color.DarkCyan));

        if (context.Remaining.Raw.Any() && context.Remaining.Raw[0] == "upgrade")
        {
            Console.WriteLine("The 'bun upgrade' command is not supported when using the BunDotNet wrapper.");
            return 1;
        }

        var runtime = await ProgressBar.RunAsync(onProgress =>
            BunInstaller.InstallAsync(version: _version, path: settings.Path, onProgress, cancellationToken)
        );
        AnsiConsole.MarkupLine($"[green]Wrapper: Executing Bun {runtime.Metadata.Version}[/]");
        AnsiConsole.WriteLine();

        await runtime.RunAsync(
            args: context.Remaining.Raw.ToArray(),
            workingDirectory: Environment.CurrentDirectory,
            cancellationToken: cancellationToken
        );
        return 0;
    }
}
