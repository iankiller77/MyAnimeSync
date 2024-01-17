using System;
using System.Linq;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MyAnimeSync.Configuration
{
    /// <summary>
    /// Plugin configuration.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            AuthenticatingUser = Guid.Empty;
            UserConfigs = Array.Empty<UserConfig>();
        }

        /// <summary>
        /// Gets or sets the user that is currently being authentificated.
        /// </summary>
        public Guid AuthenticatingUser { get; set; }

#pragma warning disable CA1819 // Properties should not return arrays
        /// <summary>
        /// Gets or sets the list of user configs.
        /// </summary>
        public UserConfig[] UserConfigs { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays

        /// <summary>
        /// Get config by id.
        /// </summary>
        /// <param name="id">The user id.</param>
        /// <returns>Stored user config.</returns>
        public UserConfig? GetByGuid(Guid id)
        {
            return UserConfigs.FirstOrDefault(c => c.Id == id);
        }

        /// <summary>
        /// Get config for the user that is currently getting authenticated.
        /// </summary>
        /// <returns>Stored user config.</returns>
        public UserConfig? GetAuthenticatingUserConfig()
        {
            if (AuthenticatingUser == Guid.Empty)
            {
                return null;
            }

            return GetByGuid(AuthenticatingUser);
        }
    }
}
