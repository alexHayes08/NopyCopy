using NopyCopyV2.Modals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        //public IList<string> ListedFileExtensions { get; set; }
        public IList<Override> Overrides { get; set; }
        public string WatchedFileExtensions { get; set; }

        #endregion
    }
}
