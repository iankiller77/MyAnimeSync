using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using HarmonyLib;
using Jellyfin.Plugin.MyAnimeSync.Api.Mal;
using Jellyfin.Plugin.MyAnimeSync.Api.TVDB;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using Jellyfin.Plugin.MyAnimeSync.HttpHelper;
using Jellyfin.Plugin.MyAnimeSync.Service;

namespace MyAnimeSync.Tests;

internal static class HarmonyMocks
{
    private const string HarmonyId = "myanimesync.tests";

    // GetAnimeID and GetAnimeInfo are counter-based because InternalUpdateAnimeListSpecial
    // calls each of them twice (once for the initial search, once for the fallback).
    internal static int AnimeIdCallCount { get; set; }
    internal static int? AnimeIdFirst { get; set; }
    internal static int? AnimeIdSecond { get; set; }

    internal static int AnimeInfoCallCount { get; set; }
    internal static AnimeData? AnimeInfoFirst { get; set; }
    internal static AnimeData? AnimeInfoSecond { get; set; }

    internal static AnimeData? AnimeSequel { get; set; }
    internal static int? TvdbId { get; set; }
    internal static EpisodeData[]? Episodes { get; set; }

    internal static string? HttpGetResponse { get; set; }
    internal static JsonNode? AuthenticatedGetResponse { get; set; }

    // --- Patch methods ---

    private static bool PatchGetUserAnimeInfoFailed(ref Task<UserAnimeInfo> __result)
    {
        __result = Task.FromResult<UserAnimeInfo>(new(null, false));
        return false;
    }

    private static bool PatchGetAnimeID(ref Task<int?> __result)
    {
        AnimeIdCallCount++;
        __result = Task.FromResult(AnimeIdCallCount == 1 ? AnimeIdFirst : AnimeIdSecond);
        return false;
    }

    private static bool PatchGetAnimeInfo(ref Task<AnimeData?> __result)
    {
        AnimeInfoCallCount++;
        __result = Task.FromResult(AnimeInfoCallCount == 1 ? AnimeInfoFirst : AnimeInfoSecond);
        return false;
    }

    private static bool PatchGetAnimeSequel(ref Task<AnimeData?> __result)
    {
        __result = Task.FromResult(AnimeSequel);
        return false;
    }

    private static bool PatchGetSerieID(ref Task<int?> __result)
    {
        __result = Task.FromResult(TvdbId);
        return false;
    }

    private static bool PatchGetEpisodesData(ref Task<EpisodeData[]?> __result)
    {
        __result = Task.FromResult(Episodes);
        return false;
    }

    private static bool PatchSendGetRequest(ref Task<string?> __result)
    {
        __result = Task.FromResult(HttpGetResponse);
        return false;
    }

    private static bool PatchSendAuthenticatedGetRequest(ref Task<JsonNode?> __result)
    {
        __result = Task.FromResult(AuthenticatedGetResponse);
        return false;
    }

    private static bool PatchHttpClientSendFail(ref Task<HttpResponseMessage> __result)
    {
        __result = Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        return false;
    }

    private static bool PatchHttpClientSendThrow(ref Task<HttpResponseMessage> __result)
    {
        __result = Task.FromException<HttpResponseMessage>(new HttpRequestException("Simulated failure"));
        return false;
    }

    private static bool PatchStreamReaderNullData(ref Task<string> __result)
    {
        __result = Task.FromResult<string>(null!);
        return false;
    }

    private static bool PatchHttpClientSendNullBody(ref Task<HttpResponseMessage> __result)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent("null");
        __result = Task.FromResult(response);
        return false;
    }


    // --- Per-function patch registration ---

    internal static Harmony CreateHarmony() => new(HarmonyId);

    internal static void AddUserAnimeInfoFailedPatch(Harmony harmony) =>
        harmony.Patch(AccessTools.Method(typeof(MalApiHandler), nameof(MalApiHandler.GetUserAnimeInfo)), prefix: new HarmonyMethod(typeof(HarmonyMocks), nameof(PatchGetUserAnimeInfoFailed)));

    internal static void AddAnimeIDPatch(Harmony harmony) =>
        harmony.Patch(AccessTools.Method(typeof(MalApiHandler), nameof(MalApiHandler.GetAnimeID)), prefix: new HarmonyMethod(typeof(HarmonyMocks), nameof(PatchGetAnimeID)));

    internal static void AddAnimeInfoPatch(Harmony harmony) =>
        harmony.Patch(AccessTools.Method(typeof(MalApiHandler), nameof(MalApiHandler.GetAnimeInfo)), prefix: new HarmonyMethod(typeof(HarmonyMocks), nameof(PatchGetAnimeInfo)));

    internal static void AddAnimeSequelPatch(Harmony harmony) =>
        harmony.Patch(AccessTools.Method(typeof(OnMarkedService), nameof(OnMarkedService.GetAnimeSequel)), prefix: new HarmonyMethod(typeof(HarmonyMocks), nameof(PatchGetAnimeSequel)));

    internal static void AddSerieIDPatch(Harmony harmony) =>
        harmony.Patch(AccessTools.Method(typeof(TVDBApiHandler), nameof(TVDBApiHandler.GetSerieID)), prefix: new HarmonyMethod(typeof(HarmonyMocks), nameof(PatchGetSerieID)));

    internal static void AddEpisodesDataPatch(Harmony harmony) =>
        harmony.Patch(AccessTools.Method(typeof(TVDBApiHandler), nameof(TVDBApiHandler.GetEpisodesData)), prefix: new HarmonyMethod(typeof(HarmonyMocks), nameof(PatchGetEpisodesData)));

    internal static void AddHttpGetRequestPatch(Harmony harmony) =>
        harmony.Patch(AccessTools.Method(typeof(HttpRequestHelper), nameof(HttpRequestHelper.SendGetRequest)), prefix: new HarmonyMethod(typeof(HarmonyMocks), nameof(PatchSendGetRequest)));

    internal static void AddAuthenticatedGetRequestPatch(Harmony harmony) =>
        harmony.Patch(
            AccessTools.Method(typeof(HttpRequestHelper), nameof(HttpRequestHelper.SendAuthenticatedGetRequest), [typeof(string), typeof(Dictionary<string, string?>), typeof(string), typeof(bool)]),
            prefix: new HarmonyMethod(typeof(HarmonyMocks), nameof(PatchSendAuthenticatedGetRequest)));

    internal static void AddHttpClientFailPatch(Harmony harmony) =>
        harmony.Patch(
            AccessTools.Method(typeof(HttpClient), "SendAsync", [typeof(HttpRequestMessage), typeof(HttpCompletionOption), typeof(CancellationToken)]),
            prefix: new HarmonyMethod(typeof(HarmonyMocks), nameof(PatchHttpClientSendFail)));

    internal static void AddHttpClientThrowPatch(Harmony harmony) =>
        harmony.Patch(
            AccessTools.Method(typeof(HttpClient), "SendAsync", [typeof(HttpRequestMessage), typeof(HttpCompletionOption), typeof(CancellationToken)]),
            prefix: new HarmonyMethod(typeof(HarmonyMocks), nameof(PatchHttpClientSendThrow)));

    internal static void AddStreamReaderNullDataPatch(Harmony harmony) =>
        harmony.Patch(
            AccessTools.Method(typeof(StreamReader), nameof(StreamReader.ReadToEndAsync), Type.EmptyTypes),
            prefix: new HarmonyMethod(typeof(HarmonyMocks), nameof(PatchStreamReaderNullData)));

    internal static void AddHttpClientNullBodyPatch(Harmony harmony) =>
        harmony.Patch(
            AccessTools.Method(typeof(HttpClient), "SendAsync", [typeof(HttpRequestMessage), typeof(HttpCompletionOption), typeof(CancellationToken)]),
            prefix: new HarmonyMethod(typeof(HarmonyMocks), nameof(PatchHttpClientSendNullBody)));


    // --- Cleanup ---

    internal static void Cleanup(Harmony harmony)
    {
        harmony.UnpatchAll(HarmonyId);
        AnimeIdCallCount = 0;
        AnimeIdFirst = null;
        AnimeIdSecond = null;
        AnimeInfoCallCount = 0;
        AnimeInfoFirst = null;
        AnimeInfoSecond = null;
        AnimeSequel = null;
        TvdbId = null;
        Episodes = null;
        HttpGetResponse = null;
        AuthenticatedGetResponse = null;
    }

    // --- Factory helpers ---
    internal static AnimeData ValidAnimeInfo(int episodeCount = 12) => new AnimeData { ID = 1, EpisodeCount = episodeCount };
    internal static UserConfig SpecialUserConfig() => new UserConfig { AllowSpecials = true, TokenAcquireDateTime = DateTime.Now };
    internal static EpisodeData MatchingEpisode() => new EpisodeData { EpisodeNumber = 1, Name = "Special" };
    internal static AnimeData SeasonalAnimeInfo() => new AnimeData { ID = 1, EpisodeCount = 12, MediaType = MediaType.SeasonalAnime };
}
