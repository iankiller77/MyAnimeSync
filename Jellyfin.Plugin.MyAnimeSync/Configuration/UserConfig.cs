using System;
using Jellyfin.Data.Entities;

namespace Jellyfin.Plugin.MyAnimeSync.Configuration
{
    /// <summary>
    /// User config.
    /// </summary>
    public class UserConfig
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserConfig"/> class.
        /// </summary>
        public UserConfig()
        {
            UserToken = string.Empty;
            RefreshToken = string.Empty;
            ClientID = string.Empty;
            ClientSecret = string.Empty;
            CodeChallenge = string.Empty;
        }

        /// <summary>
        /// Gets or sets user token.
        /// </summary>
        public string UserToken { get; set; }

        /// <summary>
        /// Gets or sets refresh token.
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets client id.
        /// </summary>
        public string ClientID { get; set; }

        /// <summary>
        /// Gets or sets client secret.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets code challenge.
        /// </summary>
        public string CodeChallenge { get; set; }

        /// <summary>
        /// Gets or sets user id.
        /// </summary>
        public Guid Id { get; set; }
    }
}