using System;

namespace Jellyfin.Plugin.MyAnimeSync.Configuration
{
    /// <summary>
    /// Update entry for failed updates.
    /// </summary>
    public class UserMapping
    {
        private UserMapping()
        {
            UserProvidedName = string.Empty;
            UserProvidedOriginalName = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMapping"/> class.
        /// </summary>
        /// <param name="serieID">The jellyfin's serie guid.<see cref="string"/>.</param>
        /// <param name="userProvidedName">The serie's name provided by the user.<see cref="string"/>.</param>
        /// <param name="userProvidedOriginalName">The serie's original title provided by the user.<see cref="string"/>.</param>
        public UserMapping(Guid serieID, string userProvidedName, string userProvidedOriginalName)
        {
            SerieID = serieID;
            UserProvidedName = userProvidedName;
            UserProvidedOriginalName = userProvidedOriginalName;
        }

        /// <summary>
        /// Gets or sets the jellyfin serie id.
        /// </summary>
        public Guid SerieID { get; set; }

        /// <summary>
        /// Gets or sets the user provided series name.
        /// </summary>
        public string UserProvidedName { get; set; }

        /// <summary>
        /// Gets or sets the user provided original series name.
        /// </summary>
        public string UserProvidedOriginalName { get; set; }
    }
}