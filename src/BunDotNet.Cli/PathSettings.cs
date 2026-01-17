using Spectre.Console.Cli;
using System.ComponentModel;

namespace BunDotNet.Cli;

public class PathSettings : CommandSettings
{
    [CommandOption("-p|--path")]
    [Description("The Bun installation directory.")]
    public string? Path { get; init; }
}
