using System.ComponentModel;
using Spectre.Console.Cli;

namespace BunDotNet.Cli.Features.Wrapper;

[Description("Sets up Bun and ececutes the specified command.")]
public class WrapperCommand : Command<WrapperCommand.Settings>
{
    public class Settings : DirectorySettings
    {
        [CommandOption("-v|--version")]
        [Description("The Bun version to use.")]
        [DefaultValue("latest")]
        public string Version { get; init; } = "latest";
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (context.Remaining.Raw.Any() && context.Remaining.Raw[0] == "upgrade")
        {
            Console.WriteLine("The 'bun upgrade' command is not supported when using the Bun wrapper.");
            return 1;
        }

        Console.WriteLine("todo: implement wrapper command");
        return 0;
    }
}
