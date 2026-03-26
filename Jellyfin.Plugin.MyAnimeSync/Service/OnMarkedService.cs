using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.ProviderHandler;
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

                    await MALSProvider.UpdateAnimeList(serie, episodeNumber.Value, episode.AiredSeasonNumber, userConfig, _logger).ConfigureAwait(false);
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