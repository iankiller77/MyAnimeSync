using Jellyfin.Plugin.MyAnimeSync.Api.Mal;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using Jellyfin.Plugin.MyAnimeSync.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyAnimeSync.Tests;

public class TestUserListUpdate
{
    private readonly string _accessToken;
    private ILogger<TestUserListUpdate> _logger = new NullLogger<TestUserListUpdate>();

    public TestUserListUpdate()
    {
        _accessToken = Environment.GetEnvironmentVariable("MAL_ACCESS_TOKEN")
            ?? throw new InvalidOperationException("MAL_ACCESS_TOKEN is not set.");
    }

    // Most basic test, retrieve the anime without any season logic.
    [Fact]
    public async Task TestBasicUpdate()
    {
        UserConfig userConfig = new UserConfig();

        userConfig.UserToken = _accessToken;

        UpdateEntry entry = new UpdateEntry("Demon Slayer", "Kimetsu no Yaiba", 8, 1, null);
        bool result = await OnMarkedService.UpdateAnimeList(entry, userConfig, _logger);
        Assert.True(result);
    }

    // Test for generic anime tagged with the proper season.
    [Fact]
    public void TestUpdateWithSeason()
    {
        UserConfig userConfig = new UserConfig();

        userConfig.UserToken = _accessToken;

        int episodeNumber = 8;
        AnimeData? info = OnMarkedService.InternalRetrieveAnimeData("Demon Slayer", ref episodeNumber, 2, userConfig, _logger);
        Assert.NotNull(info);
        Assert.NotNull(info.ID);
        Assert.Equal(47778, info.ID);
    }

    // Test season search using absolute episode.
    // This test also check for anime with multiple sequel type. (movie vs tv serie)
    [Fact]
    public void TestUpdateWithSeasonOffset()
    {
        UserConfig userConfig = new UserConfig();

        userConfig.UserToken = _accessToken;

        int episodeNumber = 60;
        AnimeData? info = OnMarkedService.InternalRetrieveAnimeData("Demon Slayer", ref episodeNumber, 1, userConfig, _logger);
        Assert.NotNull(info);
        Assert.NotNull(info.ID);
        Assert.Equal(55701, info.ID);
        Assert.Equal(5, episodeNumber);
    }

    // Test for season with parts
    [Fact]
    public void TestUpdateWithSeasonParts()
    {
        UserConfig userConfig = new UserConfig();

        userConfig.UserToken = _accessToken;

        UpdateEntry entry = new UpdateEntry("Re:ZERO -Starting Life in Another World-", "Re:Zero kara Hajimeru Isekai Seikatsu", 2, 3, null);
        int episodeNumber = 2;
        AnimeData? info = OnMarkedService.InternalRetrieveAnimeData("Re:ZERO -Starting Life in Another World-", ref episodeNumber, 3, userConfig, _logger);
        Assert.NotNull(info);
        Assert.NotNull(info.ID);
        Assert.Equal(54857, info.ID);
    }

    // TODO: Add test for fallback search!
    // TODO: Add test for original title use.
}
