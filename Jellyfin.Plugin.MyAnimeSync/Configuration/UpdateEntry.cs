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
            EpisodeNumber = 0;
            SeasonNumber = 0;
            RetryCount = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateEntry"/> class.
        /// </summary>
        /// <param name="serie">The serie's name.<see cref="string"/>.</param>
        /// <param name="episodeNumber">The episode number.<see cref="int"/>.</param>
        /// <param name="seasonNumber">The season number.<see cref="int"/>.</param>
        /// <param name="retryCount">The amount of times we tried to update the user list.<see cref="int"/>.</param>
        public UpdateEntry(string serie, int episodeNumber, int seasonNumber, int retryCount = 0)
        {
            Serie = serie;
            EpisodeNumber = episodeNumber;
            SeasonNumber = seasonNumber;
            RetryCount = retryCount;
        }

        /// <summary>
        /// Gets or sets the serie's name.
        /// </summary>
        public string Serie { get; set; }

        /// <summary>
        /// Gets or sets the episode number.
        /// </summary>
        public int EpisodeNumber { get; set; }

        /// <summary>
        /// Gets or sets the season number.
        /// </summary>
        public int SeasonNumber { get; set; }

        /// <summary>
        /// Gets or sets the retry count of the entry update.
        /// </summary>
        public int RetryCount { get; set; }
    }
}