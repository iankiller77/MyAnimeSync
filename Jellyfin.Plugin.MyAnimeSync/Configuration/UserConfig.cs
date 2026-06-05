using System;
using System.Collections.Generic;
using System.Linq;
using J2N.Collections.Generic.Extensions;

namespace Jellyfin.Plugin.MyAnimeSync.Configuration
{
    /// <summary>
    /// User config.
    /// </summary>
    public class UserConfig
    {
        private static readonly object _failedUpdateLock = new object();

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
            OriginalTitleSearch = false;
            OriginalTitleSearchFallback = false;
            AllowSpecials = false;
            Throttle = true;
            ListMonitoredLibraryGuid = Array.Empty<Guid>();
            FailedUpdates = Array.Empty<UpdateEntry>();
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
        /// Gets or sets a value indicating whether we should allow updating special episodes.
        /// </summary>
        public bool AllowSpecials { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we want to force using original titles for serie search.
        /// </summary>
        public bool OriginalTitleSearch { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we want to force using original titles for serie search.
        /// </summary>
        public bool OriginalTitleSearchFallback { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether we want to throttle myanimelist.net api requests.
        /// </summary>
        public bool Throttle { get; set; }

        /// <summary>
        /// Gets or sets the list of libraries monitored for the user.
        /// </summary>
        public Guid[] ListMonitoredLibraryGuid { get; set; }

        /// <summary>
        /// Gets or sets the list of failed anime updates.
        /// </summary>
        public UpdateEntry[] FailedUpdates { get; set; }

        /// <summary>
        /// Gets or sets user id.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Update status for failed entries.
        /// </summary>
        /// <param name="episodeInfo">The episode's information.<see cref="UpdateEntry"/>.</param>
        /// <param name="success">The success status of the update.<see cref="bool"/>.</param>
        public void UpdateFailEntries(UpdateEntry episodeInfo, bool success = false)
        {
            lock (_failedUpdateLock)
            {
                // Only retrieve result with same season number since different seasons are separate entries.
                UpdateEntry? existingFailedEntry = FailedUpdates.FirstOrDefault<UpdateEntry>(item => item.Serie == episodeInfo.Serie && item.SeasonNumber == episodeInfo.SeasonNumber);
                if (existingFailedEntry == null)
                {
                    if (success)
                    {
                        return;
                    }

                    List<UpdateEntry> tempList = FailedUpdates.ToList();
                    tempList.Add(episodeInfo);
                    FailedUpdates = tempList.ToArray();
                }
                else
                {
                    int episodeNumber = episodeInfo.EpisodeNumber;

                    if (success)
                    {
                        UpdateRetrySuccess(existingFailedEntry);
                        return;
                    }

                    if (existingFailedEntry.EpisodeNumber < episodeNumber)
                    {
                        existingFailedEntry.EpisodeNumber = episodeNumber;
                        existingFailedEntry.RetryCount = 0;
                    }
                    else
                    {
                        existingFailedEntry.RetryCount += 1;
                    }
                }

                Plugin.Instance?.SaveConfiguration();
            }
        }

        /// <summary>
        /// Remove the entry from failed update list.
        /// </summary>
        /// <param name="entry">The season number.<see cref="UpdateEntry"/>.</param>
        private void UpdateRetrySuccess(UpdateEntry entry)
        {
            List<UpdateEntry> tempList = FailedUpdates.ToList();
            tempList.RemoveAll<UpdateEntry>(item => item.Serie == entry.Serie && item.SeasonNumber == entry.SeasonNumber);
            FailedUpdates = tempList.ToArray();

            Plugin.Instance?.SaveConfiguration();
        }

        /// <summary>
        /// Retrieve the update entry.
        /// </summary>
        /// <param name="serie">The name of the serie.</param>
        /// <param name="season">The season of the entrie.</param>
        /// <returns>The update entry.</returns>
        public UpdateEntry? GetUpdateEntry(string serie, int season)
        {
            return FailedUpdates.FirstOrDefault<UpdateEntry>(item => item.Serie == serie && item.SeasonNumber == season);
        }
    }
}