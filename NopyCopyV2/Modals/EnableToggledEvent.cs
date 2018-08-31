using System;
using System.Collections.Generic;

namespace NopyCopyV2.Modals
{
    public class ConfigUpdatedEvent : EventArgs, INopyCopyConfiguration
    {
        public ConfigUpdatedEvent(bool isEnabled, 
            bool isWhiteList, 
            string watchedFileExtensions
            /*IList<Override> overrides*/)
        {
            IsEnabled = isEnabled;
            IsWhiteList = isWhiteList;
            WatchedFileExtensions = watchedFileExtensions;
            //Overrides = overrides;
        }

        public ConfigUpdatedEvent(INopyCopyConfiguration configuration)
        {
            IsEnabled = configuration.IsEnabled;
            IsWhiteList = configuration.IsWhiteList;
            WatchedFileExtensions = configuration.WatchedFileExtensions;
        }

        public bool IsEnabled { get; private set; }
        public bool IsWhiteList { get; private set; }
        public string WatchedFileExtensions { get; private set; }
        //public IList<Override> Overrides { get; private set; }
    }
}
