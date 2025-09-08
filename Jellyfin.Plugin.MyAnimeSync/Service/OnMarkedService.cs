using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MyAnimeSync.Api.Mal;
using Jellyfin.Plugin.MyAnimeSync.Api.TVDB;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MyAnimeSync.Service
{
    /// <inheritdoc/>
    public class OnMarkedService : IHostedService
    {
        private static readonly object _dictLock = new object();
        private static Dictionary<string, object> _updateLocks = new Dictionary<string, object>();
        private readonly ILogger<OnMarkedService> _logger;
        private readonly IUserDataManager _userDataManager;
        private readonly ILibraryManager _libraryManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnMarkedService"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{OnMarkedService}"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
        public OnMarkedService(ILogger<OnMarkedService> logger, IUserDataManager userDataManager, ILibraryManager libraryManager)
        {
            _logger = logger;
            _userDataManager = userDataManager;
            _libraryManager = libraryManager;
        }

        /// <inheritdoc/>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _userDataManager.UserDataSaved += OnUserDataMarkedPlayed;
            return Task.CompletedTask;
        }

        private static async Task<AnimeData?> GetAnimeSequel(AnimeData info, UserConfig userConfig, ILogger logger)
        {
            RelatedAnime[]? nodes = info.RelatedNodes;
            if (nodes == null)
            {
                logger.LogError(
                "Could not retrieve related animes for anime : {Title}",
                info.Title);
                return null;
            }

            // If we have more than one sequel, one of them is a movie. Always return the tv version.
            RelatedAnime[]? relatedAnimes = Array.FindAll(nodes, element => element.RelationType == "sequel");
            RelatedAnime? relatedAnime = null;
            if (relatedAnimes.Length == 1)
            {
                relatedAnime = relatedAnimes[0];
            }
            else
            {
                foreach (RelatedAnime anime in relatedAnimes)
                {
                    if (anime == null || anime.SearchEntry == null || anime.SearchEntry.ID == null)
                    {
                        logger.LogError(
                        "Could not retrieve sequel data for anime with multiple sequel : {Title}",
                        info.Title);
                        return null;
                    }

                    AnimeData? animeData = await MalApiHandler.GetAnimeInfo(anime.SearchEntry.ID.Value, userConfig).ConfigureAwait(true);
                    if (animeData != null && animeData.MediaType == MediaType.SeasonalAnime)
                    {
                        return animeData;
                    }
                }
            }

            if (relatedAnime == null || relatedAnime.SearchEntry == null || relatedAnime.SearchEntry.ID == null)
            {
                logger.LogError(
                "Could not retrieve sequel for anime : {Title}",
                info.Title);
                return null;
            }

            return await MalApiHandler.GetAnimeInfo(relatedAnime.SearchEntry.ID.Value, userConfig).ConfigureAwait(true);
        }

        private static object GetLock(string anime)
        {
            lock (_dictLock)
            {
                if (_updateLocks.TryGetValue(anime, out object? uLock))
                {
                    return uLock;
                }

                _updateLocks.Add(anime, new object());
                return _updateLocks[anime];
            }
        }

        private static bool UpdateUserList(string serie, int episodeNumber, int? seasonNumber, AnimeData info, UserConfig userConfig, ILogger logger)
        {
            if (info.ID == null) { return false; }

            // Only check and update user library while nothing is currently being updated.
            lock (GetLock(serie))
            {
#pragma warning disable CA1849
                // Retrieve anime status in user library.
                UserAnimeInfo entry = MalApiHandler.GetUserAnimeInfo(info.ID.Value, userConfig).Result;
                if (!entry.SuccessStatus)
                {
                    logger.LogError(
                            "Could not parse anime list for user : {User}",
                            userConfig.Id);
                    return false;
                }

                if (entry.Info != null && entry.Info.StatusInfo != null)
                {
                    // Do nothing if anime is already marked as completed or if the episode number is not higher then the user watched episode.
                    if (entry.Info.StatusInfo.Status == WatchStatus.Completed || entry.Info.StatusInfo.EpisodeWatched >= episodeNumber)
                    {
                        logger.LogInformation("No need to update user entry for anime : {Anime} season {Season} ep {Ep}, watch status {LastWatched}", serie, seasonNumber, episodeNumber, entry.Info.StatusInfo.EpisodeWatched);
                        return true;
                    }
                }

                if (info.EpisodeCount > 0 && episodeNumber > info.EpisodeCount)
                {
                    logger.LogError("Unexpected episode number : {Episode} for {Anime} season {Season}", episodeNumber, serie, seasonNumber);
                    return false;
                }

                string status = WatchStatus.Watching;
                if (info.EpisodeCount > 0 && episodeNumber >= info.EpisodeCount)
                {
                    status = WatchStatus.Completed;
                }

                // Update anime status
                return MalApiHandler.UpdateUserInfo(info.ID.Value, episodeNumber, status, userConfig).Result;
#pragma warning restore CA1849
            }
        }

        private static async Task<bool> InternalUpdateAnimeList(string serie, int episodeNumber, int? seasonNumber, UserConfig userConfig, ILogger logger)
        {
            // Update tokens if needed before using the api.
            await MalApiHandler.RefreshTokens(userConfig).ConfigureAwait(true);

            int? id = await MalApiHandler.GetAnimeID(serie, userConfig).ConfigureAwait(true);
            if (id == null)
            {
                logger.LogError(
                    "Could not retrieve id for anime : {AnimeName}",
                    serie);
                return false;
            }

            AnimeData? info = await MalApiHandler.GetAnimeInfo(id.Value, userConfig).ConfigureAwait(true);
            if (info == null || info.ID == null || info.EpisodeCount == null)
            {
                logger.LogError(
                    "Could not retrieve anime info for id : {ID}",
                    id);
                return false;
            }

            int seasonOffset = seasonNumber - 1 ?? 0;

            // If we have a specified anime season.
            while (seasonOffset > 0)
            {
                info = await GetAnimeSequel(info, userConfig, logger).ConfigureAwait(true);
                if (info == null || info.ID == null || info.EpisodeCount == null || info.Title == null || info.MediaType == null || info.AlternativeTitles == null || info.AlternativeTitles.EnglishTitle == null)
                {
                    logger.LogError(
                        "Could not retrieve expected sequel using season offset for anime : {ID}",
                        id);
                    return false;
                }

                // Ignore anime movie for season offset.
                if (info.MediaType == MediaType.Movie)
                {
                    continue;
                }

                // Check if it's the first part of a season with original title.
                Regex expression = new Regex(".*part ([0-9]+).*", RegexOptions.IgnoreCase);
                Match match = expression.Match(info.Title);
                if (match.Success)
                {
                    int partNumber;
                    _ = int.TryParse(match.Groups[1].Value, out partNumber);
                    if (partNumber > 1)
                    {
                        continue;
                    }
                }

                // Check if it's the first part of a season with english title.
                match = expression.Match(info.AlternativeTitles.EnglishTitle);
                if (match.Success)
                {
                    int partNumber;
                    _ = int.TryParse(match.Groups[1].Value, out partNumber);
                    if (partNumber > 1)
                    {
                        continue;
                    }
                }

                seasonOffset--;
            }

            // If episode is > expect max season episode, we try to find the proper season.
            // Also if the number of episodes is unknown, result is 0. So treat it as being the proper anime season.
            while (info.EpisodeCount > 0 && info.EpisodeCount < episodeNumber)
            {
                episodeNumber -= info.EpisodeCount.Value;
                info = await GetAnimeSequel(info, userConfig, logger).ConfigureAwait(true);
                if (info == null || info.ID == null || info.EpisodeCount == null)
                {
                    logger.LogError(
                        "Could not retrieve expected sequel using episode offset for anime : {ID}",
                        id);
                    return false;
                }
            }

            return UpdateUserList(serie, episodeNumber, seasonNumber, info, userConfig, logger);
        }

        private static async Task<bool> InternalUpdateAnimeListSpecial(string serie, int episodeNumber, UserConfig userConfig, ILogger logger)
        {
            if (!userConfig.AllowSpecials)
            {
                logger.LogWarning("Updating special episodes is disabled, skipping the update for {Serie} special #{EpisodeNumber}!", serie, episodeNumber);
                return true;
            }

            await MalApiHandler.RefreshTokens(userConfig).ConfigureAwait(true);

            int? tvdbID = await TVDBApiHandler.GetSerieID(serie).ConfigureAwait(true);
            if (tvdbID == null)
            {
                logger.LogError("Could not retrieve ID from tvdb for : {SerieName}", serie);
                return false;
            }

            EpisodeData[]? episodes = await TVDBApiHandler.GetEpisodesData(tvdbID.Value, 0).ConfigureAwait(true);
            if (episodes == null || episodes.Length < 1)
            {
                logger.LogError("Could not retrieve episodes data for {Serie} season {Season}", serie, 0);
                return false;
            }

            EpisodeData? episodeData = Array.Find(episodes, episode => episode.EpisodeNumber == episodeNumber);
            if (episodeData == null || episodeData.Name == null)
            {
                logger.LogError("Could not retrieve name from tvdb for episode : {Serie} season {Season} episode {Episode}", serie, 0, episodeNumber);
                return false;
            }

            // Update to use episode name.
            string episodeName = episodeData.Name;

            AnimeData? info = null;

            int? id = await MalApiHandler.GetAnimeID(episodeName, userConfig).ConfigureAwait(true);
            if (id != null)
            {
                info = await MalApiHandler.GetAnimeInfo(id.Value, userConfig).ConfigureAwait(true);
            }

            if (info == null || info.ID == null || info.EpisodeCount == null || info.MediaType == null || info.MediaType == MediaType.SeasonalAnime)
            {
                episodeName = episodeName.Split('-')[0].Trim();
                int? newID = await MalApiHandler.GetAnimeID(episodeName, userConfig).ConfigureAwait(true);
                if (newID == null)
                {
                    logger.LogError(
                    "Could not retrieve id for anime : {AnimeName} - {EpisodeName}", serie, episodeName);
                    return false;
                }

                info = await MalApiHandler.GetAnimeInfo(newID.Value, userConfig).ConfigureAwait(true);
                if (info == null || info.ID == null || info.EpisodeCount == null || info.MediaType == null || info.MediaType == MediaType.SeasonalAnime)
                {
                    logger.LogError(
                        "Could not retrieve expected sequel using episode offset for anime : {ID}", id);
                    return false;
                }
            }

            // TODO: Try to match with proper episode number for the special!
            episodeNumber = info.EpisodeCount.Value;

            return UpdateUserList(episodeName, episodeNumber, 0, info, userConfig, logger);
        }

        /// <summary>
        /// Update anime watch list on MyAnimeList when an episode is marked as watched.
        /// </summary>
        /// <param name="serie">The serie's name.<see cref="string"/>.</param>
        /// <param name="episodeNumber">The episode number.<see cref="int"/>.</param>
        /// <param name="seasonNumber">The season number.<see cref="int"/>.</param>
        /// <param name="userConfig">The user config. <see cref="UserConfig"/>.</param>
        /// <param name="logger">The logger. <see cref="ILogger"/>.</param>
        /// <returns> The task. </returns>
        public static async Task<bool> UpdateAnimeList(string serie, int episodeNumber, int? seasonNumber, UserConfig userConfig, ILogger logger)
        {
            bool success;

            if (seasonNumber == null || seasonNumber > 0)
            {
                success = await InternalUpdateAnimeList(serie, episodeNumber, seasonNumber, userConfig, logger).ConfigureAwait(true);
            }
            else
            {
                success = await InternalUpdateAnimeListSpecial(serie, episodeNumber, userConfig, logger).ConfigureAwait(true);
            }

            userConfig.UpdateFailEntries(serie, episodeNumber, seasonNumber ?? 1, success: success);

            return success;
        }

        /// <summary>
        /// Update anime watch list on MyAnimeList when an episode is marked as watched.
        /// </summary>
        /// <param name="sender">Sender.<see cref="object"/>.</param>
        /// <param name="eventArgs">Informations about the event.<see cref="UserDataSaveEventArgs"/>.</param>
        private async void OnUserDataMarkedPlayed(object? sender, UserDataSaveEventArgs eventArgs)
        {
            // If we have a new video marked as played.
            if ((eventArgs.SaveReason == UserDataSaveReason.TogglePlayed || eventArgs.SaveReason == UserDataSaveReason.PlaybackFinished) && eventArgs.UserData.Played)
            {
                // Check if the user has a config!
                var userID = eventArgs.UserId;
                var userConfig = Plugin.Instance?.Configuration.GetByGuid(userID);
                if (userConfig == null || string.IsNullOrEmpty(userConfig.UserToken))
                {
                    _logger.LogError(
                        "User {UserName} does not have anime backup setup.",
                        eventArgs.UserId);
                    return;
                }

                if (eventArgs.Item is Episode episode)
                {
                    string serie = episode.SeriesName;
                    List<VirtualFolderInfo> virtualFolders = _libraryManager.GetVirtualFolders();
                    Folder? folder = episode.Series.GetParent() as Folder;
                    if (folder == null)
                    {
                        _logger.LogError(
                            "Could not retrieve folder associated with episode : {AnimeName}",
                            serie);
                        return;
                    }

                    if (!virtualFolders.Any(element => element.Locations.Contains(folder.ContainingFolderPath) && userConfig.ListMonitoredLibraryGuid.Contains(Guid.Parse(element.ItemId))))
                    {
                        return;
                    }

                    int? episodeNumber = episode.IndexNumber;
                    if (episodeNumber == null || episode.AiredSeasonNumber == null)
                    {
                        _logger.LogError(
                            "Could not retrieve episode number for : {AnimeName}",
                            serie);
                        return;
                    }

                    await UpdateAnimeList(serie, episodeNumber.Value, episode.AiredSeasonNumber, userConfig, _logger).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Dispoe all resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _userDataManager.UserDataSaved -= OnUserDataMarkedPlayed;
            }
        }

        /// <inheritdoc/>
        public Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose(true);
            return Task.CompletedTask;
        }
    }
}