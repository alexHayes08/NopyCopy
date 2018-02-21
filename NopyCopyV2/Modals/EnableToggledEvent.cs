using System;
using System.Collections.Generic;

namespace NopyCopyV2.Modals
{
    public class ConfigUpdatedEvent : EventArgs, INopyCopyConfiguration
    {
        public ConfigUpdatedEvent(bool isEnabled, 
            bool isWhiteList, 
            IList<string> listedFileExtensions)
        {
            IsEnabled = isEnabled;
            IsWhiteList = isWhiteList;
            ListedFileExtensions = listedFileExtensions;
        }

        public ConfigUpdatedEvent(INopyCopyConfiguration configuration)
        {
            IsEnabled = configuration.IsEnabled;
            IsWhiteList = configuration.IsWhiteList;
            ListedFileExtensions = configuration.ListedFileExtensions;
        }

        public bool IsEnabled { get; private set; }
        public bool IsWhiteList { get; private set; }
        public IList<string> ListedFileExtensions { get; private set; }
    }
}
