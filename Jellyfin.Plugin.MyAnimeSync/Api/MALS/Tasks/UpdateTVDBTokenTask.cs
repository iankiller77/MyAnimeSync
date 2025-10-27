using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MyAnimeSync.Api.TVDB;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MyAnimeSync.Api.Mal.PluginTask
{
    /// <summary>
    /// Task that update the token used for TVDB.
    /// This ScheduledTask is needed to make sure that the token used is the same as the one provided on github.
    /// </summary>
    public class UpdateTVDBTokenTask : IScheduledTask
    {
        private readonly ILogger<UpdateTVDBTokenTask> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateTVDBTokenTask"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{UpdateTVDBTokenTask}"/> interface.</param>
        public UpdateTVDBTokenTask(ILogger<UpdateTVDBTokenTask> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public string Name => "MyAnimeSync update TVDB token.";

        /// <inheritdoc/>
        public string Key => "MyAnimeSyncMalTVDBTokenUpdate";

        /// <inheritdoc/>
        public string Description => "Update the global token used for TVDB requests.";

        /// <inheritdoc/>
        public string Category => "MyAnimeSync";

        /// <inheritdoc/>
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (Plugin.Instance == null)
            {
                throw new DataException("Plugin instance was null!");
            }

            if (!await TVDBApiHandler.UpdateTVDBToken().ConfigureAwait(true))
            {
                _logger.LogError("Could not update TVDB Token");
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