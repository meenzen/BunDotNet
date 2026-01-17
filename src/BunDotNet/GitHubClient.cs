using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace BunDotNet;

[SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly")]
internal class GitHubClient : IDisposable
{
    private readonly HttpClient _httpClient;

    public GitHubClient()
    {
        _httpClient = new HttpClient { BaseAddress = new("https://api.github.com/") };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json")
        );
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new("BunDotNet", ThisAssembly.AssemblyInformationalVersion));
    }

    public async Task<string> GetLatestReleaseTagAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken = default
    )
    {
        var response = await _httpClient.GetAsync($"/repos/{owner}/{repo}/releases/latest", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("tag_name", out var tagNameElement))
        {
            return tagNameElement.GetString() ?? throw new InvalidOperationException("Tag name is null.");
        }

        throw new InvalidOperationException("Tag name not found in the response.");
    }

    public void Dispose() => _httpClient.Dispose();
}
