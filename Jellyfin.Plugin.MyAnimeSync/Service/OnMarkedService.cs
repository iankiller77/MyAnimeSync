using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Emby.Naming.TV;
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

        internal static async Task<AnimeData?> GetAnimeSequel(AnimeData info, UserConfig userConfig, ILogger logger)
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
                AnimeData? sequel = null;
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
                    if (animeData != null)
                    {
                        if (animeData.MediaType == MediaType.SeasonalAnime) // Favor Seasonal Anime over anything else.
                        {
                            return animeData;
                        }
                        else if (animeData.MediaType != MediaType.Movie) // Ignore movie since we have more than 1 sequel type.
                        {
                            if (animeData.MediaType == MediaType.ONA) // Favor ONA over OVA.
                            {
                                sequel = animeData;
                            }
                            else if (sequel != null && sequel.MediaType != MediaType.ONA && animeData.MediaType == MediaType.OVA) // Favor OVA over other type.
                            {
                                sequel = animeData;
                            }
                            else if (sequel == null) // Only affect other type to sequel if no OVA and no ONA were found.
                            {
                                sequel = animeData;
                            }
                        }
                    }
                }

                return sequel;
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

        internal static bool UpdateUserList(string serie, int episodeNumber, int? seasonNumber, AnimeData info, UserConfig userConfig, ILogger logger)
        {
            if (info.ID == null) { return false; }

            // Only check and update user library while nothing is currently being updated.
            lock (GetLock(serie))
            {
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
            }
        }

        private static int CalculateMonthsBetweenDateStrings(string strDate1, string strDate2)
        {
            DateTime date1 = DateTime.ParseExact(strDate1, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime date2 = DateTime.ParseExact(strDate2, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            return Math.Abs(date2.Month - date1.Month + (12 * (date2.Year - date1.Year)));
        }

        internal static AnimeData? InternalRetrieveAnimeData(string serie, ref int episodeNumber, int? seasonNumber, UserConfig userConfig, ILogger logger, int? expectedYear = null, string? tvdbID = null)
        {
            // Try to validate the date on first anime fetched, also it should always be a tv episode or movie.
            int? id = MalApiHandler.GetAnimeID(serie, logger, userConfig, expectedYear, [MediaType.SeasonalAnime, MediaType.Movie]).Result;
            if (id == null)
            {
                logger.LogError(
                    "Could not retrieve id for anime : {AnimeName}",
                    serie);
                return null;
            }

            AnimeData? info = MalApiHandler.GetAnimeInfo(id.Value, userConfig).Result;
            if (info == null || info.ID == null || info.EpisodeCount == null || info.MediaType == null)
            {
                logger.LogError(
                    "Could not retrieve anime info for id : {ID}",
                    id);
                return null;
            }

            string serieType = info.MediaType;

            int seasonOffset = seasonNumber - 1 ?? 0;

            if (userConfig.UseAbsoluteEpisode && seasonOffset > 0)
            {
                // Check if long running show logic should be applied to this anime. (This include shows like Jojo's Bizarre Adventure)
                if
                (
                    userConfig.ForceAbsoluteEpisode ||
                    (info.Status == SeasonStatus.Airing && info.EndDate == null) ||
                    (info.Status == SeasonStatus.Finished && info.StartDate != null && info.EndDate != null && CalculateMonthsBetweenDateStrings(info.StartDate, info.EndDate) > 5)
                )
                {
                    if (tvdbID == null)
                    {
                        logger.LogError("Absolute episode was requested but could not retrieve TVDB episode id from jellyfin metadatas for : {Anime} season {Season} ep {Episode}", serie, seasonNumber, episodeNumber);
                        return null;
                    }

                    EpisodeData? data = TVDBApiHandler.GetEpisodeData(tvdbID).Result;
                    if (data == null || data.AbsoluteEpisodeNumber == null)
                    {
                        logger.LogError("Could not retrieve episode data for tvdb episode id : {EpisodeID}", tvdbID);
                        return null;
                    }

                    seasonOffset = 0;
                    episodeNumber = data.AbsoluteEpisodeNumber.Value;
                }
            }

            // If we have a specified anime season.
            while (seasonOffset > 0)
            {
                info = GetAnimeSequel(info, userConfig, logger).Result;
                if (info == null || info.ID == null || info.EpisodeCount == null || info.Title == null || info.MediaType == null || info.AlternativeTitles == null || info.AlternativeTitles.EnglishTitle == null)
                {
                    logger.LogError(
                        "Could not retrieve expected sequel using season offset for anime : {ID}",
                        id);

                    // TODO: Check if we can try to find the absolute episode number instead of the season number. (For anime like one piece)
                    // int? tvdbID = await TVDBApiHandler.GetSerieID(serie).ConfigureAwait(true);
                    return null;
                }

                // Ignore anime movie and OVA for season offset.
                if (info.MediaType == MediaType.Movie || (serieType != MediaType.OVA && info.MediaType == MediaType.OVA) || info.MediaType == MediaType.TVSpecial)
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
                info = GetAnimeSequel(info, userConfig, logger).Result;
                if (info == null || info.ID == null || info.EpisodeCount == null)
                {
                    logger.LogError(
                        "Could not retrieve expected sequel using episode offset for anime : {ID}",
                        id);
                    return null;
                }
            }

            return info;
        }

        internal static async Task<bool> InternalUpdateAnimeList(UpdateEntry episode, bool useOriginalTitle, UserConfig userConfig, ILogger logger)
        {
            // Update tokens if needed before using the api.
            if (!await MalApiHandler.RefreshTokens(userConfig).ConfigureAwait(true))
            {
                logger.LogError("Could not update token for user: {UserID}", userConfig.Id);
            }

            string serie = episode.Serie;

            if (useOriginalTitle)
            {
                if (string.IsNullOrWhiteSpace(episode.OriginalSerieTitle))
                {
                    logger.LogError("The use of original serie title was requested but is not defined by jellyfin for serie: {Serie}", serie);
                    return false;
                }

                serie = episode.OriginalSerieTitle;
            }

            int episodeNumber = episode.EpisodeNumber;
            int seasonNumber = episode.SeasonNumber;

            AnimeData? info = null;
            await Task.Run(() =>
            {
                info = InternalRetrieveAnimeData(serie, ref episodeNumber, seasonNumber, userConfig, logger, episode.StartYear, episode.TVDBEpisodeID);
            }).ConfigureAwait(true);

            if (info == null)
            {
                return false;
            }

            return UpdateUserList(serie, episodeNumber, seasonNumber, info, userConfig, logger);
        }

        internal static async Task<bool> InternalUpdateAnimeListSpecial(UpdateEntry episode, bool useOriginalTitle, UserConfig userConfig, ILogger logger)
        {
            string serie = episode.Serie;
            int episodeNumber = episode.EpisodeNumber;

            if (!userConfig.AllowSpecials)
            {
                logger.LogWarning("Updating special episodes is disabled, skipping the update for {Serie} special #{EpisodeNumber}!", serie, episodeNumber);
                return true;
            }

            if (!await MalApiHandler.RefreshTokens(userConfig).ConfigureAwait(true))
            {
                logger.LogError("Could not update token for user: {UserID}", userConfig.Id);
            }

            if (useOriginalTitle)
            {
                if (string.IsNullOrWhiteSpace(episode.OriginalSerieTitle))
                {
                    logger.LogError("The use of original serie title was requested but is not defined by jellyfin for serie: {Serie}", serie);
                    return false;
                }

                serie = episode.OriginalSerieTitle;
            }

            int? tvdbID = await TVDBApiHandler.GetSerieID(serie).ConfigureAwait(true);
            if (tvdbID == null)
            {
                logger.LogError("Could not retrieve ID from tvdb for : {SerieName}", serie);
                return false;
            }

            EpisodeData[]? episodes = await TVDBApiHandler.GetSeasonEpisodes(tvdbID.Value, 0).ConfigureAwait(true);
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

            int? id = await MalApiHandler.GetAnimeID(episodeName, logger, userConfig).ConfigureAwait(true);
            if (id != null)
            {
                info = await MalApiHandler.GetAnimeInfo(id.Value, userConfig).ConfigureAwait(true);
            }

            if (info == null || info.ID == null || info.EpisodeCount == null || info.MediaType == null || info.MediaType == MediaType.SeasonalAnime)
            {
                episodeName = episodeName.Split('-')[0].Trim();
                int? newID = await MalApiHandler.GetAnimeID(episodeName, logger, userConfig).ConfigureAwait(true);
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
        /// <param name="episode">Object containing episode information.<see cref="Episode"/>.</param>
        /// <param name="userConfig">The user config. <see cref="UserConfig"/>.</param>
        /// <param name="logger">The logger. <see cref="ILogger"/>.</param>
        /// <returns> The task. </returns>
        public static async Task<bool> UpdateAnimeList(UpdateEntry episode, UserConfig userConfig, ILogger logger)
        {
            bool success;

            string serie = episode.Serie;
            bool fallbackSearch = userConfig.OriginalTitleSearchFallback;

            int seasonNumber = episode.SeasonNumber;

            // If this is a retry, try to validate if the jellyfin metadata changed.
            // Ignore updated metadata if the user updated the entry.
            if (!episode.UserEdited && episode.TryCount > 0 && episode.SerieID != Guid.Empty)
            {
                Series? serieInfo = BaseItem.LibraryManager.GetItemById(episode.SerieID) as Series;
                if (serieInfo == null)
                {
                    logger.LogError("Could not validate serie metadata for serie with id : {SerieID} - Metadata validation will be skipped and old metadata will be used!", episode.SerieID);
                }
                else
                {
                    if (serieInfo.Name != episode.Serie || serieInfo.OriginalTitle != episode.OriginalSerieTitle)
                    {
                        logger.LogWarning(
                            """
                            Detected metadata change for serie with id : {SerieID} - Metadata will be updated!
                            New series name : {SeriesName} and new series original title : {OriginalName}
                            """,
                            episode.SerieID,
                            serieInfo.Name,
                            serieInfo.OriginalTitle);

                        episode.Serie = serieInfo.Name;
                        episode.OriginalSerieTitle = serieInfo.OriginalTitle;
                    }
                }
            }

            if (seasonNumber > 0)
            {
                success = await InternalUpdateAnimeList(episode, userConfig.OriginalTitleSearch, userConfig, logger).ConfigureAwait(true);
                // Determine if the fallback to default search name applies
                if (fallbackSearch && !success)
                {
                    success = await InternalUpdateAnimeList(episode, !userConfig.OriginalTitleSearch, userConfig, logger).ConfigureAwait(true);
                }
            }
            else
            {
                success = await InternalUpdateAnimeListSpecial(episode, userConfig.OriginalTitleSearch, userConfig, logger).ConfigureAwait(true);
                // Determine if the fallback to default search name applies
                if (fallbackSearch && !success)
                {
                    success = await InternalUpdateAnimeListSpecial(episode, !userConfig.OriginalTitleSearch, userConfig, logger).ConfigureAwait(true);
                }
            }

            userConfig.UpdateFailEntries(episode, success: success);

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
                    List<VirtualFolderInfo> virtualFolders = _libraryManager.GetVirtualFolders();
                    Folder? folder = episode.Series.GetParent() as Folder;
                    if (folder == null)
                    {
                        _logger.LogError(
                            "Could not retrieve folder associated with episode : {AnimeName}",
                            episode.SeriesName);
                        return;
                    }

                    if (!virtualFolders.Any(element => element.Locations.Contains(folder.ContainingFolderPath) && userConfig.ListMonitoredLibraryGuid.Contains(Guid.Parse(element.ItemId))))
                    {
                        return;
                    }

                    UpdateEntry? entry = UpdateEntry.GenerateEntryFromEpisode(episode, _logger);

                    if (entry != null)
                    {
                        await UpdateAnimeList(entry, userConfig, _logger).ConfigureAwait(false);
                    }
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