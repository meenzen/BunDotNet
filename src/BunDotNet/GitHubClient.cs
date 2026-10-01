using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BunDotNet;

[SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly")]
internal class GitHubClient : IDisposable
{
    private const string ReleaseTagPath = "/releases/tag/";

    private readonly HttpClient _apiClient;
    private readonly HttpClient _webClient;
    private readonly string? _token;

    /// <summary>
    /// Creates a client for the GitHub API.
    /// </summary>
    /// <param name="token">The token used to authenticate API requests. If null, requests are unauthenticated.</param>
    /// <param name="handler">The handler used for all requests, intended for testing. It must not follow redirects.</param>
    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded")]
    public GitHubClient(string? token = null, HttpMessageHandler? handler = null)
    {
        _token = token;
        _apiClient = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
        _apiClient.BaseAddress = new("https://api.github.com/");
        _apiClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json")
        );
        _apiClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        _apiClient.DefaultRequestHeaders.UserAgent.Add(new("BunDotNet", ThisAssembly.AssemblyInformationalVersion));

        // The website is used as a fallback when the API is rate limited, the latest release redirects to its tag
        _webClient = handler is null
            ? new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            : new HttpClient(handler, disposeHandler: false);
        _webClient.BaseAddress = new("https://github.com/");
        _webClient.DefaultRequestHeaders.UserAgent.Add(new("BunDotNet", ThisAssembly.AssemblyInformationalVersion));
    }

    public async Task<string> GetLatestReleaseTagAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken = default
    )
    {
        var path = $"/repos/{owner}/{repo}/releases/latest";
        var response = await SendApiRequestAsync(path, _token, cancellationToken);

        // An invalid or expired token should not prevent the request, public data is available without one
        if (response.StatusCode == HttpStatusCode.Unauthorized && _token is not null)
        {
            response.Dispose();
            response = await SendApiRequestAsync(path, token: null, cancellationToken);
        }

        using (response)
        {
            if (IsRateLimited(response))
            {
                return await GetLatestReleaseTagFromWebAsync(owner, repo, cancellationToken);
            }

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("tag_name", out var tagNameElement))
            {
                return tagNameElement.GetString() ?? throw new InvalidOperationException("Tag name is null.");
            }

            throw new InvalidOperationException("Tag name not found in the response.");
        }
    }

    private async Task<HttpResponseMessage> SendApiRequestAsync(
        string path,
        string? token,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await _apiClient.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// GitHub responds with 403 or 429 when the primary or secondary rate limit is exceeded.
    /// See https://docs.github.com/en/rest/using-the-rest-api/troubleshooting-the-rest-api#rate-limit-errors
    /// </summary>
    internal static bool IsRateLimited(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return true;
        }

        if (response.StatusCode != HttpStatusCode.Forbidden)
        {
            return false;
        }

        if (response.Headers.RetryAfter is not null)
        {
            return true;
        }

        return response.Headers.TryGetValues("x-ratelimit-remaining", out var values) && values.Contains("0");
    }

    /// <summary>
    /// Resolves the latest release tag using the redirect of https://github.com/{owner}/{repo}/releases/latest,
    /// which is not subject to the API rate limit.
    /// </summary>
    private async Task<string> GetLatestReleaseTagFromWebAsync(
        string owner,
        string repo,
        CancellationToken cancellationToken
    )
    {
        using var request = new HttpRequestMessage(HttpMethod.Head, $"/{owner}/{repo}/releases/latest");
        using var response = await _webClient.SendAsync(request, cancellationToken);

        var location = response.Headers.Location?.OriginalString;
        var index = location?.IndexOf(ReleaseTagPath, StringComparison.Ordinal) ?? -1;
        if (index < 0)
        {
            throw new HttpRequestException(
                "The GitHub API rate limit was exceeded and the latest release could not be determined from the "
                    + $"website (status {(int)response.StatusCode}). Provide a GitHub token to raise the rate limit.",
                inner: null,
                response.StatusCode
            );
        }

        var tag = location![(index + ReleaseTagPath.Length)..];
        var end = tag.IndexOfAny(['?', '#']);
        if (end >= 0)
        {
            tag = tag[..end];
        }

        tag = tag.TrimEnd('/');
        return Uri.UnescapeDataString(tag);
    }

    public void Dispose()
    {
        _apiClient.Dispose();
        _webClient.Dispose();
    }
}
