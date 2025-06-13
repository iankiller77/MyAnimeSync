using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Jellyfin.Plugin.MyAnimeSync.Api.Mal;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using Jellyfin.Plugin.MyAnimeSync.Service;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
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
        private readonly ILibraryManager _libraryManager;
        private readonly IUserDataManager _userDataManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="Endpoints"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{Endpoints}"/> interface.</param>
        /// /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        /// /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        public Endpoints(ILogger<Endpoints> logger, ILibraryManager libraryManager, IUserDataManager userDataManager)
        {
            _logger = logger;
            _libraryManager = libraryManager;
            _userDataManager = userDataManager;
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
        /// Synchronise entire jellyfin watch status with myanimelist.net.
        /// </summary>
        /// <param name="userID">The user config. <see cref="Guid"/>.</param>
        /// <returns> A task.</returns>
        [HttpGet("fullUpdate")]
        public async Task FullLibraryUpdate([FromQuery(Name = "guid")] Guid userID)
        {
            UserConfig? uConfig = Plugin.Instance?.Configuration.GetByGuid(userID);
            if (uConfig == null || string.IsNullOrEmpty(uConfig.UserToken))
            {
                return;
            }

            List<VirtualFolderInfo> virtualFolders = _libraryManager.GetVirtualFolders().FindAll(element => uConfig.ListMonitoredLibraryGuid.Contains(Guid.Parse(element.ItemId)));
            List<Folder> watchedFolders = new List<Folder>();
            foreach (VirtualFolderInfo virtualFolder in virtualFolders)
            {
                var folderItem = _libraryManager.FindByPath(virtualFolder.Locations[0], true);
                if (folderItem is Folder)
                {
                    Folder? folder = folderItem as Folder;
                    if (folder == null)
                    {
                        continue;
                    }

                    watchedFolders.Add(folder);
                }
            }

            foreach (Folder folder in watchedFolders)
            {
                /*
                    TODO: Investigate improving algorithm to avoid updating all episodes when season is not provided.
                            Also investigate improving overall code to reuse update logic for multiple anime at the same time.
                            EX: Create of a list of all animes to update before updating and creating a batch update function to optimize the amount of update requests and reuse code.
                */

                foreach (var serieItem in folder.Children)
                {
                    if (serieItem is Series)
                    {
                        Series? serie = serieItem as Series;
                        if (serie == null)
                        {
                            continue;
                        }

                        foreach (var item in serie.Children)
                        {
                            if (item is Episode)
                            {
                                Episode? episode = item as Episode;
                                if (episode == null || episode.IndexNumber == null)
                                {
                                    continue;
                                }

                                bool isPlayed = _userDataManager.GetUserData(userID, episode).Played;
                                if (isPlayed)
                                {
                                    await OnMarkedService.UpdateAnimeList(serie.Name, episode.IndexNumber.Value, episode.AiredSeasonNumber, uConfig, _logger).ConfigureAwait(false);
                                }
                            }
                            else if (item is Season)
                            {
                                Season? season = item as Season;
                                if (season == null)
                                {
                                    continue;
                                }

                                int maxEpisodeNumber = -1;
                                foreach (var item2 in season.Children)
                                {
                                    if (item2 is Episode)
                                    {
                                        Episode? episode = item2 as Episode;
                                        if (episode == null)
                                        {
                                            continue;
                                        }

                                        bool isPlayed = _userDataManager.GetUserData(userID, episode).Played;
                                        if (isPlayed)
                                        {
                                            if (episode.IndexNumber != null && episode.IndexNumber > maxEpisodeNumber)
                                            {
                                                maxEpisodeNumber = episode.IndexNumber.Value;
                                            }
                                        }
                                    }
                                }

                                if (maxEpisodeNumber > 0)
                                {
                                    await OnMarkedService.UpdateAnimeList(serie.Name, maxEpisodeNumber, season.IndexNumber, uConfig, _logger).ConfigureAwait(false);
                                }
                            }
                        }
                    }
                }
            }
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