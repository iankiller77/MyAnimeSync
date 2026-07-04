using System.Text.Json.Nodes;
using Jellyfin.Plugin.MyAnimeSync.Api.TVDB;

namespace MyAnimeSync.Tests.Mocked;

// These tests cover the null-guard and happy-path logic inside TVDBApiHandler.
// Token retrieval (SendGetRequest) and the authenticated API call
// (SendAuthenticatedGetRequest) are both patched via Harmony.
[Collection("SerialTests")]
public class TestNullAndErrorCasesTVDB
{
    // --- GetSerieID ---

    [Fact]
    public async Task TVDBApiHandler_GetSerieID_TokenNull()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.HttpGetResponse = null;
        HarmonyMocks.AddHttpGetRequestPatch(harmony);
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
        HarmonyMocks.HttpGetResponse = "valid-token";
        HarmonyMocks.AuthenticatedGetResponse = null;
        HarmonyMocks.AddHttpGetRequestPatch(harmony);
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
        HarmonyMocks.HttpGetResponse = "valid-token";
        HarmonyMocks.AuthenticatedGetResponse = JsonNode.Parse("""{"status":"success","data":[{"id":null,"name":"Test"}]}""");
        HarmonyMocks.AddHttpGetRequestPatch(harmony);
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
        HarmonyMocks.HttpGetResponse = null;
        HarmonyMocks.AddHttpGetRequestPatch(harmony);
        try
        {
            Assert.Null(await TVDBApiHandler.GetEpisodesData(1, 1));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task TVDBApiHandler_GetEpisodesData_NullResponse()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.HttpGetResponse = "valid-token";
        HarmonyMocks.AuthenticatedGetResponse = null;
        HarmonyMocks.AddHttpGetRequestPatch(harmony);
        HarmonyMocks.AddAuthenticatedGetRequestPatch(harmony);
        try
        {
            Assert.Null(await TVDBApiHandler.GetEpisodesData(1, 1));
        }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

}
