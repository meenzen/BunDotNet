using System.CommandLine;
using Spectre.Console;

namespace BunDotNet.Cli;

public static class WrapperCommand
{
    public static Command Create()
    {
        var path = CommonOptions.Path();
        var gitHubToken = CommonOptions.GitHubToken();
        var version = new Option<BunVersion?>("--version", "-v")
        {
            Description =
                "The Bun version to use. Use 'latest' for the latest stable version or 'canary' for the latest canary "
                + "build.",
            HelpName = "version",
            DefaultValueFactory = _ => null,
            CustomParser = result =>
            {
                var value = result.Tokens.Count > 0 ? result.Tokens[0].Value : null;
                try
                {
                    return BunVersion.Parse(value);
                }
                catch (FormatException e)
                {
                    result.AddError($"Invalid version '{value}': {e.Message}");
                    return null;
                }
            },
        };
        var silent = new Option<bool>("--silent") { Description = "Less verbose output." };
        var bunArgs = new Argument<string[]>("bun-args")
        {
            Description = "The arguments passed to Bun. Separate them with '--' to pass options.",
            Arity = ArgumentArity.ZeroOrMore,
        };
        var command = new Command("wrapper", "Sets up Bun and executes the specified command.")
        {
            path,
            gitHubToken,
            version,
            silent,
            bunArgs,
        };
        command.SetAction(
            async (parseResult, cancellationToken) =>
            {
                var isSilent = parseResult.GetValue(silent);
                var args = parseResult.GetValue(bunArgs) ?? [];
                if (!isSilent)
                {
                    AnsiConsole.Write(new FigletText("BunDotNet").Color(Color.DarkCyan));
                }

                if (args.Length > 0 && args[0].Equals("upgrade", StringComparison.InvariantCultureIgnoreCase))
                {
                    Console.WriteLine("The 'bun upgrade' command is not supported when using the BunDotNet wrapper.");
                    return 1;
                }

                var bunVersion = parseResult.GetValue(version);
                var installPath = parseResult.GetValue(path);
                var token = parseResult.GetValue(gitHubToken);
                var runtime = isSilent switch
                {
                    true => await BunInstaller.InstallAsync(
                        version: bunVersion,
                        path: installPath,
                        gitHubToken: token,
                        cancellationToken: cancellationToken
                    ),
                    false => await ProgressBar.RunAsync(onProgress =>
                        BunInstaller.InstallAsync(
                            version: bunVersion,
                            path: installPath,
                            onProgress,
                            token,
                            cancellationToken
                        )
                    ),
                };

                if (!isSilent)
                {
                    AnsiConsole.MarkupLine($"[green]Wrapper: Executing Bun {runtime.Metadata.Version}[/]");
                    AnsiConsole.WriteLine();
                }

                return await runtime.RunAsync(
                    args: args,
                    workingDirectory: Environment.CurrentDirectory,
                    cancellationToken: cancellationToken
                );
            }
        );
        return command;
    }
}
