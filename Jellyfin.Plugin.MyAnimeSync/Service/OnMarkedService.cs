using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MyAnimeSync.Api.Mal;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MyAnimeSync.Service
{
    /// <inheritdoc/>
    public class OnMarkedService : IHostedService
    {
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

        private async Task<AnimeData?> GetAnimeSequel(AnimeData info, UserConfig userConfig)
        {
            RelatedAnime[]? nodes = info.RelatedNodes;
            if (nodes == null)
            {
                _logger.LogError(
                "Could not retrieve related animes for anime : {Title}",
                info.Title);
                return null;
            }

            RelatedAnime? relatedAnime = Array.Find(nodes, element => element.RelationType == "sequel");
            if (relatedAnime == null || relatedAnime.SearchEntry == null || relatedAnime.SearchEntry.ID == null)
            {
                _logger.LogError(
                "Could not retrieve sequel for anime : {Title}",
                info.Title);
                return null;
            }

            return await MalApiHandler.GetAnimeInfo(relatedAnime.SearchEntry.ID.Value, userConfig).ConfigureAwait(true);
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

                // Update tokens if needed before using the api.
                await MalApiHandler.RefreshTokens(userConfig).ConfigureAwait(true);
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

                    int? id = await MalApiHandler.GetAnimeID(serie, userConfig).ConfigureAwait(true);
                    if (id == null)
                    {
                        _logger.LogError(
                            "Could not retrieve id for anime : {AnimeName}",
                            serie);
                        return;
                    }

                    int? episodeNumber = episode.IndexNumber;
                    if (episodeNumber == null)
                    {
                        _logger.LogError(
                            "Could not retrieve episode number for : {AnimeName}",
                            serie);
                        return;
                    }

                    AnimeData? info = await MalApiHandler.GetAnimeInfo(id.Value, userConfig).ConfigureAwait(true);
                    if (info == null || info.ID == null || info.EpisodeCount == null)
                    {
                        _logger.LogError(
                            "Could not retrieve anime info for id : {ID}",
                            id);
                        return;
                    }

                    int seasonOffset = episode.AiredSeasonNumber - 1 ?? 0;

                    // If we have a specified anime season.
                    while (seasonOffset > 0)
                    {
                        info = await GetAnimeSequel(info, userConfig).ConfigureAwait(true);
                        if (info == null || info.ID == null || info.EpisodeCount == null || info.Title == null || info.MediaType == null)
                        {
                            _logger.LogError(
                                "Could not retrieve expected sequel using season offset for anime : {ID}",
                                id);
                            return;
                        }

                        // Ignore anime movie for season offset.
                        if (info.MediaType == MediaType.Movie)
                        {
                            continue;
                        }

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

                        seasonOffset--;
                    }

                    // If episode is > expect max season episode, we try to find the proper season.
                    // Also if the number of episodes is unknown, result is 0. So treat it as being the proper anime season.
                    while (info.EpisodeCount > 0 && info.EpisodeCount < episodeNumber)
                    {
                        episodeNumber -= info.EpisodeCount;
                        info = await GetAnimeSequel(info, userConfig).ConfigureAwait(true);
                        if (info == null || info.ID == null || info.EpisodeCount == null)
                        {
                            _logger.LogError(
                                "Could not retrieve expected sequel using episode offset for anime : {ID}",
                                id);
                            return;
                        }
                    }

                    // Retrieve anime status in user library.
                    UserAnimeInfo entry = await MalApiHandler.GetUserAnimeInfo(info.ID.Value, userConfig).ConfigureAwait(true);
                    if (!entry.SuccessStatus)
                    {
                        _logger.LogError(
                                "Could not parse anime list for user : {User}",
                                userID);
                        return;
                    }

                    if (entry.Info != null && entry.Info.StatusInfo != null)
                    {
                        // Do nothing if anime is already marked as completed or if the episode number is not higher then the user watched episode.
                        if (entry.Info.StatusInfo.Status == WatchStatus.Completed || entry.Info.StatusInfo.EpisodeWatched >= episodeNumber.Value)
                        {
                            _logger.LogInformation("No need to update user entry for anime : {Anime} season {Season} ep {Ep}", serie, episode.AiredSeasonNumber, entry.Info.StatusInfo.EpisodeWatched);
                            return;
                        }
                    }

                    bool completed = false;
                    if (info.EpisodeCount > 0 && episodeNumber >= info.EpisodeCount)
                    {
                        completed = true;
                    }

                    // Update anime status
                    MalApiHandler.UpdateUserInfo(info.ID.Value, episodeNumber.Value, completed, userConfig);
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