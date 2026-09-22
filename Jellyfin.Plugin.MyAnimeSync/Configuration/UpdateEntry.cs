using System;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
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
            TryCount = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateEntry"/> class.
        /// </summary>
        /// <param name="serieID">The jellyfin's serie guid.<see cref="string"/>.</param>
        /// <param name="serie">The serie's name.<see cref="string"/>.</param>
        /// <param name="originalSerieTitle">The serie's original title name.<see cref="string"/>.</param>
        /// <param name="episodeNumber">The episode number.<see cref="int"/>.</param>
        /// <param name="seasonNumber">The season number.<see cref="int"/>.</param>
        /// <param name="startYear">The start year of the season.<see cref="int"/>.</param>
        /// <param name="tvdbEpisodeID">The episode ID on TVDB.<see cref="string"/>.</param>
        /// <param name="tryCount">The amount of times we tried to update the user list.<see cref="int"/>.</param>
        /// <param name="userEdited">Flag specifying if the user manually updated the entry.<see cref="bool"/>.</param>
        public UpdateEntry(Guid serieID, string serie, string originalSerieTitle, int episodeNumber, int seasonNumber, int? startYear, string? tvdbEpisodeID, int tryCount = 0, bool userEdited = false)
        {
            SerieID = serieID;
            Serie = serie;
            OriginalSerieTitle = originalSerieTitle;
            EpisodeNumber = episodeNumber;
            SeasonNumber = seasonNumber;
            StartYear = startYear;
            TVDBEpisodeID = tvdbEpisodeID;
            TryCount = tryCount;
            UserEdited = userEdited;
        }

        /// <summary>
        /// Gets or sets the jellyfin serie id.
        /// </summary>
        public Guid SerieID { get; set; }

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
        /// Gets or sets the TVDB episode id.
        /// </summary>
        public string? TVDBEpisodeID { get; set; }

        /// <summary>
        /// Gets or sets the try count of the entry update.
        /// </summary>
        public int TryCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user manually updated the entry.
        /// </summary>
        public bool UserEdited { get; set; }

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
            string? tvdbEpisodeID = episode.GetProviderId("Tvdb");

            return new UpdateEntry(episode.Series.Id, episode.SeriesName, originalTitle, episodeNumber, season, startYear, tvdbEpisodeID);
        }
    }
}