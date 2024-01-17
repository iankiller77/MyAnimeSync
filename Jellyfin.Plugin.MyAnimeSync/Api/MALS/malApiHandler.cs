using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
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
            uConfig.RefreshToken = jsonData.Access_token;
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
            uConfig.RefreshToken = jsonData.Access_token;
            Plugin.Instance?.SaveConfiguration();
        }

        /// <summary>
        /// Retrieve the anime info associated with a tittle.
        /// </summary>
        /// <param name="animeName">The user config. <see cref="string"/>.</param>
        /// <param name="uConfig">The user config. <see cref="UserConfig"/>.</param>
        /// <returns> The generated tokens. </returns>
        public static JsonNode? GetAnimeInfo(string animeName, UserConfig uConfig)
        {
            string token = uConfig.UserToken;

            var values = new Dictionary<string, string?>()
            {
                { "q", animeName },
                { "limit", "1" }
            };

            JsonNode? jsonData = SendAuthenticatedGetRequest(AnimeUrl, values, token);

            return jsonData;
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
