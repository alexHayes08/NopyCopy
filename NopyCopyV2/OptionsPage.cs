using Microsoft.VisualStudio.Shell;
using NopyCopyV2.Modals;
using NopyCopyV2.Xaml;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NopyCopyV2
{
    [Guid("9A3C3003-4671-4282-ADA7-D76B01704220")]
    public class OptionsPage : DialogPage, INopyCopyConfiguration
    {
        #region Fields

        public const string CATEGORY_NAME = "NopyCopy";
        //private System.Windows.Controls.UserControl window;

        #endregion

        #region Ctor(s)

        public OptionsPage()
        {
            //ListedFileExtensions = new List<string>();
        }

        #endregion

        #region Properties

        [Category(CATEGORY_NAME)]
        [DisplayName("Enabled")]
        [Description("Toggles whether this extension on/off.")]
        public bool IsEnabled { get; set; }

        [Category(CATEGORY_NAME)]
        [DisplayName("Is white list")]
        [Description("If true than the extensions listed in 'listed file " +
            "extensions' are whitelisted, if false than that list is a " +
            "black list.")]
        public bool IsWhiteList { get; set; }

        [Category(CATEGORY_NAME)]
        [DisplayName("Watched file extensions")]
        public string WatchedFileExtensions { get; set; }

        //[Category(CATEGORY_NAME)]
        //public IList<string> ListedFileExtensions { get; set; }

        //[Category(CATEGORY_NAME)]
        //public IList<Override> Overrides { get; set; }

        //protected override IWin32Window Window
        //{
        //    get
        //    {
        //        if (window == null)
        //        {
        //            window = new OptionsControl();
        //        }

        //        return window.
        //    }
        //}

        #endregion
    }
}
