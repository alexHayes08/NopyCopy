using Microsoft.VisualStudio.Shell;
using NopyCopyV2.Modals;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NopyCopyV2
{
    [Guid("9A3C3003-4671-4282-ADA7-D76B01704220")]
    public class OptionsPage : DialogPage, INopyCopyConfiguration
    {
        #region Fields

        public const string CATEGORY_NAME = "NopyCopy";

        // These values are used due to the way vs works and something about
        // auto-gen stuff.
        // https://github.com/Microsoft/VSSDK-Extensibility-Samples/tree/master/Options
        private bool isEnabled;
        private bool enableFileExtensions;
        private bool isWhiteList;
        private string watchedFileExtensions;

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
        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        [Category(CATEGORY_NAME)]
        [DisplayName("Enable file extensions")]
        [Description("Whether to use the 'Watched file extensions' to filter" +
            " which files are copied on save.")]
        public bool EnableFileExtensions
        {
            get => enableFileExtensions;
            set => enableFileExtensions = value;
        }

        [Category(CATEGORY_NAME)]
        [DisplayName("Is white list")]
        [Description("If true than the extensions listed in 'listed file " +
            "extensions' are whitelisted, if false than that list is a " +
            "black list.")]
        public bool IsWhiteList
        {
            get => isWhiteList;
            set => isWhiteList = value;
        }

        [Category(CATEGORY_NAME)]
        [DisplayName("Watched file extensions")]
        public string WatchedFileExtensions
        {
            get => watchedFileExtensions;
            set => watchedFileExtensions = value;
        }

        #endregion
    }
}
