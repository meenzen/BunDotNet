namespace BunDotNet.Tests;

public class GitHubTokenTests
{
    private static Task<string?> ResolveAsync(
        string? explicitToken,
        Dictionary<string, string> environment,
        string? ghToken = null
    ) =>
        GitHubToken.ResolveAsync(
            explicitToken,
            name => environment.GetValueOrDefault(name),
            _ => Task.FromResult(ghToken)
        );

    [Test]
    public async Task Resolve_ExplicitToken_TakesPrecedence()
    {
        var token = await ResolveAsync("explicit", new() { ["BUNDOTNET_GITHUB_TOKEN"] = "env" }, "gh");

        await Assert.That(token).IsEqualTo("explicit");
    }

    [Test]
    [Arguments("BUNDOTNET_GITHUB_TOKEN", "GH_TOKEN")]
    [Arguments("BUNDOTNET_GITHUB_TOKEN", "GITHUB_TOKEN")]
    [Arguments("GH_TOKEN", "GITHUB_TOKEN")]
    public async Task Resolve_EnvironmentVariables_UsePrecedence(string preferred, string other)
    {
        var token = await ResolveAsync(null, new() { [other] = "other", [preferred] = "preferred" }, "gh");

        await Assert.That(token).IsEqualTo("preferred");
    }

    [Test]
    public async Task Resolve_NoEnvironmentVariable_UsesGhToken()
    {
        var token = await ResolveAsync(null, new() { ["GH_TOKEN"] = "  " }, "gh\n");

        await Assert.That(token).IsEqualTo("gh");
    }

    [Test]
    public async Task Resolve_NothingAvailable_ReturnsNull()
    {
        var token = await ResolveAsync("", [], ghToken: null);

        await Assert.That(token).IsNull();
    }

    [Test]
    [Arguments("0")]
    [Arguments("false")]
    [Arguments("OFF")]
    [Arguments("no")]
    public async Task Resolve_AutomaticDiscoveryDisabled_ReturnsNull(string value)
    {
        var token = await ResolveAsync(
            null,
            new() { ["BUNDOTNET_GITHUB_AUTH"] = value, ["GITHUB_TOKEN"] = "env" },
            "gh"
        );

        await Assert.That(token).IsNull();
    }

    [Test]
    public async Task Resolve_AutomaticDiscoveryDisabled_UsesExplicitToken()
    {
        var token = await ResolveAsync("explicit", new() { ["BUNDOTNET_GITHUB_AUTH"] = "0" });

        await Assert.That(token).IsEqualTo("explicit");
    }

    [Test]
    [Arguments("1")]
    [Arguments("true")]
    public async Task Resolve_AutomaticDiscoveryEnabled_UsesEnvironment(string value)
    {
        var token = await ResolveAsync(null, new() { ["BUNDOTNET_GITHUB_AUTH"] = value, ["GITHUB_TOKEN"] = "env" });

        await Assert.That(token).IsEqualTo("env");
    }
}
