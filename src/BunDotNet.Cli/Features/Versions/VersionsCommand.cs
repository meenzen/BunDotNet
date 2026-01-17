using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using BunDotNet.Cli.Features.Wrapper;
using Spectre.Console.Cli;

namespace BunDotNet.Cli.Features.Versions;

[Description("Lists installed Bun versions.")]
public class VersionsCommand : Command<VersionsCommand.Settings>
{
    [SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty")]
    public class Settings : DirectorySettings { }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        Console.WriteLine("todo: implement version listing");
        return 0;
    }
}
