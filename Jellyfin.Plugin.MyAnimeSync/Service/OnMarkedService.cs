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
        private void OnUserDataMarkedPlayed(object? sender, UserDataSaveEventArgs eventArgs)
        {
            // Check if the user has a config!
            Console.WriteLine("Episode was marked!");
            var userID = eventArgs.UserId;
            var userConfig = Plugin.Instance?.Configuration.GetByGuid(userID);
            if (userConfig == null || string.IsNullOrEmpty(userConfig.UserToken))
            {
                _logger.LogError(
                    "User {UserName} does not have anime backup setup.",
                    eventArgs.UserId);
                return;
            }

            // If we have a new video marked as played.
            if (eventArgs.SaveReason == UserDataSaveReason.TogglePlayed && eventArgs.UserData.Played)
            {
                if (eventArgs.Item is Episode episode)
                {
                    string serie = episode.SeriesName;
                    MalApiHandler.GetAnimeInfo(serie, userConfig);
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