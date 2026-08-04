using System.Text.Json.Nodes;
using Jellyfin.Plugin.MyAnimeSync.Api.TVDB;

namespace MyAnimeSync.Tests.Mocked;

// These tests cover the null-guard and happy-path logic inside TVDBApiHandler.
// Token retrieval (SendJsonPostRequest) and the authenticated API call
// (SendAuthenticatedGetRequest) are both patched via Harmony.
[Collection("SerialTests")]
public class TestNullAndErrorCasesTVDB
{
    // --- GetSerieID ---

    [Fact]
    public async Task TVDBApiHandler_GetSerieID_TokenNull()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.JsonPostResponse = null;
        HarmonyMocks.AddJsonPostRequestPatch(harmony);
        try
        {
            Assert.Null(await TVDBApiHandler.GetSerieID("Test"));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task TVDBApiHandler_GetSerieID_NullResponse()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.JsonPostResponse = JsonNode.Parse("""{"status":"success","data":{"token":"valid-token"}}""");
        HarmonyMocks.AuthenticatedGetResponse = null;
        HarmonyMocks.AddJsonPostRequestPatch(harmony);
        HarmonyMocks.AddAuthenticatedGetRequestPatch(harmony);
        try
        {
            Assert.Null(await TVDBApiHandler.GetSerieID("Test"));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task TVDBApiHandler_GetSerieID_NullId()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.JsonPostResponse = JsonNode.Parse("""{"status":"success","data":{"token":"valid-token"}}""");
        HarmonyMocks.AuthenticatedGetResponse = JsonNode.Parse("""{"status":"success","data":[{"id":null,"name":"Test"}]}""");
        HarmonyMocks.AddJsonPostRequestPatch(harmony);
        HarmonyMocks.AddAuthenticatedGetRequestPatch(harmony);
        try
        {
            Assert.Null(await TVDBApiHandler.GetSerieID("Test"));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- GetEpisodesData ---

    [Fact]
    public async Task TVDBApiHandler_GetEpisodesData_TokenNull()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.JsonPostResponse = null;
        HarmonyMocks.AddJsonPostRequestPatch(harmony);
        try
        {
            Assert.Null(await TVDBApiHandler.GetSeasonEpisodes(1, 1));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task TVDBApiHandler_GetEpisodesData_NullResponse()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.JsonPostResponse = JsonNode.Parse("""{"status":"success","data":{"token":"valid-token"}}""");
        HarmonyMocks.AuthenticatedGetResponse = null;
        HarmonyMocks.AddJsonPostRequestPatch(harmony);
        HarmonyMocks.AddAuthenticatedGetRequestPatch(harmony);
        try
        {
            Assert.Null(await TVDBApiHandler.GetSeasonEpisodes(1, 1));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

}
