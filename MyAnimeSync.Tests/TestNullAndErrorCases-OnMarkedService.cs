using Jellyfin.Plugin.MyAnimeSync.Api.Mal;
using Jellyfin.Plugin.MyAnimeSync.Api.TVDB;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using Jellyfin.Plugin.MyAnimeSync.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MyAnimeSync.Tests.Mocked;

// These tests cover defensive null checks that guard against corrupted API responses.
// They do not require a real MAL token.
[Collection("SerialTests")]
public class TestNullAndErrorCasesOnMarkedService
{
    private readonly ILogger _logger = NullLogger.Instance;

    // --- GetAnimeSequel: null / missing data in RelatedNodes ---

    [Fact]
    public async Task OnMarkedService_GetAnimeSequel_NullRelatedNodes()
    {
        AnimeData info = new AnimeData { Title = "Test", RelatedNodes = null };
        AnimeData? result = await OnMarkedService.GetAnimeSequel(info, new UserConfig(), _logger);
        Assert.Null(result);
    }

    [Fact]
    public async Task OnMarkedService_GetAnimeSequel_SequelWithNullSearchEntry()
    {
        AnimeData info = new AnimeData
        {
            Title = "Test",
            RelatedNodes = [new RelatedAnime { RelationType = "sequel", SearchEntry = null }]
        };
        AnimeData? result = await OnMarkedService.GetAnimeSequel(info, new UserConfig(), _logger);
        Assert.Null(result);
    }

    [Fact]
    public async Task OnMarkedService_GetAnimeSequel_MultipleSequelsFirstWithNullSearchEntry()
    {
        AnimeData info = new AnimeData
        {
            Title = "Test",
            RelatedNodes =
            [
                new RelatedAnime { RelationType = "sequel", SearchEntry = null },
                new RelatedAnime { RelationType = "sequel", SearchEntry = new AnimeSearchEntry { ID = 2 } }
            ]
        };
        AnimeData? result = await OnMarkedService.GetAnimeSequel(info, new UserConfig(), _logger);
        Assert.Null(result);
    }

    // --- Mock using harmony!
    // --- UpdateUserList: GetUserAnimeInfo returns SuccessStatus = false ---

    [Fact]
    public void OnMarkedService_UpdateUserList_GetUserAnimeInfoFailed()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddUserAnimeInfoFailedPatch(harmony);
        try
        {
            AnimeData info = new AnimeData { ID = 1, EpisodeCount = 12 };
            bool result = OnMarkedService.UpdateUserList("Test", 1, 1, info, new UserConfig(), _logger);
            Assert.False(result);
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- InternalRetrieveAnimeData: initial API call failures ---

    [Fact]
    public void OnMarkedService_InternalRetrieveAnimeData_GetAnimeIdNull()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AnimeIdFirst = null;
        HarmonyMocks.AddAnimeIDPatch(harmony);
        try
        {
            int ep = 1;
            Assert.Null(OnMarkedService.InternalRetrieveAnimeData("Test", ref ep, 1, new UserConfig(), _logger));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public void OnMarkedService_InternalRetrieveAnimeData_GetAnimeInfoNull()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AnimeIdFirst = 1;
        HarmonyMocks.AnimeInfoFirst = null;
        HarmonyMocks.AddAnimeIDPatch(harmony);
        HarmonyMocks.AddAnimeInfoPatch(harmony);
        try
        {
            int ep = 1;
            Assert.Null(OnMarkedService.InternalRetrieveAnimeData("Test", ref ep, 1, new UserConfig(), _logger));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- InternalRetrieveAnimeData: season-offset sequel failures (seasonNumber = 2) ---

    [Fact]
    public void OnMarkedService_InternalRetrieveAnimeData_SequelNullForSeasonOffset()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AnimeIdFirst = 1;
        HarmonyMocks.AnimeInfoFirst = HarmonyMocks.ValidAnimeInfo();
        HarmonyMocks.AnimeSequel = null;
        HarmonyMocks.AddAnimeIDPatch(harmony);
        HarmonyMocks.AddAnimeInfoPatch(harmony);
        HarmonyMocks.AddAnimeSequelPatch(harmony);
        try
        {
            int ep = 1;
            Assert.Null(OnMarkedService.InternalRetrieveAnimeData("Test", ref ep, 2, new UserConfig(), _logger));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- InternalRetrieveAnimeData: episode-offset sequel failures (episodeNumber = 10, EpisodeCount = 5) ---

    [Fact]
    public void OnMarkedService_InternalRetrieveAnimeData_SequelNullForEpisodeOffset()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AnimeIdFirst = 1;
        HarmonyMocks.AnimeInfoFirst = HarmonyMocks.ValidAnimeInfo(episodeCount: 5);
        HarmonyMocks.AnimeSequel = null;
        HarmonyMocks.AddAnimeIDPatch(harmony);
        HarmonyMocks.AddAnimeInfoPatch(harmony);
        HarmonyMocks.AddAnimeSequelPatch(harmony);
        try
        {
            int ep = 10;
            Assert.Null(OnMarkedService.InternalRetrieveAnimeData("Test", ref ep, 1, new UserConfig(), _logger));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- InternalUpdateAnimeListSpecial: TVDB failures ---

    [Fact]
    public async Task OnMarkedService_InternalUpdateAnimeListSpecial_GetSerieIdNull()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.TvdbId = null;
        HarmonyMocks.AddSerieIDPatch(harmony);
        try
        {
            Assert.False(await OnMarkedService.InternalUpdateAnimeListSpecial("Test", 1, HarmonyMocks.SpecialUserConfig(), _logger));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task OnMarkedService_InternalUpdateAnimeListSpecial_GetEpisodesDataNull()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.TvdbId = 1;
        HarmonyMocks.Episodes = null;
        HarmonyMocks.AddSerieIDPatch(harmony);
        HarmonyMocks.AddEpisodesDataPatch(harmony);
        try
        {
            Assert.False(await OnMarkedService.InternalUpdateAnimeListSpecial("Test", 1, HarmonyMocks.SpecialUserConfig(), _logger));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- InternalUpdateAnimeListSpecial: EpisodeData failures ---

    [Fact]
    public async Task OnMarkedService_InternalUpdateAnimeListSpecial_EpisodeDataNull()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.TvdbId = 1;
        HarmonyMocks.Episodes = [new EpisodeData { EpisodeNumber = 99, Name = "Other" }];
        HarmonyMocks.AddSerieIDPatch(harmony);
        HarmonyMocks.AddEpisodesDataPatch(harmony);
        try
        {
            // episode 1 not in the list → Array.Find returns null
            Assert.False(await OnMarkedService.InternalUpdateAnimeListSpecial("Test", 1, HarmonyMocks.SpecialUserConfig(), _logger));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- InternalUpdateAnimeListSpecial: first info block failures (each triggering the fallback, then newID=null) ---

    [Fact]
    public async Task OnMarkedService_InternalUpdateAnimeListSpecial_GetAnimeIdNull()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.TvdbId = 1;
        HarmonyMocks.Episodes = [HarmonyMocks.MatchingEpisode()];
        HarmonyMocks.AnimeIdFirst = null;
        HarmonyMocks.AnimeIdSecond = null;
        HarmonyMocks.AddSerieIDPatch(harmony);
        HarmonyMocks.AddEpisodesDataPatch(harmony);
        HarmonyMocks.AddAnimeIDPatch(harmony);
        try
        {
            // id=null → if(id!=null) skipped → info=null → fallback → newID=null → false
            Assert.False(await OnMarkedService.InternalUpdateAnimeListSpecial("Test", 1, HarmonyMocks.SpecialUserConfig(), _logger));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task OnMarkedService_InternalUpdateAnimeListSpecial_FirstInfoNull()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.TvdbId = 1;
        HarmonyMocks.Episodes = [HarmonyMocks.MatchingEpisode()];
        HarmonyMocks.AnimeIdFirst = 1;
        HarmonyMocks.AnimeIdSecond = null;
        HarmonyMocks.AnimeInfoFirst = null;
        HarmonyMocks.AddSerieIDPatch(harmony);
        HarmonyMocks.AddEpisodesDataPatch(harmony);
        HarmonyMocks.AddAnimeIDPatch(harmony);
        HarmonyMocks.AddAnimeInfoPatch(harmony);
        try
        {
            Assert.False(await OnMarkedService.InternalUpdateAnimeListSpecial("Test", 1, HarmonyMocks.SpecialUserConfig(), _logger));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- InternalUpdateAnimeListSpecial: fallback second info block failures (first info triggers fallback via SeasonalAnime) ---

    [Fact]
    public async Task OnMarkedService_InternalUpdateAnimeListSpecial_FallbackInfoNull()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.TvdbId = 1;
        HarmonyMocks.Episodes = [HarmonyMocks.MatchingEpisode()];
        HarmonyMocks.AnimeIdFirst = 1;
        HarmonyMocks.AnimeIdSecond = 1;
        HarmonyMocks.AnimeInfoFirst = HarmonyMocks.SeasonalAnimeInfo();
        HarmonyMocks.AnimeInfoSecond = null;
        HarmonyMocks.AddSerieIDPatch(harmony);
        HarmonyMocks.AddEpisodesDataPatch(harmony);
        HarmonyMocks.AddAnimeIDPatch(harmony);
        HarmonyMocks.AddAnimeInfoPatch(harmony);
        try
        {
            Assert.False(await OnMarkedService.InternalUpdateAnimeListSpecial("Test", 1, HarmonyMocks.SpecialUserConfig(), _logger));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task OnMarkedService_InternalUpdateAnimeListSpecial_FallbackInfoSeasonalAnime()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.TvdbId = 1;
        HarmonyMocks.Episodes = [HarmonyMocks.MatchingEpisode()];
        HarmonyMocks.AnimeIdFirst = 1;
        HarmonyMocks.AnimeIdSecond = 1;
        HarmonyMocks.AnimeInfoFirst = HarmonyMocks.SeasonalAnimeInfo();
        HarmonyMocks.AnimeInfoSecond = HarmonyMocks.SeasonalAnimeInfo();
        HarmonyMocks.AddSerieIDPatch(harmony);
        HarmonyMocks.AddEpisodesDataPatch(harmony);
        HarmonyMocks.AddAnimeIDPatch(harmony);
        HarmonyMocks.AddAnimeInfoPatch(harmony);
        try
        {
            Assert.False(await OnMarkedService.InternalUpdateAnimeListSpecial("Test", 1, HarmonyMocks.SpecialUserConfig(), _logger));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }
}
