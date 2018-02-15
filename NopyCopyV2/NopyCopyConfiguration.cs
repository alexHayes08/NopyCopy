using System.Collections.Generic;

namespace NopyCopyV2
{
    public class NopyCopyConfiguration
    {
        #region Properties

        /// <summary>
        /// These file extensions (*.cshtml, *.js, etc...) will automatically 
        /// be copied when debugging.
        /// </summary>
        public IList<string> ListedFileExtensions { get; set; }

        /// <summary>
        /// Determines whether the file extensions in 'ListedFileExtensions' 
        /// are whitelisted or blacklisted.
        /// </summary>
        public bool IsWhiteList { get; set; }

        /// <summary>
        /// Used to disable or enable the plugin.
        /// </summary>
        public bool IsEnabled { get; set; }

        #endregion
    }
}