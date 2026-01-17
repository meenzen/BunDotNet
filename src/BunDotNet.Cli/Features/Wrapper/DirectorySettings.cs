using System.ComponentModel;
using Spectre.Console.Cli;

namespace BunDotNet.Cli.Features.Wrapper;

public class DirectorySettings : CommandSettings
{
    [CommandOption("-d|--directory")]
    [Description("The Bun installation directory.")]
    public string? Directory { get; init; }
}
