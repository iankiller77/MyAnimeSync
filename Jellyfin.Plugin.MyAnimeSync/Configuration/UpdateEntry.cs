using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MyAnimeSync.Configuration
{
    /// <summary>
    /// Update entry for failed updates.
    /// </summary>
    public class UpdateEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateEntry"/> class.
        /// </summary>
        private UpdateEntry()
        {
            Serie = string.Empty;
            OriginalSerieTitle = string.Empty;
            EpisodeNumber = 0;
            SeasonNumber = 0;
            RetryCount = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateEntry"/> class.
        /// </summary>
        /// <param name="serie">The serie's name.<see cref="string"/>.</param>
        /// <param name="originalSerieTitle">The serie's original title name.<see cref="string"/>.</param>
        /// <param name="episodeNumber">The episode number.<see cref="int"/>.</param>
        /// <param name="seasonNumber">The season number.<see cref="int"/>.</param>
        /// <param name="startYear">The start year of the season.<see cref="int"/>.</param>
        /// <param name="retryCount">The amount of times we tried to update the user list.<see cref="int"/>.</param>
        public UpdateEntry(string serie, string originalSerieTitle, int episodeNumber, int seasonNumber, int? startYear, int retryCount = 0)
        {
            Serie = serie;
            OriginalSerieTitle = originalSerieTitle;
            EpisodeNumber = episodeNumber;
            SeasonNumber = seasonNumber;
            StartYear = startYear;
            RetryCount = retryCount;
        }

        /// <summary>
        /// Gets or sets the serie's name.
        /// </summary>
        public string Serie { get; set; }

        /// <summary>
        /// Gets or sets the serie's name.
        /// </summary>
        public string OriginalSerieTitle { get; set; }

        /// <summary>
        /// Gets or sets the episode number.
        /// </summary>
        public int EpisodeNumber { get; set; }

        /// <summary>
        /// Gets or sets the season number.
        /// </summary>
        public int SeasonNumber { get; set; }

        /// <summary>
        /// Gets or sets the start year.
        /// </summary>
        public int? StartYear { get; set; }

        /// <summary>
        /// Gets or sets the retry count of the entry update.
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// Generate an update entry from the episode info provided by jellyfin.
        /// </summary>
        /// <param name="episode">The episode info provided by jellyfin<see cref="Episode"/> .</param>
        /// <param name="logger">The logger. <see cref="ILogger"/>.</param>
        /// <returns>The update entry generated from the episode info<see cref="UpdateEntry"/>.</returns>
        public static UpdateEntry? GenerateEntryFromEpisode(Episode episode, ILogger logger)
        {
            if (episode.IndexNumber == null)
            {
                logger.LogError(
                            "Could not retrieve episode number for : {AnimeName}",
                            episode.SeriesName);
                return null;
            }

            string? originalTitle = episode.Series.OriginalTitle ?? string.Empty;
            int episodeNumber = episode.IndexNumber.Value;
            int season = episode.AiredSeasonNumber ?? 1;
            int? startYear = episode.Series.ProductionYear;

            return new UpdateEntry(episode.SeriesName, originalTitle, episodeNumber, season, startYear);
        }
    }
}