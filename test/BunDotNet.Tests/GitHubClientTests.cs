using System.Net;
using System.Text;

namespace BunDotNet.Tests;

public class GitHubClientTests
{
    private const string ApiPath = "/repos/oven-sh/bun/releases/latest";
    private const string WebPath = "/oven-sh/bun/releases/latest";

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpMethod Method, Uri Uri, string? Authorization)> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            Requests.Add((request.Method, request.RequestUri!, request.Headers.Authorization?.ToString()));
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Release(string tag) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent($$"""{"tag_name":"{{tag}}"}""", Encoding.UTF8, "application/json"),
        };

    private static HttpResponseMessage RateLimited()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        response.Headers.Add("x-ratelimit-remaining", "0");
        return response;
    }

    private static HttpResponseMessage Redirect(string location) =>
        new(HttpStatusCode.Found) { Headers = { Location = new Uri(location) } };

    private static async Task<string> GetLatestAsync(StubHandler handler, string? token)
    {
        using var client = new GitHubClient(token, handler);
        return await client.GetLatestReleaseTagAsync("oven-sh", "bun");
    }

    [Test]
    public async Task GetLatest_WithToken_SendsBearerToken()
    {
        var handler = new StubHandler(_ => Release("bun-v1.3.6"));

        var tag = await GetLatestAsync(handler, "secret");

        await Assert.That(tag).IsEqualTo("bun-v1.3.6");
        await Assert.That(handler.Requests.Count).IsEqualTo(1);
        await Assert.That(handler.Requests[0].Uri.AbsolutePath).IsEqualTo(ApiPath);
        await Assert.That(handler.Requests[0].Authorization).IsEqualTo("Bearer secret");
    }

    [Test]
    public async Task GetLatest_WithoutToken_SendsNoAuthorization()
    {
        var handler = new StubHandler(_ => Release("bun-v1.3.6"));

        await GetLatestAsync(handler, token: null);

        await Assert.That(handler.Requests[0].Authorization).IsNull();
    }

    [Test]
    public async Task GetLatest_InvalidToken_RetriesWithoutToken()
    {
        var handler = new StubHandler(request =>
            request.Headers.Authorization is null
                ? Release("bun-v1.3.6")
                : new HttpResponseMessage(HttpStatusCode.Unauthorized)
        );

        var tag = await GetLatestAsync(handler, "expired");

        await Assert.That(tag).IsEqualTo("bun-v1.3.6");
        await Assert.That(handler.Requests.Count).IsEqualTo(2);
        await Assert.That(handler.Requests[1].Authorization).IsNull();
    }

    [Test]
    public async Task GetLatest_RateLimited_FallsBackToWebsite()
    {
        var handler = new StubHandler(request =>
            request.RequestUri!.AbsolutePath == ApiPath
                ? RateLimited()
                : Redirect("https://github.com/oven-sh/bun/releases/tag/bun-v1.3.6")
        );

        var tag = await GetLatestAsync(handler, "secret");

        await Assert.That(tag).IsEqualTo("bun-v1.3.6");
        await Assert.That(handler.Requests.Count).IsEqualTo(2);
        var web = handler.Requests[1];
        await Assert.That(web.Uri.Host).IsEqualTo("github.com");
        await Assert.That(web.Uri.AbsolutePath).IsEqualTo(WebPath);
        await Assert.That(web.Method).IsEqualTo(HttpMethod.Head);
        // the token is only meant for the API
        await Assert.That(web.Authorization).IsNull();
    }

    [Test]
    public async Task GetLatest_SecondaryRateLimit_FallsBackToWebsite()
    {
        var handler = new StubHandler(request =>
            request.RequestUri!.AbsolutePath == ApiPath
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                : Redirect("https://github.com/oven-sh/bun/releases/tag/bun-v1.3.6?ref=latest")
        );

        var tag = await GetLatestAsync(handler, token: null);

        await Assert.That(tag).IsEqualTo("bun-v1.3.6");
    }

    [Test]
    public async Task GetLatest_RateLimitedAndWebsiteUnavailable_Throws()
    {
        var handler = new StubHandler(request =>
            request.RequestUri!.AbsolutePath == ApiPath
                ? RateLimited()
                : new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        );

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => GetLatestAsync(handler, token: null));

        await Assert.That(exception!.Message).Contains("rate limit");
    }

    [Test]
    public async Task GetLatest_ForbiddenWithoutRateLimit_Throws()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden));

        await Assert.ThrowsAsync<HttpRequestException>(() => GetLatestAsync(handler, token: null));

        await Assert.That(handler.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task IsRateLimited_ForbiddenWithRetryAfter_ReturnsTrue()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        response.Headers.RetryAfter = new(TimeSpan.FromSeconds(60));

        await Assert.That(GitHubClient.IsRateLimited(response)).IsTrue();
    }

    [Test]
    public async Task IsRateLimited_ForbiddenWithRemainingRequests_ReturnsFalse()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Forbidden);
        response.Headers.Add("x-ratelimit-remaining", "42");

        await Assert.That(GitHubClient.IsRateLimited(response)).IsFalse();
    }
}
