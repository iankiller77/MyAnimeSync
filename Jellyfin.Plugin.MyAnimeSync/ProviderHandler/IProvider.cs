using System.Threading.Tasks;
using Jellyfin.Plugin.MyAnimeSync.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ProviderHandler
{
    /// <summary>
    ///  Base class for providers.
    /// </summary>
    public interface IProvider
    {
        /// <summary>
        /// Update anime watch list on MyAnimeList when an episode is marked as watched.
        /// </summary>
        /// <param name="serie">The serie's name.<see cref="string"/>.</param>
        /// <param name="episodeNumber">The episode number.<see cref="int"/>.</param>
        /// <param name="seasonNumber">The season number.<see cref="int"/>.</param>
        /// <param name="userConfig">The user config. <see cref="UserConfig"/>.</param>
        /// <param name="logger">The logger. <see cref="ILogger"/>.</param>
        /// <returns> The task. </returns>
        public static abstract Task<bool> UpdateAnimeList(string serie, int episodeNumber, int? seasonNumber, UserConfig userConfig, ILogger logger);
    }
}