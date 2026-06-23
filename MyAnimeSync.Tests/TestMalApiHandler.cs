using Jellyfin.Plugin.MyAnimeSync.Api.Mal;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyAnimeSync.Tests.Mocked;

[Collection("SerialTests")]
public class TestMalApiHandler
{
    private readonly string _accessToken;
    private ILogger<TestMalApiHandler> _logger = new NullLogger<TestMalApiHandler>();

    public TestMalApiHandler()
    {
        _accessToken = Environment.GetEnvironmentVariable("MAL_ACCESS_TOKEN")
            ?? throw new InvalidOperationException("MAL_ACCESS_TOKEN is not set.");
    }

    // Test for authCode Url Generation with valid data
    [Fact]
    public void TestAuthCodeUrlGeneration()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;
        userConfig.ClientID = "abc";
        userConfig.ClientSecret = "def";

        string? url = MalApiHandler.GenerateAuthCodeUrl(userConfig);
        Assert.NotNull(url);

        // Try with existing code challenge
        userConfig.CodeChallenge = "challenge";
        url = MalApiHandler.GenerateAuthCodeUrl(userConfig);
        Assert.NotNull(url);
    }

    // Test for authCode Url Generation with polluted data
    [Fact]
    public void TestPollutedAuthCodeUrlGeneration()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;

        string? url = MalApiHandler.GenerateAuthCodeUrl(userConfig);
        Assert.Null(url);

        userConfig.ClientID = "abc\\_";
        userConfig.ClientSecret = "\\_def";

        url = MalApiHandler.GenerateAuthCodeUrl(userConfig);
        Assert.NotNull(url);
    }
}
