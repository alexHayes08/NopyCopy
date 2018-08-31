using System;

namespace NopyCopyV2.Modals
{
    public class ConfigUpdatedEvent : EventArgs, INopyCopyConfiguration
    {
        public ConfigUpdatedEvent(bool isEnabled,
            bool enableFileExtensions,
            bool isWhiteList, 
            string watchedFileExtensions)
        {
            IsEnabled = isEnabled;
            IsWhiteList = isWhiteList;
            WatchedFileExtensions = watchedFileExtensions;
        }

        public ConfigUpdatedEvent(INopyCopyConfiguration configuration)
        {
            IsEnabled = configuration.IsEnabled;
            IsWhiteList = configuration.IsWhiteList;
            WatchedFileExtensions = configuration.WatchedFileExtensions;
        }

        public bool IsEnabled { get; private set; }
        public bool EnableFileExtensions { get; private set; }
        public bool IsWhiteList { get; private set; }
        public string WatchedFileExtensions { get; private set; }
    }
}
