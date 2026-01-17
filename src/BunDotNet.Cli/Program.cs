using System.Globalization;
using BunDotNet.Cli.Features.Cleanup;
using BunDotNet.Cli.Features.Upgrade;
using BunDotNet.Cli.Features.Versions;
using BunDotNet.Cli.Features.Wrapper;
using Spectre.Console.Cli;

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

return await app.RunAsync(args);
