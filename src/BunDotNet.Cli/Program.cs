using System.CommandLine;
using System.CommandLine.Help;
using System.Globalization;
using BunDotNet.Cli;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

// RootCommand is named after the executable, but the tool is invoked as 'bun'
var root = new Command(
    "bun",
    """
    A .NET CLI tool wrapper for the Bun JavaScript runtime.

    Examples:
      bun wrapper -- install
      bun wrapper -- run ./script.ts
      bun wrapper --version 1.3.6 -- run ./script.ts
      bun wrapper --version canary -- run ./script.ts
      bun upgrade --canary
    """
)
{
    WrapperCommand.Create(),
    UpgradeCommand.Create(),
    VersionsCommand.Create(),
    CleanupCommand.Create(),
    new HelpOption(),
    new VersionOption(),
};

// Response files are disabled, Bun arguments like '@types/node' must be passed through unchanged
var configuration = new ParserConfiguration { ResponseFileTokenReplacer = null };

// Signals are handled by the commands, the wrapper must not exit or kill Bun while it is shutting down
var invocation = new InvocationConfiguration { ProcessTerminationTimeout = null };
return await root.Parse(args, configuration).InvokeAsync(invocation);
