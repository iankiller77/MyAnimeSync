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
        /// <param name="serie">The serie's name.<see cref="string"/>.</param>
        /// <param name="episodeNumber">The episode number.<see cref="int"/>.</param>
        /// <param name="seasonNumber">The season number.<see cref="int"/>.</param>
        /// <param name="retryCount">The amount of time we retried to update this entry.<see cref="int"/>.</param>
        /// <param name="success">The success status of the update.<see cref="bool"/>.</param>
        public void UpdateFailEntries(string serie, int episodeNumber, int seasonNumber, int retryCount = 0, bool success = false)
        {
            lock (_failedUpdateLock)
            {
                // Only retrieve result with same season number since different seasons are separate entries.
                UpdateEntry? entry = FailedUpdates.FirstOrDefault<UpdateEntry>(item => item.Serie == serie && item.SeasonNumber == seasonNumber);
                if (entry == null)
                {
                    if (success)
                    {
                        return;
                    }

                    entry = new UpdateEntry(serie, episodeNumber, seasonNumber, retryCount);

                    List<UpdateEntry> tempList = FailedUpdates.ToList();
                    tempList.Add(entry);
                    FailedUpdates = tempList.ToArray();
                }
                else
                {
                    if (success)
                    {
                        UpdateRetrySuccess(entry);
                        return;
                    }

                    if (entry.EpisodeNumber < episodeNumber)
                    {
                        entry.EpisodeNumber = episodeNumber;
                        retryCount = 0;
                    }
                    else if (retryCount != 0)
                    {
                        entry.RetryCount = retryCount;
                    }
                    else
                    {
                        UpdateRetryFailed(entry);
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
        /// Update status for failed entries, this will increment retry count.
        /// </summary>
        /// <param name="entry">The serie's name.<see cref="UpdateEntry"/>.</param>
        private void UpdateRetryFailed(UpdateEntry entry)
        {
            UpdateFailEntries(entry.Serie, entry.EpisodeNumber, entry.SeasonNumber, entry.RetryCount + 1);
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