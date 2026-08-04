using Jellyfin.Plugin.MyAnimeSync.Api.Mal;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using Jellyfin.Plugin.MyAnimeSync.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyAnimeSync.Tests.UserListUpdate;

public abstract class Base
{
    protected readonly string _accessToken;
    protected readonly ILogger _logger = NullLogger.Instance;

    protected Base()
    {
        _accessToken = Environment.GetEnvironmentVariable("MAL_ACCESS_TOKEN")
            ?? throw new InvalidOperationException("MAL_ACCESS_TOKEN is not set.");
    }
}

// Most basic test, retrieve the anime without any season logic.
public class BasicUpdate : Base
{
    [Fact]
    public async Task TestBasicUpdate()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;

        // Delete Anime for user List to force an addition to the user list.
        await MalApiHandler.DeleteAnimeFromUserList(38000, userConfig);

        UpdateEntry entry = new UpdateEntry("Demon Slayer", "Kimetsu no Yaiba", 26, 1, null, null);
        bool result = await OnMarkedService.UpdateAnimeList(entry, userConfig, _logger);
        Assert.True(result);
    }
}

// Test for original title
public class TestOriginalTitle : Base
{
    [Fact]
    public async Task OriginalTitle()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;
        userConfig.OriginalTitleSearch = true;

        UpdateEntry entry = new UpdateEntry("Is It Wrong to Try to Pick Up Girls in a Dungeon?", "ダンジョンに出会いを求めるのは間違っているだろうか", 1, 5, 2015, null);
        bool result = await OnMarkedService.UpdateAnimeList(entry, userConfig, _logger);
        Assert.True(result);
    }
}

// Test for title fallback
public class TestFallbackSearch : Base
{
    [Fact]
    public async Task FallbackSearch()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;
        userConfig.OriginalTitleSearch = true;
        userConfig.OriginalTitleSearchFallback = true;

        UpdateEntry entry = new UpdateEntry("Is It Wrong to Try to Pick Up Girls in a Dungeon?", "", 1, 5, 2015, null);
        bool result = await OnMarkedService.UpdateAnimeList(entry, userConfig, _logger);
        Assert.True(result);
    }
}

// Test for generic anime tagged with the proper season.
public class TestUpdateWithSeason : Base
{
    [Fact]
    public void UpdateWithSeason()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;

        int episodeNumber = 8;
        AnimeData? info = OnMarkedService.InternalRetrieveAnimeData("Demon Slayer", ref episodeNumber, 2, userConfig, _logger);
        Assert.NotNull(info);
        Assert.NotNull(info.ID);
        Assert.Equal(47778, info.ID);
    }
}

// Test season search using absolute episode.
// This test also check for anime with multiple sequel type. (movie vs tv serie)
public class TestUpdateWithSeasonOffset : Base
{
    [Fact]
    public void UpdateWithSeasonOffset()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;

        int episodeNumber = 60;
        AnimeData? info = OnMarkedService.InternalRetrieveAnimeData("Demon Slayer", ref episodeNumber, null, userConfig, _logger);
        Assert.NotNull(info);
        Assert.NotNull(info.ID);
        Assert.Equal(55701, info.ID);
        Assert.Equal(5, episodeNumber);
    }
}

// Test for anime with movie as exclusive sequel inbetween tv seasons.
public class TestUpdateWithMovieSequel : Base
{
    [Fact]
    public void UpdateWithMovieSequel()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;
        userConfig.AllowNSFW = true;

        int episodeNumber = 8;
        AnimeData? info = OnMarkedService.InternalRetrieveAnimeData("Goblin Slayer", ref episodeNumber, 2, userConfig, _logger);
        Assert.NotNull(info);
        Assert.NotNull(info.ID);
        Assert.Equal(47160, info.ID);
    }
}


public class TestUpdateWithTvSpecial() : Base
{
    [Fact]
    public void UpdateWithTvSpecial()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;
        userConfig.AllowNSFW = true;

        int episodeNumber = 2;
        AnimeData? info = OnMarkedService.InternalRetrieveAnimeData("Mushoku Tensei: Jobless Reincarnation", ref episodeNumber, 2, userConfig, _logger);
        Assert.NotNull(info);
        Assert.NotNull(info.ID);
        Assert.Equal(51179, info.ID);
    }
}

// Test for season with parts
public class TestUpdateWithSeasonParts : Base
{
    [Fact]
    public void UpdateWithSeasonParts()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;

        int episodeNumber = 2;
        AnimeData? info = OnMarkedService.InternalRetrieveAnimeData("Re:ZERO -Starting Life in Another World-", ref episodeNumber, 3, userConfig, _logger);
        Assert.NotNull(info);
        Assert.NotNull(info.ID);
        Assert.Equal(54857, info.ID);
    }
}

// Test for update to the user list with bad data (episode > max episode).
public class TestUserListUpdateFail : Base
{
    [Fact]
    public void UserListUpdateFail()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;

        int episodeNumber = 6;
        AnimeData? info = OnMarkedService.InternalRetrieveAnimeData("デスノート", ref episodeNumber, 1, userConfig, _logger);
        Assert.NotNull(info);

        bool result = OnMarkedService.UpdateUserList("Death Note", 100, 1, info, userConfig, _logger);
        Assert.False(result);
    }
}

// Test for special anime
// TODO: For now, I only check for the japanese version of the anime, more test are required to know how well it work with english titles
//       But it seems pretty bad right now. Maybe change this eventually, should we force using the japanese title?
//       Might need to properly implement the search behaviour for tvdb even if japanese title seems to be extremely accurate so far. (In cases where it is not available)
public class TestBasicUpdateForSpecial : Base
{
    [Fact]
    public async Task BasicUpdateForSpecial()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;

        UpdateEntry entry = new UpdateEntry("Overlord", "オーバーロード", 1, 0, 2015, null);
        bool result = await OnMarkedService.UpdateAnimeList(entry, userConfig, _logger);
        Assert.True(result);

        userConfig.AllowSpecials = true;
        userConfig.OriginalTitleSearch = true;
        result = await OnMarkedService.UpdateAnimeList(entry, userConfig, _logger);
        Assert.True(result);
    }
}

// Test for special anime with fallback
public class TestFallbackSearchForSpecial : Base
{
    [Fact]
    public async Task FallbackSearchForSpecial()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;
        userConfig.OriginalTitleSearch = true;
        userConfig.OriginalTitleSearchFallback = true;

        UpdateEntry entry = new UpdateEntry("Overlord", "オーバーロード", 1, 0, 2015, null);
        bool result = await OnMarkedService.UpdateAnimeList(entry, userConfig, _logger);
        Assert.True(result);

        userConfig.AllowSpecials = true;
        result = await OnMarkedService.UpdateAnimeList(entry, userConfig, _logger);
        Assert.True(result);
    }
}

// Test for multiple animes with different production year. (Ex: JoJo's Bizarre Adventure vs JoJo's Bizarre Adventure (2012))
public class TestYearSpecificSearch : Base
{
    [Fact]
    public void YearSpecificSearch()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;
        userConfig.UseAbsoluteEpisode = true;
        int episodeNumber = 2;

        AnimeData? info = OnMarkedService.InternalRetrieveAnimeData("JoJo's Bizarre Adventure", ref episodeNumber, 1, userConfig, _logger, 2012);
        Assert.NotNull(info);
        Assert.Equal(14719, info.ID);
    }
}

// Test for Absolute Episode Search
public class TestAbsoluteEpisodeSearch : Base
{
    [Fact]
    public void AbsoluteEpisodeSearch()
    {
        UserConfig userConfig = new UserConfig();
        userConfig.UserToken = _accessToken;
        userConfig.UseAbsoluteEpisode = true;
        int episodeNumber = 2;

        // Test failing scenarios

        // ID Null
        AnimeData? info = OnMarkedService.InternalRetrieveAnimeData("One Piece", ref episodeNumber, 22, userConfig, _logger);
        Assert.Null(info);

        // Invalid ID
        info = OnMarkedService.InternalRetrieveAnimeData("One Piece", ref episodeNumber, 22, userConfig, _logger, tvdbID: "-1");
        Assert.Null(info);

        // Test working scenario

        info = OnMarkedService.InternalRetrieveAnimeData("One Piece", ref episodeNumber, 22, userConfig, _logger, tvdbID: "10173817");
        Assert.NotNull(info);
        Assert.NotNull(info.ID);
        Assert.Equal(21, info.ID);
        Assert.Equal(1087, episodeNumber);

        episodeNumber = 2;
        info = OnMarkedService.InternalRetrieveAnimeData("JoJo's Bizarre Adventure", ref episodeNumber, 2, userConfig, _logger, 2012, "4809299");
        Assert.NotNull(info);
        Assert.NotNull(info.ID);
        Assert.Equal(20899, info.ID);
    }
}

// TODO: Unit test for special that validate that we retrieve the proper anime ID!
// TODO: Integration test with Selenium for EndPoints, the config page and overall plugin. (should technically be possible to reach 100% code cover)