using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MyAnimeSync.Api.Mal.PluginTask
{
    /// <summary>
    /// Task that update the token of all users.
    /// This ScheduledTask prevent user token expiring before refreshing them in cases where the user does not watch animes for an extended period of time.
    /// </summary>
    public class MalTokenUpdateTask : IScheduledTask
    {
        private readonly ILogger<MalTokenUpdateTask> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MalTokenUpdateTask"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{MalTokenUpdateTask}"/> interface.</param>
        public MalTokenUpdateTask(ILogger<MalTokenUpdateTask> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public string Name => "MyAnimeSync update all user tokens.";

        /// <inheritdoc/>
        public string Key => "MyAnimeSyncMalTokenUpdate";

        /// <inheritdoc/>
        public string Description => "Update the token of all users with valid refresh tokens.";

        /// <inheritdoc/>
        public string Category => "MyAnimeSync";

        /// <inheritdoc/>
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (Plugin.Instance == null)
            {
                throw new DataException("Plugin instance was null!");
            }

            UserConfig[] configs = Plugin.Instance.Configuration.UserConfigs;

            foreach (UserConfig uConfig in configs)
            {
                if (!(string.IsNullOrEmpty(uConfig.ClientID) || string.IsNullOrEmpty(uConfig.ClientSecret) || string.IsNullOrEmpty(uConfig.RefreshToken)))
                {
                    await MalApiHandler.RefreshTokens(uConfig).ConfigureAwait(false);
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var trigger = new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromDays(1).Ticks
            };

            return new[] { trigger };
        }
    }
}