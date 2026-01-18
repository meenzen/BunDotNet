using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace BunDotNet.Cli;

[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class PathSettings : CommandSettings
{
    [CommandOption("-p|--path")]
    [Description("The Bun installation directory.")]
    public string? Path { get; init; }
}
