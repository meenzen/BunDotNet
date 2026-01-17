namespace BunDotNet;

public sealed class BunCleanupResult
{
    public required IReadOnlyList<BunRuntime> RemovedVersions { get; init; }
}
