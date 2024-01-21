using System;
using System.Security.Authentication;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Jellyfin.Plugin.MyAnimeSync.Api.Mal;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.MyAnimeSync.Endpoints
{
    /// <summary>
    /// Plugin endpoints.
    /// </summary>
    [ApiController]
    [Route("MyAnimeSync")]
    public class Endpoints : ControllerBase
    {
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
            UserConfig? uConfig = Plugin.Instance?.Configuration.GetAuthenticatingUserConfig();
            if (uConfig == null)
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
    }
}