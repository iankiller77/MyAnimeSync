using System.Text.Json.Serialization;

#pragma warning disable SA1402, SA1649 // Desactivate class check for this file.
namespace Jellyfin.Plugin.MyAnimeSync.Api.Mal
{
    /// <summary>
    /// Structure for watch status value on myanimelist api.
    /// </summary>
    public static class WatchStatus
    {
        /// <summary>Currently watching.</summary>
        public const string Watching = "watching";

        /// <summary>Completed.</summary>
        public const string Completed = "completed";

        /// <summary>On Hold.</summary>
        public const string OnHold = "on_hold";

        /// <summary>Dropped.</summary>
        public const string Dropped = "dropped";

        /// <summary>Planning to watch.</summary>
        public const string PlanToWatch = "plan_to_watch";
    }

    /// <summary>
    /// Structure for type of anime media.
    /// </summary>
    public static class MediaType
    {
        /// <summary>Seasonal Anime.</summary>
        public const string SeasonalAnime = "tv";

        /// <summary>Anime movie.</summary>
        public const string Movie = "movie";
    }

    /// <summary>
    /// Json object respresenting a node.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Gets or sets search entry.
        /// </summary>
        [JsonPropertyName("node")]
        public AnimeSearchEntry? SearchEntry { get; set; }
    }

    /// <summary>
    /// Json object representing an entry from an anime search result.
    /// </summary>
    public class AnimeSearchEntry
    {
        /// <summary>
        /// Gets or sets anime id.
        /// </summary>
        [JsonPropertyName("id")]
        public int? ID { get; set; }

        /// <summary>
        /// Gets or sets the anime title.
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the alternative anime titles.
        /// </summary>
        [JsonPropertyName("alternative_titles")]
        public AlternativeTitles? AlternativeTitles { get; set; }
    }

    /// <summary>
    /// Json object representing the information of an anime.
    /// </summary>
    public class AnimeData
    {
        /// <summary>
        /// Gets or sets anime id.
        /// </summary>
        [JsonPropertyName("id")]
        public int? ID { get; set; }

        /// <summary>
        /// Gets or sets the anime title.
        /// </summary>
        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// Gets or sets the media type.
        /// </summary>
        [JsonPropertyName("media_type")]
        public string? MediaType { get; set; }

        /// <summary>
        /// Gets or sets the amount of episode for the anime season.
        /// </summary>
        [JsonPropertyName("num_episodes")]
        public int? EpisodeCount { get; set; }

        /// <summary>
        /// Gets or sets the list of related animes.
        /// </summary>
        [JsonPropertyName("related_anime")]
        public RelatedAnime[]? RelatedNodes { get; set; }
    }

    /// <summary>
    /// Json object representing the alternative titles of an anime.
    /// </summary>
    public class AlternativeTitles
    {
        /// <summary>
        /// Gets or sets the english title.
        /// </summary>
        [JsonPropertyName("en")]
        public string? EnglishTitle { get; set; }
    }

    /// <summary>
    /// Json object representing the information of a related anime.
    /// </summary>
    public class RelatedAnime
    {
        /// <summary>
        /// Gets or sets search entry.
        /// </summary>>
        [JsonPropertyName("node")]
        public AnimeSearchEntry? SearchEntry { get; set; }

        /// <summary>
        /// Gets or sets the anime relation type.
        /// </summary>
        [JsonPropertyName("relation_type")]
        public string? RelationType { get; set; }
    }

    /// <summary>
    /// Json object representing an entry for user anime list.
    /// </summary>
    public class AnimeListEntry
    {
        /// <summary>
        /// Gets or sets search entry.
        /// </summary>>
        [JsonPropertyName("node")]
        public AnimeSearchEntry? AnimeInfo { get; set; }

        /// <summary>
        /// Gets or sets the anime status.
        /// </summary>
        [JsonPropertyName("list_status")]
        public AnimeStatus? StatusInfo { get; set; }
    }

    /// <summary>
    /// The status of the anime in the user list.
    /// </summary>
    public class AnimeStatus
    {
        /// <summary>
        /// Gets or sets the status of the anime.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the number of episode watched for the anime.
        /// </summary>
        [JsonPropertyName("num_episodes_watched")]
        public int? EpisodeWatched { get; set; }
    }

    /// <summary>
    /// Data stracture used to retrieve AnimeInfo.
    /// </summary>
    public class UserAnimeInfo
    {
        private UserAnimeInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserAnimeInfo"/> class.
        /// </summary>
        /// <param name="entry">The information on the anime entry. <see cref="AnimeListEntry"/>.</param>
        /// <param name="success">If the request was a success. <see cref="bool"/>.</param>
        public UserAnimeInfo(AnimeListEntry? entry, bool success)
        {
            Info = entry;
            SuccessStatus = success;
        }

        /// <summary>
        /// Gets or sets the entry info.
        /// </summary>
        public AnimeListEntry? Info { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the query was a success.
        /// </summary>
        public bool SuccessStatus { get; set; }
    }
}
#pragma warning restore SA1402, SA1649