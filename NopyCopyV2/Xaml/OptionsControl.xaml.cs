using NopyCopyV2.Modals;
using System.Collections.Generic;
using System.Windows.Controls;

namespace NopyCopyV2.Xaml
{
    /// <summary>
    /// Interaction logic for OptionsControl.xaml
    /// </summary>
    public partial class OptionsControl : UserControl, INopyCopyConfiguration
    {
        #region Ctor

        public OptionsControl()
        {
            InitializeComponent();
        }

        #endregion

        #region Properties

        public bool IsWhiteList { get; set; }
        public bool EnableFileExtensions { get; set; }
        public IList<Override> Overrides { get; set; }
        public string WatchedFileExtensions { get; set; }

        #endregion
    }
}
