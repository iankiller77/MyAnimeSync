using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Jellyfin.Plugin.MyAnimeSync.HttpHelper;

namespace Jellyfin.Plugin.MyAnimeSync.Api.TVDB
{
    /// <summary>
    /// Class used to make api request to TVDB.
    /// </summary>
    public static class TVDBApiHandler
    {
        private const string ApiKey = "4efaf949-f182-453a-ae02-cbb225f65ab6";
        private const string ApiBaseUrl = "https://api4.thetvdb.com/v4/";
        private const string LoginUrl = ApiBaseUrl + "login";
        private const string SearchUrl = ApiBaseUrl + "search";
        private static readonly object _lock = new object();

        private static string? Token
        {
            get
            {
                lock (_lock)
                {
                    string? token = Plugin.Instance?.Configuration.TVDBToken;
                    DateTime? tokenGenerationDate = Plugin.Instance?.Configuration.TVDBTokenGenerationDate;
                    if (token == null || tokenGenerationDate == null || DateTime.Today.Subtract(tokenGenerationDate.Value).Days >= 20)
                    {
                        var values = new Dictionary<string, string>()
                        {
                            { "apikey", ApiKey },
                            { "pin", string.Empty }
                        };

                        LoginNode? jsonData = JsonSerializer.Deserialize<LoginNode>(HttpRequestHelper.SendJsonPostRequest(LoginUrl, values, false).Result);
                        if (Plugin.Instance == null || jsonData == null || jsonData.Data == null || jsonData.Data.Token == null)
                        {
                            return null;
                        }

                        token = jsonData.Data.Token;
                        Plugin.Instance.Configuration.TVDBToken = token;
                        Plugin.Instance.Configuration.TVDBTokenGenerationDate = DateTime.Today;
                        Plugin.Instance.SaveConfiguration();
                    }

                    return token;
                }
            }
        }

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

            string? token = Token;
            if (token == null)
            {
                return null;
            }

            JsonNode? jsonData = await HttpRequestHelper.SendAuthenticatedGetRequest(SearchUrl, values, token, false).ConfigureAwait(true);
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

            string? token = Token;
            if (token == null)
            {
                return null;
            }

            string requestUrl = ApiBaseUrl + "series/" + serieID + "/episodes/default";
            JsonNode? jsonData = await HttpRequestHelper.SendAuthenticatedGetRequest(requestUrl, values, token, false).ConfigureAwait(true);
            EpisodesSearchNode? node = jsonData.Deserialize<EpisodesSearchNode>();
            if (node == null || node.Data == null || node.Data.Episodes == null || node.Data.Episodes.Length < 1)
            {
                return null;
            }

            return node.Data.Episodes;
        }
    }
}