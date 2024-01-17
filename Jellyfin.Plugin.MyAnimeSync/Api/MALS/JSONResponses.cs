using System.Text.Json.Serialization;

#pragma warning disable SA1402, SA1649 // Desactivate class check for this file.
namespace Jellyfin.Plugin.MyAnimeSync.Api.Mal
{
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
    }

    /// <summary>
    /// Json object representing the information of an anime.
    /// </summary>
    public class AnimeInfo
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
}
#pragma warning restore SA1402, SA1649