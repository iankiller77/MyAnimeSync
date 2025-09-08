using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace Jellyfin.Plugin.MyAnimeSync.HttpHelper
{
    /// <summary>
    /// Class used to make basic http requests.
    /// </summary>
    public static class HttpRequestHelper
    {
        private static readonly object _lock = new object();
        private static List<DateTime> _requestList = new List<DateTime>();

        private static void ThrottleApiRequests()
        {
            lock (_lock)
            {
                // Limit to 250 requests per 5 minutes.
                _requestList.RemoveAll(element => DateTime.Now.Subtract(element).TotalMinutes > 5);
                _requestList.Add(DateTime.Now);
                while (_requestList.Count >= 250)
                {
                    Thread.Sleep(30000);
                    _requestList.RemoveAll(element => DateTime.Now.Subtract(element).TotalMinutes > 5);
                }
            }
        }

        /// <summary>
        /// Send an Encoded Http Post Request.
        /// </summary>
        /// <param name="url">The url for the http request.<see cref="string"/>.</param>
        /// <param name="values">A dictionary of values for the request.<see cref="Dictionary{TKey, TValue}"/>.</param>
        /// <param name="throttling">A flag to determine if we should throttle requests.<see cref="bool"/>.</param>
        /// <returns>The json returned by the http request.</returns>
        public static async Task<JsonNode?> SendUrlEncodedPostRequest(string url, Dictionary<string, string> values, bool throttling)
        {
            HttpClient httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(values);

            try
            {
                if (throttling)
                {
                    ThrottleApiRequests();
                }

                var response = await httpClient.PostAsync(url, content).ConfigureAwait(true);
                if (!response.IsSuccessStatusCode)
                {
                    throw new AuthenticationException("Could not retrieve provider token");
                }

                StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(true));
                JsonNode? jsonData = JsonObject.Parse(await reader.ReadToEndAsync().ConfigureAwait(true));
                if (jsonData == null)
                {
                    throw new AuthenticationException("Could not retrieve token from request.");
                }

                return jsonData;
            }
            catch { return null; }
        }

        /// <summary>
        /// Send an Encoded Http Get Request.
        /// </summary>
        /// <param name="url">The url for the http request.<see cref="string"/>.</param>
        /// <param name="token">The token used for authentication.<see cref="string"/>.</param>
        /// <param name="throttling">A flag to determine if we should throttle requests.<see cref="bool"/>.</param>
        /// <returns>The json returned by the http request.</returns>
        public static async Task<JsonNode?> SendAuthenticatedGetRequest(string url, string token, bool throttling)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                if (!throttling)
                {
                    ThrottleApiRequests();
                }

                var response = await httpClient.GetAsync(url).ConfigureAwait(true);
                if (!response.IsSuccessStatusCode)
                {
                    throw new AuthenticationException("Authenticated get request returned an error response.");
                }

                StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(true));
                JsonNode? jsonData = JsonObject.Parse(await reader.ReadToEndAsync().ConfigureAwait(true));

                return jsonData;
            }
            catch { return null; }
        }

        /// <summary>
        /// Send an Encoded Http Get Request.
        /// </summary>
        /// <param name="url">The url for the http request.<see cref="string"/>.</param>
        /// <param name="values">A dictionary of values for the request.<see cref="Dictionary{TKey, TValue}"/>.</param>
        /// <param name="token">The token used for authentication.<see cref="string"/>.</param>
        /// <param name="throttling">A flag to determine if we should throttle requests.<see cref="bool"/>.</param>
        /// <returns>The json returned by the http request.</returns>
        public static async Task<JsonNode?> SendAuthenticatedGetRequest(string url, Dictionary<string, string?> values, string token, bool throttling)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var uri = new Uri(QueryHelpers.AddQueryString(url, values));

            try
            {
                if (throttling)
                {
                    ThrottleApiRequests();
                }

                var response = await httpClient.GetAsync(uri).ConfigureAwait(true);
                if (!response.IsSuccessStatusCode)
                {
                    throw new AuthenticationException("Authenticated get request returned an error response.");
                }

                StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(true));
                JsonNode? jsonData = JsonObject.Parse(await reader.ReadToEndAsync().ConfigureAwait(true));

                return jsonData;
            }
            catch { return null; }
        }

        /// <summary>
        /// Send an Encoded Http Patch Request.
        /// </summary>
        /// <param name="url">The url for the http request.<see cref="string"/>.</param>
        /// <param name="values">A dictionary of values for the request.<see cref="Dictionary{TKey, TValue}"/>.</param>
        /// <param name="token">The token used for authentication.<see cref="string"/>.</param>
        /// <param name="throttling">A flag to determine if we should throttle requests.<see cref="bool"/>.</param>
        /// <returns>The json returned by the http request.</returns>
        public static async Task<JsonNode?> SendPatchRequest(string url, Dictionary<string, string?> values, string token, bool throttling)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var content = new FormUrlEncodedContent(values);

            try
            {
                if (throttling)
                {
                    ThrottleApiRequests();
                }

                var response = await httpClient.PatchAsync(url, content).ConfigureAwait(true);
                if (!response.IsSuccessStatusCode)
                {
                    throw new AuthenticationException("Authenticated patch request returned an error response.");
                }

                StreamReader reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(true));
                JsonNode? jsonData = JsonObject.Parse(await reader.ReadToEndAsync().ConfigureAwait(true));

                return jsonData;
            }
            catch { return null; }
        }
    }
}