using System.Collections.Generic;
using Jellyfin.Plugin.MyAnimeSync.HttpHelper;

namespace MyAnimeSync.Tests.Mocked;

// These tests verify that every HttpRequestHelper method returns null on a failed
// HTTP response and on a thrown exception. HttpClient.SendAsync is patched via
// Harmony so no real network calls are made.
[Collection("SerialTests")]
public class TestNullAndErrorCasesHttpRequestHelper
{
    // --- SendGetRequest ---

    [Fact]
    public async Task HttpRequestHelper_SendGetRequest_FailResponse()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientFailPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendGetRequest("http://test.invalid", false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task HttpRequestHelper_SendGetRequest_ExceptionThrown()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientThrowPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendGetRequest("http://test.invalid", false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task HttpRequestHelper_SendGetRequest_NullData()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientNullBodyPatch(harmony);
        HarmonyMocks.AddStreamReaderNullDataPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendGetRequest("http://test.invalid", false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- SendUrlEncodedPostRequest ---

    [Fact]
    public async Task HttpRequestHelper_SendUrlEncodedPostRequest_FailResponse()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientFailPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendUrlEncodedPostRequest("http://test.invalid", new Dictionary<string, string>(), false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task HttpRequestHelper_SendUrlEncodedPostRequest_ExceptionThrown()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientThrowPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendUrlEncodedPostRequest("http://test.invalid", new Dictionary<string, string>(), false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task HttpRequestHelper_SendUrlEncodedPostRequest_NullBody()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientNullBodyPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendUrlEncodedPostRequest("http://test.invalid", new Dictionary<string, string>(), false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- SendAuthenticatedGetRequest (no query params) ---

    [Fact]
    public async Task HttpRequestHelper_SendAuthenticatedGetRequest_FailResponse()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientFailPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendAuthenticatedGetRequest("http://test.invalid", "token", false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task HttpRequestHelper_SendAuthenticatedGetRequest_ExceptionThrown()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientThrowPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendAuthenticatedGetRequest("http://test.invalid", "token", false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- SendAuthenticatedGetRequest (with query params) ---

    [Fact]
    public async Task HttpRequestHelper_SendAuthenticatedGetRequest_WithParams_FailResponse()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientFailPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendAuthenticatedGetRequest("http://test.invalid", new Dictionary<string, string?>(), "token", false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task HttpRequestHelper_SendAuthenticatedGetRequest_WithParams_ExceptionThrown()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientThrowPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendAuthenticatedGetRequest("http://test.invalid", new Dictionary<string, string?>(), "token", false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- SendPatchRequest ---

    [Fact]
    public async Task HttpRequestHelper_SendPatchRequest_FailResponse()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientFailPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendPatchRequest("http://test.invalid", new Dictionary<string, string?>(), "token", false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task HttpRequestHelper_SendPatchRequest_ExceptionThrown()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientThrowPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendPatchRequest("http://test.invalid", new Dictionary<string, string?>(), "token", false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    // --- SendDeleteRequest ---

    [Fact]
    public async Task HttpRequestHelper_SendDeleteRequest_FailResponse()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientFailPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendDeleteRequest("http://test.invalid", "token", false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }

    [Fact]
    public async Task HttpRequestHelper_SendDeleteRequest_ExceptionThrown()
    {
        var harmony = HarmonyMocks.CreateHarmony();
        HarmonyMocks.AddHttpClientThrowPatch(harmony);
        try { Assert.Null(await HttpRequestHelper.SendDeleteRequest("http://test.invalid", "token", false)); }
        finally { HarmonyMocks.Cleanup(harmony); }
    }
}
