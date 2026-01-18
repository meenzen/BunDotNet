# Native AOT Support

## BunDotNet Library

The `BunDotNet` library is fully compatible with Native AOT compilation. It uses:
- JSON source generation for serialization (`BunJsonSerializerContext`)
- No reflection or dynamic code generation
- `IsAotCompatible=true` property set in the project file

### Usage with AOT

Add the library to your AOT-enabled project:

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="BunDotNet" Version="..." />
</ItemGroup>
```

The library will work seamlessly with Native AOT compilation.

## BunDotNet.Cli

The CLI tool is configured to support Native AOT publishing when a RuntimeIdentifier is specified:

```bash
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r win-x64
dotnet publish -c Release -r osx-x64
dotnet publish -c Release -r osx-arm64
```

### Known Limitations

The CLI uses `Spectre.Console.Cli` for command-line parsing, which relies on reflection. While the CLI **compiles successfully** with Native AOT, there are runtime limitations due to trimming:

- The trimmer may remove types that Spectre.Console.Cli needs at runtime
- Some reflection-based features may not work correctly
- The tool continues to work perfectly when run as a standard .NET application

### Workaround

For the best experience, use the CLI as a regular .NET tool:

```bash
dotnet tool install -g BunDotNet.Cli
```

Or run it without AOT:

```bash
dotnet run --project src/BunDotNet.Cli/BunDotNet.Cli.csproj
```

### Future Improvements

To fully support Native AOT in the CLI, consider:
1. Replacing Spectre.Console.Cli with an AOT-compatible command-line library (e.g., System.CommandLine)
2. Using source generators for command registration
3. Adding custom type preservation logic for all required Spectre.Console.Cli types

## Testing AOT Compatibility

To verify AOT compatibility:

```bash
# Test the library
dotnet publish YourProject.csproj -c Release -r linux-x64

# Test the CLI (compiles but may have runtime issues)
dotnet publish src/BunDotNet.Cli/BunDotNet.Cli.csproj -c Release -r linux-x64
```

## Build Configuration

The projects are configured so that:
- Regular `dotnet build` works without requiring AOT
- Package generation (`dotnet pack`) works normally
- AOT is only enabled when publishing with a specific runtime identifier
