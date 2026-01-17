[![GitHub](https://img.shields.io/github/license/meenzen/BunDotNet.svg)](https://github.com/meenzen/BunDotNet/blob/main/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/BunDotNet.svg)](https://www.nuget.org/packages/BunDotNet)
[![NuGet](https://img.shields.io/nuget/dt/BunDotNet.svg)](https://www.nuget.org/packages/BunDotNet)

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

## Contributing

Pull requests are welcome. Please use [Conventional Commits](https://www.conventionalcommits.org/) to keep
commit messages consistent.

## Acknowledgements

- [Bun](https://bun.com/) is an amazing project. This would not be possible without it.

## License

Distributed under the [MIT License](https://choosealicense.com/licenses/mit/). See `LICENSE` for more information.
