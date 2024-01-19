using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Jellyfin.Data.Entities;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using Microsoft.AspNetCore.WebUtilities;

namespace Jellyfin.Plugin.MyAnimeSync.Api.Mal
{
    /// <summary>
    /// Class used to make api request to MyAnimeList.
    /// </summary>
    public static class MalApiHandler
    {
        private const string ApiBaseUrl = "https://myanimelist.net/v1/oauth2/";
        private const string AuthorisationUrl = ApiBaseUrl + "authorize";
        private const string TokenUrl = ApiBaseUrl + "token";
        private const string AnimeUrl = "https://api.myanimelist.net/v2/anime";
        private const string UserAnimeListUrl = "https://api.myanimelist.net/v2/users/@me/animelist";

        private static TokenResponseStruct SendUrlEncodedPostRequest(string url, Dictionary<string, string> values)
        {
            HttpClient httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(values);
            var response = httpClient.PostAsync(url, content).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new AuthenticationException("Could not retrieve provider token");
            }

            StreamReader reader = new StreamReader(response.Content.ReadAsStream());
            TokenResponseStruct? jsonData = JsonSerializer.Deserialize<TokenResponseStruct>(reader.ReadToEnd());
            if (jsonData == null)
            {
                throw new AuthenticationException("Could not retrieve token from request.");
            }

            return jsonData;
        }

        private static JsonNode? SendAuthenticatedGetRequest(string url, string token)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = httpClient.GetAsync(url).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new AuthenticationException("Authenticated get request returned an error response.");
            }

            StreamReader reader = new StreamReader(response.Content.ReadAsStream());
            JsonNode? jsonData = JsonObject.Parse(reader.ReadToEnd());

            return jsonData;
        }

        private static JsonNode? SendAuthenticatedGetRequest(string url, Dictionary<string, string?> values, string token)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var uri = new Uri(QueryHelpers.AddQueryString(url, values));

            var response = httpClient.GetAsync(uri).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new AuthenticationException("Authenticated get request returned an error response.");
            }

            StreamReader reader = new StreamReader(response.Content.ReadAsStream());
            JsonNode? jsonData = JsonObject.Parse(reader.ReadToEnd());

            return jsonData;
        }

        private static JsonNode? SendPatchRequest(string url, Dictionary<string, string?> values, string token)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var content = new FormUrlEncodedContent(values);

            var response = httpClient.PatchAsync(url, content).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new AuthenticationException("Authenticated patch request returned an error response.");
            }

            StreamReader reader = new StreamReader(response.Content.ReadAsStream());
            JsonNode? jsonData = JsonObject.Parse(reader.ReadToEnd());

            return jsonData;
        }

        private static string GenerateCodeChallenge()
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz123456789";
            var random = new Random();
            var nonce = new char[128];
            for (int i = 0; i < nonce.Length; i++)
            {
                nonce[i] = chars[random.Next(chars.Length)];
            }

            return new string(nonce);
        }

        /// <summary>
        /// Generated an auth url to be parsed into a web navigator for api authorisation.
        /// </summary>
        /// <param name="uConfig">The user config. <see cref="UserConfig"/>.</param>
        /// <returns> The uri that should be used for the api call. </returns>
        public static string? GenerateAuthCodeUrl(UserConfig uConfig)
        {
            string codeChallenge = GenerateCodeChallenge();
            if (string.IsNullOrEmpty(uConfig.ClientID))
            {
                return null;
            }

            uConfig.CodeChallenge = codeChallenge;
            Plugin.Instance?.SaveConfiguration();

            return $"{AuthorisationUrl}?response_type=code&client_id={uConfig.ClientID}&code_challenge={codeChallenge}";
        }

        /// <summary>
        /// Generate api token for futur api calls.
        /// </summary>
        /// <param name="apiCode">The user code received from authentication url. <see cref="string"/>.</param>
        /// <param name="uConfig">The user config. <see cref="UserConfig"/>.</param>
        public static void GetNewTokens(string apiCode, UserConfig uConfig)
        {
            string clientID = uConfig.ClientID;
            string clientSecret = uConfig.ClientSecret;
            string codeVerifier = uConfig.CodeChallenge;
            string grantType = "authorization_code";

            var values = new Dictionary<string, string>()
            {
                { "client_id", clientID },
                { "client_secret", clientSecret },
                { "code", apiCode },
                { "code_verifier", codeVerifier },
                { "grant_type", grantType }
            };
            TokenResponseStruct jsonData = SendUrlEncodedPostRequest(TokenUrl, values);

            uConfig.UserToken = jsonData.Access_token;
            uConfig.RefreshToken = jsonData.Refresh_token;
            Plugin.Instance?.SaveConfiguration();
        }

        /// <summary>
        /// Refresh api token for futur api calls.
        /// </summary>
        /// <param name="uConfig">The user config. <see cref="UserConfig"/>.</param>
        public static void RefreshTokens(UserConfig uConfig)
        {
            string clientID = uConfig.ClientID;
            string clientSecret = uConfig.ClientSecret;
            string refreshToken = uConfig.RefreshToken;

            var values = new Dictionary<string, string>()
            {
                { "client_id", clientID },
                { "client_secret", clientSecret },
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            };
            TokenResponseStruct jsonData = SendUrlEncodedPostRequest(TokenUrl, values);

            uConfig.UserToken = jsonData.Access_token;
            uConfig.RefreshToken = jsonData.Refresh_token;
            Plugin.Instance?.SaveConfiguration();
        }

        /// <summary>
        /// Retrieve the anime id associated with a tittle.
        /// </summary>
        /// <param name="animeName">The user config. <see cref="string"/>.</param>
        /// <param name="uConfig">The user config. <see cref="UserConfig"/>.</param>
        /// <returns> The generated tokens. </returns>
        public static int? GetAnimeID(string animeName, UserConfig uConfig)
        {
            string token = uConfig.UserToken;

            var values = new Dictionary<string, string?>()
            {
                { "q", animeName },
                { "limit", "25" },
                { "fields", "alternative_titles" }
            };

            JsonNode? jsonData = SendAuthenticatedGetRequest(AnimeUrl, values, token);
            if (jsonData == null) { return null; }

            Node[]? entry = jsonData["data"]?.Deserialize<Node[]>();
            if (entry == null) { return null; }

            Node[] matchingEntries = Array.FindAll(entry, element =>
                                                            (element.SearchEntry?.Title?.Contains(animeName, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                                                            (element.SearchEntry?.AlternativeTitles?.EnglishTitle?.Contains(animeName, StringComparison.CurrentCultureIgnoreCase) ?? false));
            if (matchingEntries.Length < 1) { return null; }

            AnimeSearchEntry? bestMatchingNode = matchingEntries.OrderBy(element => element.SearchEntry?.Title?.Length).FirstOrDefault()?.SearchEntry;
            if (bestMatchingNode == null || bestMatchingNode.ID == null) { return null; }

            return bestMatchingNode.ID;
        }

        /// <summary>
        /// Retrieve the information associated to an anime ID.
        /// </summary>
        /// <param name="animeID">The id of the anime. <see cref="int"/>.</param>
        /// <param name="uConfig">The user config. <see cref="UserConfig"/>.</param>
        /// <returns>Anime info associated to the anime id.</returns>
        public static AnimeData? GetAnimeInfo(int animeID, UserConfig uConfig)
        {
            string token = uConfig.UserToken;

            var values = new Dictionary<string, string?>()
            {
                { "fields", "id,title,num_episodes,related_anime" }
            };

            string url = AnimeUrl + "/" + animeID;
            JsonNode? jsonData = SendAuthenticatedGetRequest(url, values, token);
            if (jsonData == null) { return null; }

            AnimeData? animeInfo = jsonData.Deserialize<AnimeData>();
            return animeInfo;
        }

        /// <summary>
        /// Retrieve the watch status of a user's anime serie.
        /// </summary>
        /// <param name="animeID">The id of the anime. <see cref="int"/>.</param>
        /// <param name="uConfig">The user config. <see cref="UserConfig"/>.</param>
        /// <returns>The anime serie status.</returns>
        public static UserAnimeInfo GetUserAnimeInfo(int animeID, UserConfig uConfig)
        {
            string token = uConfig.UserToken;

            var values = new Dictionary<string, string?>()
            {
                { "fields", "list_status" },
                { "limit", "500" }
            };

            JsonNode? jsonData = SendAuthenticatedGetRequest(UserAnimeListUrl, values, token);
            if (jsonData == null) { return new UserAnimeInfo(null, false); }
            AnimeListEntry[]? animeList = jsonData["data"].Deserialize<AnimeListEntry[]>();
            if (animeList == null) { return new UserAnimeInfo(null, false); }

            AnimeListEntry? entry = Array.Find(animeList, element => element.AnimeInfo?.ID == animeID);
            while (entry == null || entry.StatusInfo == null)
            {
                JsonNode? pagingData = jsonData["paging"];
                if (pagingData == null) { return new UserAnimeInfo(null, false); }

                // If we have no next but paging attribute is visible, we parsed the entire anime list.
                string? nextUrl = pagingData["next"].Deserialize<string>();
                if (nextUrl == null) { return new UserAnimeInfo(null, true); }

                jsonData = SendAuthenticatedGetRequest(nextUrl, token);
                if (jsonData == null) { return new UserAnimeInfo(null, false); }

                animeList = jsonData["data"].Deserialize<AnimeListEntry[]>();
                if (animeList == null) { return new UserAnimeInfo(null, false); }

                entry = Array.Find(animeList, element => element.AnimeInfo?.ID == animeID);
            }

            return new UserAnimeInfo(entry, true);
        }

        /// <summary>
        /// Update the watch list of a user.
        /// </summary>
        /// <param name="animeID">The id of the anime. <see cref="int"/>.</param>
        /// <param name="episodeNumber">The episode number.<see cref="int"/>.</param>
        /// <param name="completed">Completion status of the serie.<see cref="bool"/>.</param>
        /// <param name="uConfig">The user config. <see cref="UserConfig"/>.</param>
        public static void UpdateUserInfo(int animeID, int episodeNumber, bool completed, UserConfig uConfig)
        {
            string token = uConfig.UserToken;

            var values = new Dictionary<string, string?>()
            {
                { "num_watched_episodes", string.Empty + episodeNumber }
            };

            if (completed)
            {
                values.Add("status", WatchStatus.Completed);
            }

            string url = AnimeUrl + "/" + animeID + "/my_list_status";
            SendPatchRequest(url, values, token);
        }

        internal sealed class TokenResponseStruct
        {
            public TokenResponseStruct()
            {
                Access_token = string.Empty;
                Refresh_token = string.Empty;
            }

            [JsonPropertyName("access_token")]
            public string Access_token { get; set; }

            [JsonPropertyName("refresh_token")]
            public string Refresh_token { get; set; }
        }
    }
}
