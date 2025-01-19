using System;
using System.Security.Authentication;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Jellyfin.Plugin.MyAnimeSync.Api.Mal;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using Jellyfin.Plugin.MyAnimeSync.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MyAnimeSync.Endpoints
{
    /// <summary>
    /// Plugin endpoints.
    /// </summary>
    [ApiController]
    [Route("MyAnimeSync")]
    public class Endpoints : ControllerBase
    {
        private readonly ILogger<Endpoints> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Endpoints"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{Endpoints}"/> interface.</param>
        public Endpoints(ILogger<Endpoints> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Retrieve the code from the callback url for the user authentification.
        /// </summary>
        /// <param name="code">The user authorisation code. <see cref="string"/>.</param>
        [HttpGet("apicode")]
        public async void RetrieveApiCode([FromQuery(Name = "code")] string code)
        {
            UserConfig? uConfig = Plugin.Instance?.Configuration.GetAuthenticatingUserConfig();
            if (uConfig == null)
            {
                throw new AuthenticationException("No valid user is currently being authenticated.");
            }

            await MalApiHandler.GetNewTokens(code, uConfig).ConfigureAwait(false);
        }

        /// <summary>
        /// Generate url to be used for authentication.
        /// </summary>
        /// <returns>Url to be used for api authentication.</returns>
        [HttpGet("generateUrl")]
        public string? GenerateAuthCodeUrl()
        {
            UserConfig? uConfig = Plugin.Instance?.Configuration.GetAuthenticatingUserConfig();
            if (uConfig == null)
            {
                throw new AuthenticationException("No valid user is currently being authenticated.");
            }

            return MalApiHandler.GenerateAuthCodeUrl(uConfig);
        }

        /// <summary>
        /// Test user config by making a query to mal api.
        /// </summary>
        /// <param name="guid">The user config. <see cref="Guid"/>.</param>
        /// <returns>Return the success status of the test.</returns>
        [HttpGet("testConfig")]
        public async Task<bool> TestUserConfig([FromQuery(Name = "guid")] Guid guid)
        {
            UserConfig? uConfig = Plugin.Instance?.Configuration.GetByGuid(guid);
            if (uConfig == null || string.IsNullOrEmpty(uConfig.UserToken))
            {
                return false;
            }

            JsonNode? info = await MalApiHandler.GetAnimeID("Jujutsu Kaisen", uConfig).ConfigureAwait(true);
            if (info == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Try to update user watch list with specified entry info.
        /// </summary>
        /// <param name="guid">The user config. <see cref="Guid"/>.</param>
        /// <param name="serie">The name of the serie. <see cref="string"/>.</param>
        /// <param name="season">The season of the entry. <see cref="int"/>.</param>
        /// <param name="episode">The episode of the entry. <see cref="int"/>.</param>
        /// <returns>Return the success status of the request.</returns>
        [HttpGet("retryUpdate")]
        public async Task<bool> RetryUpdate([FromQuery] Guid guid, [FromQuery] string serie, [FromQuery] int season, [FromQuery] int episode)
        {
            UserConfig? uConfig = Plugin.Instance?.Configuration.GetByGuid(guid);
            if (uConfig == null || string.IsNullOrEmpty(uConfig.UserToken))
            {
                return false;
            }

            bool success = await OnMarkedService.UpdateAnimeList(serie, episode, season, uConfig, _logger).ConfigureAwait(true);
            if (success)
            {
                UpdateEntry? entry = uConfig.GetUpdateEntry(serie, season);
                if (entry == null)
                {
                    _logger.LogError("Could not retrieve UpdateEntry while trying to delete failed entry from list.");
                    return false;
                }

                uConfig.UpdateRetrySuccess(entry);
            }

            return success;
        }
    }
}