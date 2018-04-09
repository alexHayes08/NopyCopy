using System.Collections.Generic;

namespace NopyCopyV2.Modals
{
    public interface INopyCopyConfiguration
    {
        bool IsEnabled { get; }
        bool IsWhiteList { get; }
        IList<string> ListedFileExtensions { get; }
        IList<Override> Overrides { get; }
    }
}
