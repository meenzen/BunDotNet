using System.ComponentModel;
using Spectre.Console.Cli;

namespace BunDotNet.Cli.Features.Wrapper;

public class PathSettings : CommandSettings
{
    [CommandOption("-p|--path")]
    [Description("The Bun installation directory.")]
    public string? Path { get; init; }
}
