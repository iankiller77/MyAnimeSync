using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows.Markup;
using Jellyfin.Plugin.MyAnimeSync.HttpHelper;

namespace Jellyfin.Plugin.MyAnimeSync.Api.TVDB
{
    /// <summary>
    /// Class used to make api request to TVDB.
    /// </summary>
    public static class TVDBApiHandler
    {
        private const string ApiBaseUrl = "https://api4.thetvdb.com/v4/";
        // private const string LoginUrl = ApiBaseUrl + "login";
        private const string Token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJhZ2UiOiIiLCJhcGlrZXkiOiI0ZWZhZjk0OS1mMTgyLTQ1M2EtYWUwMi1jYmIyMjVmNjVhYjYiLCJjb21tdW5pdHlfc3VwcG9ydGVkIjpmYWxzZSwiZXhwIjoxNzU5OTIxODM0LCJnZW5kZXIiOiIiLCJoaXRzX3Blcl9kYXkiOjEwMDAwMDAwMCwiaGl0c19wZXJfbW9udGgiOjEwMDAwMDAwMCwiaWQiOiIyODE0MDc1IiwiaXNfbW9kIjpmYWxzZSwiaXNfc3lzdGVtX2tleSI6ZmFsc2UsImlzX3RydXN0ZWQiOmZhbHNlLCJwaW4iOiJJUFFaT0NNTiIsInJvbGVzIjpbXSwidGVuYW50IjoidHZkYiIsInV1aWQiOiIifQ.Lb_cPyFNsHnq91q12X3-ZHDaV9WNoEyR_YyTV65v3pmWmyeinS5fHsWlnAwKjX0kAB85pbVDu5dQwFuWRACGrkXUXA4hD52fh_azRluI0IxoL_7mv1z-8HoJWgV8DgJnoX6I1AvMDH40L8Ck_DBD7nar0gJNCUYLh4qRoknmNFLOgUCzsL9KNmj4a2LFkLed5fKMCD4IWVNEFvTGFYbaSOqUwBRTP_nBWbr-lPdqw5QsC7Kqq9hlDS2uWNsrptHIxta8jviY6nu77g_uQHAY5X8U_MUx0BloV_bxY37JocP7asSGGkcLdYjcP3IcGYfYBX_KmnwQ-97Z12j2yBmVlhdsDJtfSQe-UywFSlwSMXE7GcqM0qZ7lRtglWCCEVBAzsvYR34d9lQ0aE0IbTVljFRT5P0znIrmT3l8RPK26H2PR-7P028K-6RsPKeWZNUuykUsIkoIT3q7lYORz0EtCj5kaeJJzdqfRY9mn2Dca-0dJN1_qin_mSgdCUjPjUIwbbvpylVu0xZKW7YoBwZZ3-oQhYPty5glFA2f1t5d9-sgZN7D5qguLvUu0gJ5mzkkaFS59f572LGCEeqqK1TrOMnCSN3TInVUlE1aijKwIs7LqcgJ0GFT8XYmfXrKSxhJjlTOsDViVG4ZasOnFAMux0_bIbjuU0qm8BI-w4BAWPs";
        private const string SearchUrl = ApiBaseUrl + "search";

        /*private static readonly object _lock = new object();

        private static string? Token
        {
            get
            {
                lock (_lock)
                {
                    string? token = Plugin.Instance?.Configuration.TVDBToken;
                    if (token == null)
                    {
                        var values = new Dictionary<string, string>()
                    {
                        { "apikey", ApiKey },
                        { "pin", "IPQZOCMN" }
                    };

                        LoginNode? jsonData = JsonSerializer.Deserialize<LoginNode>(HttpRequestHelper.SendUrlEncodedPostRequest(LoginUrl, values, false).Result);
                        if (Plugin.Instance == null || jsonData == null || jsonData.Data == null || jsonData.Data.Token == null)
                        {
                            return null;
                        }

                        Plugin.Instance.Configuration.TVDBToken = jsonData.Data.Token;
                        Plugin.Instance.SaveConfiguration();
                    }

                    return token;
                }
            }
        }*/

        /// <summary>
        /// Gets the id of the best matching serie.
        /// </summary>
        /// <param name="name">Name of the serie.<see cref="string"/>.</param>
        /// <returns>The id of the serie.</returns>
        public static async Task<int?> GetSerieID(string name)
        {
            var values = new Dictionary<string, string?>()
            {
                { "query", name },
                { "type", "series" }
            };

            JsonNode? jsonData = await HttpRequestHelper.SendAuthenticatedGetRequest(SearchUrl, values, Token, false).ConfigureAwait(true);
            SeriesSearchNode? node = jsonData.Deserialize<SeriesSearchNode>();
            if (node == null || node.Data == null || node.Data.Length < 1)
            {
                return null;
            }

            string? fullID = node.Data[0].ID;
            if (fullID == null) { return null; }

            int id;
            _ = int.TryParse(fullID.Split('-')[1], out id);
            return id;
        }

        /// <summary>
        /// Gets the info on a specific episode.
        /// </summary>
        /// <param name="serieID">TVDB ID for the serie.<see cref="string"/>.</param>
        /// <param name="season">Season number for the episode.<see cref="int"/>.</param>
        /// <returns>The information for the specified episode.</returns>
        public static async Task<EpisodeData[]?> GetEpisodesData(int serieID, int season)
        {
            var values = new Dictionary<string, string?>()
            {
                { "season", string.Empty + season }
            };

            string requestUrl = ApiBaseUrl + "series/" + serieID + "/episodes/default";
            JsonNode? jsonData = await HttpRequestHelper.SendAuthenticatedGetRequest(requestUrl, values, Token, false).ConfigureAwait(true);
            EpisodesSearchNode? node = jsonData.Deserialize<EpisodesSearchNode>();
            if (node == null || node.Data == null || node.Data.Episodes == null || node.Data.Episodes.Length < 1)
            {
                return null;
            }

            return node.Data.Episodes;
        }
    }
}