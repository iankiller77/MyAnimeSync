using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using Jellyfin.Plugin.MyAnimeSync.Service;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MyAnimeSync.Api.Mal.PluginTask
{
    /// <summary>
    /// Task that update the token of all users.
    /// This ScheduledTask prevent user token expiring before refreshing them in cases where the user does not watch animes for an extended period of time.
    /// </summary>
    public class RetryFailedMalUpdatesTask : IScheduledTask
    {
        private readonly ILogger<RetryFailedMalUpdatesTask> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryFailedMalUpdatesTask"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{RetryFailedMalUpdatesTask}"/> interface.</param>
        public RetryFailedMalUpdatesTask(ILogger<RetryFailedMalUpdatesTask> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public string Name => "MyAnimeSync retry to update all failed entries.";

        /// <inheritdoc/>
        public string Key => "MyAnimeSyncMalRetryUpdates";

        /// <inheritdoc/>
        public string Description => "Retry to update user watched library for all failed entries";

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
                foreach (UpdateEntry entry in uConfig.FailedUpdates)
                {
                    if (entry.RetryCount < 5)
                    {
                        await OnMarkedService.UpdateAnimeList(entry.Serie, entry.EpisodeNumber, entry.SeasonNumber, uConfig, _logger).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            var trigger = new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromDays(1).Ticks
            };

            return new[] { trigger };
        }
    }
}