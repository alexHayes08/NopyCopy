using System;

namespace NopyCopyV2.Modals
{
    public class EnableToggledEvent : EventArgs
    {
        public bool IsEnabled { get; set; }
    }
}
