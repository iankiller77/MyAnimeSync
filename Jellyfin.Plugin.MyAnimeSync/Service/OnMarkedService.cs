using System;
using System.Threading.Tasks;
using Jellyfin.Plugin.MyAnimeSync.Api.Mal;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MyAnimeSync.Service
{
    /// <inheritdoc/>
    public class OnMarkedService : IServerEntryPoint
    {
        private readonly ILogger<OnMarkedService> _logger;
        private readonly IUserDataManager _userDataManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnMarkedService"/> class.
        /// </summary>
        /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
        /// <param name="logger">Instance of the <see cref="ILogger{OnMarkedService}"/> interface.</param>
        /// <param name="userDataManager">Instance of the <see cref="IUserDataManager"/> interface.</param>
        public OnMarkedService(ISessionManager sessionManager, ILogger<OnMarkedService> logger, IUserDataManager userDataManager)
        {
            _logger = logger;
            _userDataManager = userDataManager;
        }

        /// <inheritdoc/>
        public Task RunAsync()
        {
            _userDataManager.UserDataSaved += OnUserDataMarkedPlayed;
            return Task.CompletedTask;
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

                    // If episode is > expect max season episode, we try to find the proper season.
                    // Also if the number of episodes is unknown, result is 0. So treat it as being the proper anime season.
                    while (info.EpisodeCount > 0 && info.EpisodeCount < episodeNumber)
                    {
                        episodeNumber -= info.EpisodeCount;
                        RelatedAnime[]? nodes = info.RelatedNodes;
                        if (nodes == null)
                        {
                            _logger.LogError(
                            "Could not retrieve related animes for anime : {ID}",
                            id);
                            return;
                        }

                        RelatedAnime? relatedAnime = Array.Find(nodes, element => element.RelationType == "sequel");
                        if (relatedAnime == null || relatedAnime.SearchEntry == null || relatedAnime.SearchEntry.ID == null)
                        {
                            _logger.LogError(
                            "Could not retrieve sequel for anime : {ID}",
                            id);
                            return;
                        }

                        info = await MalApiHandler.GetAnimeInfo(relatedAnime.SearchEntry.ID.Value, userConfig).ConfigureAwait(true);
                        if (info == null || info.ID == null || info.EpisodeCount == null)
                        {
                            _logger.LogError(
                                "Could not retrieve anime info for anime related to anime : {ID}",
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

                    if (entry.Info != null)
                    {
                        // Do nothing if anime is already marked as completed.
                        if (entry.Info.StatusInfo?.Status == WatchStatus.Completed)
                        {
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

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
    }
}