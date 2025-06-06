using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        private static async Task<TokenResponseStruct> SendUrlEncodedPostRequest(string url, Dictionary<string, string> values)
        {
            HttpClient httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(values);
            var response = await httpClient.PostAsync(url, content).ConfigureAwait(true);
            if (!response.IsSuccessStatusCode)
            {
                throw new AuthenticationException("Could not retrieve provider token");
            }

            StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(true));
            TokenResponseStruct? jsonData = JsonSerializer.Deserialize<TokenResponseStruct>(await reader.ReadToEndAsync().ConfigureAwait(true));
            if (jsonData == null)
            {
                throw new AuthenticationException("Could not retrieve token from request.");
            }

            return jsonData;
        }

        private static async Task<JsonNode?> SendAuthenticatedGetRequest(string url, string token)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.GetAsync(url).ConfigureAwait(true);
            if (!response.IsSuccessStatusCode)
            {
                throw new AuthenticationException("Authenticated get request returned an error response.");
            }

            StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(true));
            JsonNode? jsonData = JsonObject.Parse(await reader.ReadToEndAsync().ConfigureAwait(true));

            return jsonData;
        }

        private static async Task<JsonNode?> SendAuthenticatedGetRequest(string url, Dictionary<string, string?> values, string token)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var uri = new Uri(QueryHelpers.AddQueryString(url, values));

            var response = await httpClient.GetAsync(uri).ConfigureAwait(true);
            if (!response.IsSuccessStatusCode)
            {
                throw new AuthenticationException("Authenticated get request returned an error response.");
            }

            StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(true));
            JsonNode? jsonData = JsonObject.Parse(await reader.ReadToEndAsync().ConfigureAwait(true));

            return jsonData;
        }

        private static async Task<JsonNode?> SendPatchRequest(string url, Dictionary<string, string?> values, string token)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var content = new FormUrlEncodedContent(values);

            var response = await httpClient.PatchAsync(url, content).ConfigureAwait(true);
            if (!response.IsSuccessStatusCode)
            {
                throw new AuthenticationException("Authenticated patch request returned an error response.");
            }

            StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(true));
            JsonNode? jsonData = JsonObject.Parse(await reader.ReadToEndAsync().ConfigureAwait(true));

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
            string codeChallenge;
            if (string.IsNullOrEmpty(uConfig.CodeChallenge))
            {
                codeChallenge = GenerateCodeChallenge();
                uConfig.CodeChallenge = codeChallenge;
                Plugin.Instance?.SaveConfiguration();
            }
            else
            {
                codeChallenge = uConfig.CodeChallenge;
            }

            if (string.IsNullOrEmpty(uConfig.ClientID))
            {
                return null;
            }

            return $"{AuthorisationUrl}?response_type=code&client_id={uConfig.ClientID}&code_challenge={codeChallenge}";
        }

        private static async Task UpdateTokensInConfig(TokenResponseStruct response, UserConfig uConfig)
        {
            await Task.Run(() =>
            {
                if (response.AccessToken == null || response.RefreshToken == null || response.ExpiresIn == null)
                {
                    throw new AuthenticationException("Could not retrieve data from token response!");
                }

                uConfig.TokenExpirationDateTime = DateTime.Now.AddSeconds(response.ExpiresIn.Value);
                uConfig.TokenAcquireDateTime = DateTime.Now;
                uConfig.UserToken = response.AccessToken;
                uConfig.RefreshToken = response.RefreshToken;
                Plugin.Instance?.SaveConfiguration();
            }).ConfigureAwait(true);
        }

        /// <summary>
        /// Generate api token for futur api calls.
        /// </summary>
        /// <param name="apiCode">The user code received from authentication url. <see cref="string"/>.</param>
        /// <param name="uConfig">The user config. <see cref="UserConfig"/>.</param>
        /// <returns>The task.</returns>
        public static async Task GetNewTokens(string apiCode, UserConfig uConfig)
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
            TokenResponseStruct jsonData = await SendUrlEncodedPostRequest(TokenUrl, values).ConfigureAwait(true);
            await UpdateTokensInConfig(jsonData, uConfig).ConfigureAwait(true);
        }

        /// <summary>
        /// Refresh api token for futur api calls.
        /// </summary>
        /// <param name="uConfig">The user config. <see cref="UserConfig"/>.</param>
        /// <returns>The task.</returns>
        public static async Task RefreshTokens(UserConfig uConfig)
        {
            DateTime expirationDateTime = uConfig.TokenExpirationDateTime;
            DateTime acquiredTokenDate = uConfig.TokenAcquireDateTime;

            // Only refresh if tokens are invalid or if the token is at least 7 days old.
            if (DateTime.Now.AddMinutes(5) < expirationDateTime || (DateTime.Now - acquiredTokenDate).TotalDays < 7) { return; }

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

            TokenResponseStruct jsonData = await SendUrlEncodedPostRequest(TokenUrl, values).ConfigureAwait(true);
            await UpdateTokensInConfig(jsonData, uConfig).ConfigureAwait(true);
        }

        /// <summary>
        /// Remove special characters from a string.
        /// </summary>
        /// <param name="str">The string to edit.</param>
        /// <returns>The string with no special characters.</returns>
        public static string RemoveSpecialCharacters(string str)
        {
            return Regex.Replace(str, "[^a-zA-Z0-9\\ _.]+", string.Empty, RegexOptions.Compiled);
        }

        /// <summary>
        /// Retrieve the anime id associated with a tittle.
        /// </summary>
        /// <param name="animeName">The user config. <see cref="string"/>.</param>
        /// <param name="uConfig">The user config. <see cref="UserConfig"/>.</param>
        /// <returns> The generated tokens. </returns>
        public static async Task<int?> GetAnimeID(string animeName, UserConfig uConfig)
        {
            string token = uConfig.UserToken;
            bool nsfwCheck = uConfig.AllowNSFW;

            string searchString = animeName;
            if (searchString.Length > 64)
            {
                searchString = searchString.Remove(64);
            }

            var values = new Dictionary<string, string?>()
            {
                { "q", searchString },
                { "limit", "25" },
                { "fields", "alternative_titles" },
                { "nsfw", nsfwCheck.ToString() }
            };

            JsonNode? jsonData = await SendAuthenticatedGetRequest(AnimeUrl, values, token).ConfigureAwait(true);
            if (jsonData == null) { return null; }

            Node[]? entry = jsonData["data"]?.Deserialize<Node[]>();
            if (entry == null) { return null; }

            List<Node> matchingEntries = Array.FindAll(entry, element =>
                                                            (element.SearchEntry?.Title?.Contains(animeName, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                                                            (element.SearchEntry?.AlternativeTitles?.EnglishTitle?.Contains(animeName, StringComparison.CurrentCultureIgnoreCase) ?? false)).ToList();

            // If we could not retrieve with complete string, try matching individual words instead.
            if (matchingEntries.Count < 1)
            {
                // Remove special characters
                string formatedName = RemoveSpecialCharacters(animeName);
                string[] words = formatedName.Split(" ");
                int mostWordMatched = 0;
                foreach (Node node in entry)
                {
                    // Remove special character if search entry is defined.
                    if (node.SearchEntry != null)
                    {
                        if (node.SearchEntry.Title != null)
                        {
                            node.SearchEntry.Title = RemoveSpecialCharacters(node.SearchEntry.Title);
                        }

                        if (node.SearchEntry.AlternativeTitles != null && node.SearchEntry.AlternativeTitles.EnglishTitle != null)
                        {
                            node.SearchEntry.AlternativeTitles.EnglishTitle = RemoveSpecialCharacters(node.SearchEntry.AlternativeTitles.EnglishTitle);
                        }
                    }

                    int matchedWordCount = words.Count(element =>
                                                    (node.SearchEntry?.Title?.Contains(element, StringComparison.CurrentCultureIgnoreCase) ?? false) ||
                                                    (node.SearchEntry?.AlternativeTitles?.EnglishTitle?.Contains(element, StringComparison.CurrentCultureIgnoreCase) ?? false));
                    if (matchedWordCount == mostWordMatched)
                    {
                        matchingEntries.Add(node);
                    }
                    else if (matchedWordCount > mostWordMatched)
                    {
                        matchingEntries.Clear();
                        matchingEntries.Add(node);
                        mostWordMatched = matchedWordCount;
                    }
                }

                // If we did not match all the words, return null.
                if (mostWordMatched < words.Length) { return null; }
            }

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
        public static async Task<AnimeData?> GetAnimeInfo(int animeID, UserConfig uConfig)
        {
            string token = uConfig.UserToken;

            var values = new Dictionary<string, string?>()
            {
                { "fields", "id,title,media_type,num_episodes,related_anime,alternative_titles" }
            };

            string url = AnimeUrl + "/" + animeID;
            JsonNode? jsonData = await SendAuthenticatedGetRequest(url, values, token).ConfigureAwait(true);
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
        public static async Task<UserAnimeInfo> GetUserAnimeInfo(int animeID, UserConfig uConfig)
        {
            string token = uConfig.UserToken;

            var values = new Dictionary<string, string?>()
            {
                { "fields", "list_status" },
                { "limit", "500" }
            };

            JsonNode? jsonData = await SendAuthenticatedGetRequest(UserAnimeListUrl, values, token).ConfigureAwait(true);
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

                jsonData = await SendAuthenticatedGetRequest(nextUrl, token).ConfigureAwait(true);
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
        /// <param name="status">Watch status of the serie.<see cref="bool"/>.</param>
        /// <param name="uConfig">The user config. <see cref="UserConfig"/>.</param>
        /// <returns>A task.</returns>
        public static async Task UpdateUserInfo(int animeID, int episodeNumber, string status, UserConfig uConfig)
        {
            string token = uConfig.UserToken;

            var values = new Dictionary<string, string?>()
            {
                { "num_watched_episodes", string.Empty + episodeNumber },
                { "status", status }
            };

            string url = AnimeUrl + "/" + animeID + "/my_list_status";
            await SendPatchRequest(url, values, token).ConfigureAwait(true);
        }

        internal sealed class TokenResponseStruct
        {
            public TokenResponseStruct()
            {
                AccessToken = string.Empty;
                RefreshToken = string.Empty;
            }

            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [JsonPropertyName("refresh_token")]
            public string RefreshToken { get; set; }

            /// <summary>
            /// Gets or sets time before the token expire in seconds.
            /// </summary>
            [JsonPropertyName("expires_in")]
            public int? ExpiresIn { get; set; }
        }
    }
}