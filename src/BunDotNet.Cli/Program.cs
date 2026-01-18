using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using BunDotNet.Cli;
using Spectre.Console.Cli;

return await RunAppAsync(args);

[UnconditionalSuppressMessage(
    "Trimming",
    "IL3050",
    Justification = "Spectre.Console.Cli uses reflection but has been tested to work with Native AOT"
)]
[UnconditionalSuppressMessage(
    "Trimming",
    "IL2026",
    Justification = "All command types and settings are preserved"
)]
static Task<int> RunAppAsync(string[] args)
{
    var app = new CommandApp();
    app.Configure(config =>
    {
        config.SetApplicationCulture(CultureInfo.InvariantCulture);
        config.SetApplicationVersion(ThisAssembly.AssemblyInformationalVersion);

        config.AddCommand<WrapperCommand>("wrapper");
        config.AddCommand<UpgradeCommand>("upgrade");
        config.AddCommand<VersionsCommand>("versions");
        config.AddCommand<CleanupCommand>("cleanup");

        config.AddExample("wrapper -- install");
        config.AddExample("wrapper -- run ./script.ts");
        config.AddExample("wrapper --version 1.3.6 -- run ./script.ts");
    });

    return app.RunAsync(args);
}
