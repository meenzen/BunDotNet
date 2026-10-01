[![GitHub](https://img.shields.io/github/license/meenzen/BunDotNet.svg)](https://github.com/meenzen/BunDotNet/blob/main/LICENSE)
[![NuGet Version](https://img.shields.io/nuget/v/BunDotNet.svg)](https://www.nuget.org/packages/BunDotNet)
[![NuGet Downloads (CLI)](https://img.shields.io/nuget/dt/BunDotNet.Cli.svg?label=Downloads%20(CLI))](https://www.nuget.org/packages/BunDotNet.Cli)
[![NuGet Downloads (Library)](https://img.shields.io/nuget/dt/BunDotNet.svg?label=Downloads%20(Library))](https://www.nuget.org/packages/BunDotNet)
[![codecov](https://codecov.io/gh/meenzen/BunDotNet/graph/badge.svg?token=90oSB72YQI)](https://codecov.io/gh/meenzen/BunDotNet)

# BunDotNet

Tools that help integrate Bun, A fast all-in-one JavaScript runtime, into your .NET projects.

## Library

Install the NuGet package:

```bash
dotnet add package BunDotNet
```

Use it like this:

```csharp
using BunDotNet;

// install the latest version of Bun
var runtime = await BunInstaller.InstallAsync();

// or install a specific version
var runtime = await BunInstaller.InstallAsync(version: BunVersion.Parse("1.3.6"));

// or install the latest canary build
var runtime = await BunInstaller.InstallAsync(version: BunVersion.Canary);

// then use the bun cli to run a script
await runtime.RunAsync(args: ["run", "script.ts"], workingDirectory: Environment.CurrentDirectory);

// or list the installed versions
var versions = await BunInstaller.ListVersionsAsync();
```

## CLI Tool Usage

This is a .NET CLI tool that wraps the Bun CLI. It automatically sets up the Bun for you. The only requirement is
.NET 10.

Run a TypeScript or JavaScript file using Bun from the command line:

```bash
dotnet tool exec BunDotNet.Cli -- wrapper -- run script.ts
```

Do you need a specific version of Bun? No problem:

```bash
dotnet tool exec BunDotNet.Cli -- wrapper --version 1.3.6 -- run script.ts
```

Want to try the latest canary build? Use the special version `canary`:

```bash
dotnet tool exec BunDotNet.Cli -- wrapper --version canary -- run script.ts
```

More commands and options can be found by running:

```bash
dotnet tool exec BunDotNet.Cli -- --help
```

You can also install the CLI tool in your project:

```bash
dotnet tool install BunDotNet.Cli
```

Then run it like this:

```bash
dotnet bun wrapper -- run script.ts
```

## Canary Builds

Bun publishes [canary builds](https://github.com/oven-sh/bun/releases/tag/canary) from its main branch. They are
supported as a separate release channel:

- Use `BunVersion.Canary` in the library, or `--version canary` in the CLI. `BunVersion.Parse("canary")` works too.
- An installed canary build is refreshed when it is older than 24 hours. If the refresh fails (e.g. offline), the
  installed build is used.
- Only one canary build is kept. Refreshing replaces the previous one.
- `latest` (or no version) always resolves to the latest stable version, never to canary.
- Cleanup keeps the latest stable version and the canary build.
- Force a refresh with `BunInstaller.UpgradeCanaryAsync()` or `dotnet bun upgrade --canary`.
- Canary builds are downloaded directly and do not use the GitHub API, so they are not affected by rate limits.

## GitHub API Rate Limits

BunDotNet uses the GitHub API to find the latest Bun release. Unauthenticated requests are limited to 60 per hour per
IP address, which is easy to hit on shared CI runners. BunDotNet handles this for you:

1. If a GitHub token is available, it is used to authenticate the API request, which raises the limit to 5000
   requests per hour.
2. If the token is rejected (e.g. it expired), the request is retried without it.
3. If the API is still rate limited, the latest version is looked up on `https://github.com/oven-sh/bun/releases/latest`
   instead, which is not subject to the API rate limit.

Downloads of Bun itself are never rate limited by the API, and installing a specific version does not query the API at
all. The token is only sent to `api.github.com` and does not need any permissions, since Bun releases are public.

### Token Discovery

The first token found is used, in this order:

| Source                                   | Example                                                              |
|------------------------------------------|----------------------------------------------------------------------|
| Explicit parameter or CLI option         | `BunInstaller.InstallAsync(gitHubToken: "...")`, `--github-token ...` |
| `BUNDOTNET_GITHUB_TOKEN` env variable    | A token used only by BunDotNet                                       |
| `GH_TOKEN` env variable                  | The variable used by the GitHub CLI                                  |
| `GITHUB_TOKEN` env variable              | The GitHub Actions workflow token                                    |
| GitHub CLI login                         | Run `gh auth login` once, BunDotNet calls `gh auth token`            |

Prefer environment variables over `--github-token`, since command line arguments are visible to other processes.

Set `BUNDOTNET_GITHUB_AUTH=0` to disable automatic discovery. Only a token passed explicitly is used then.

### GitHub Actions

The workflow token is not exposed to processes by default, map it to an environment variable:

```yaml
- run: dotnet bun wrapper -- run script.ts
  env:
    GITHUB_TOKEN: ${{ github.token }}
```

## Contributing

Pull requests are welcome. Please use [Conventional Commits](https://www.conventionalcommits.org/) to keep
commit messages consistent.

## Acknowledgements

- [Bun](https://bun.com/) is an amazing project. This would not be possible without it.

## License

Distributed under the [MIT License](https://choosealicense.com/licenses/mit/). See `LICENSE` for more information.
