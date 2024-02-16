using System;
using System.Collections.Generic;
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
            TokenExpirationDateTime = DateTime.MinValue;
            TokenAcquireDateTime = DateTime.MinValue;
            ClientID = string.Empty;
            ClientSecret = string.Empty;
            CodeChallenge = string.Empty;
            AllowNSFW = false;
            ListMonitoredLibraryGuid = Array.Empty<Guid>();
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
        /// Gets or sets the expiration date of the user token.
        /// </summary>
        public DateTime TokenExpirationDateTime { get; set; }

        /// <summary>
        /// Gets or sets the date the tokens were either refreshed or created.
        /// </summary>
        public DateTime TokenAcquireDateTime { get; set; }

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
        /// Gets or sets a value indicating whether we should allow nsfw entry in anime search.
        /// </summary>
        public bool AllowNSFW { get; set; }

        /// <summary>
        /// Gets or sets the list of libraries monitored for the user.
        /// </summary>
        public Guid[] ListMonitoredLibraryGuid { get; set; }

        /// <summary>
        /// Gets or sets user id.
        /// </summary>
        public Guid Id { get; set; }
    }
}