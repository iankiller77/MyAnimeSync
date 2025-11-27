using System.Text.Json.Serialization;

#pragma warning disable SA1402, SA1649 // Desactivate class check for this file.
namespace Jellyfin.Plugin.MyAnimeSync.Api.TVDB
{
    /// <summary>
    /// Json object respresenting a login result.
    /// </summary>
    public class LoginNode
    {
        /// <summary>
        /// Gets or sets the querry status.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets data.
        /// </summary>
        [JsonPropertyName("data")]
        public LoginData? Data { get; set; }
    }

    /// <summary>
    /// Json object representing the data of a login quarry.
    /// </summary>
    public class LoginData
    {
        /// <summary>
        /// Gets or sets the token.
        /// </summary>
        public string? Token { get; set; }
    }

    /// <summary>
    /// Json object respresenting a search result.
    /// </summary>
    public class SeriesSearchNode
    {
        /// <summary>
        /// Gets or sets the querry status.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the querry data.
        /// </summary>
        [JsonPropertyName("data")]
        public SeriesSearchData[]? Data { get; set; }
    }

    /// <summary>
    /// Json object representing the data of a search quarry.
    /// </summary>
    public class SeriesSearchData
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [JsonPropertyName("id")]
        public string? ID { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Json object respresenting a search result.
    /// </summary>
    public class EpisodesSearchNode
    {
        /// <summary>
        /// Gets or sets the querry status.
        /// </summary>
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        /// <summary>
        /// Gets or sets the querry data.
        /// </summary>
        [JsonPropertyName("data")]
        public EpisodesSearchData? Data { get; set; }
    }

    /// <summary>
    /// Json object representing episodes datas.
    /// </summary>
    public class EpisodesSearchData
    {
        /// <summary>
        /// Gets or sets the list of episodes.
        /// </summary>
        [JsonPropertyName("episodes")]
        public EpisodeData[]? Episodes { get; set; }
    }

    /// <summary>
    /// Json object representing the data of one episode.
    /// </summary>
    public class EpisodeData
    {
        /// <summary>
        /// Gets or sets the name of the episode.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the episode.
        /// </summary>
        [JsonPropertyName("number")]
        public int? EpisodeNumber { get; set; }
    }
}
#pragma warning restore SA1402, SA1649