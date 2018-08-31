using System;
using System.Collections.Generic;

namespace NopyCopyV2.Modals
{
    public class NopyCopyConfiguration : INopyCopyConfiguration,
        IObservable<INopyCopyConfiguration>
    {
        #region Fields

        private IList<IObserver<INopyCopyConfiguration>> observers;

        // Reference to the options page
        private readonly OptionsPage optionsPage;

        #endregion

        #region Ctor(s)

        public NopyCopyConfiguration(OptionsPage options)
        {
            observers = new List<IObserver<INopyCopyConfiguration>>();
            optionsPage = options;
            optionsPage.LoadSettingsFromStorage();

            // Add common extensions
            if (optionsPage.WatchedFileExtensions == null
                || optionsPage.WatchedFileExtensions.Length == 0)
            {
                optionsPage.WatchedFileExtensions = string.Join(", ",
                    new List<string>()
                    {
                        ".js",
                        ".json",
                        ".html",
                        ".cshtml",
                        ".css",
                        ".scss",
                        ".txt"
                    });
            }

            optionsPage.SaveSettingsToStorage();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Whether to use the 'WatchedFileExtensions' property when deciding
        /// whether or not to copy a file.
        /// </summary>
        public bool EnableFileExtensions
        {
            get => optionsPage.EnableFileExtensions;
            set
            {
                if (value != optionsPage.EnableFileExtensions)
                {
                    optionsPage.EnableFileExtensions = value;
                    OnChange();
                }
            }
        }

        /// <summary>
        /// Retrieves a semi-colon(;) seperated list of file extensions
        /// (example: *.js;*.cs).
        /// </summary>
        public string WatchedFileExtensions
        {
            get => optionsPage.WatchedFileExtensions;
            set
            {
                if (value != optionsPage.WatchedFileExtensions)
                {
                    optionsPage.WatchedFileExtensions = value;
                    OnChange();
                }
            }
        }

        /// <summary>
        /// Determines whether the file extensions in 'ListedFileExtensions' 
        /// are whitelisted or blacklisted.
        /// </summary>
        public bool IsWhiteList
        {
            get => optionsPage.IsWhiteList;
            set
            {
                if (value != optionsPage.IsWhiteList)
                {
                    optionsPage.IsWhiteList = value;
                    OnChange();
                }
            }
        }

        /// <summary>
        /// Determines whether the file extensions in 'ListedFileExtensions' 
        /// are whitelisted or blacklisted.
        /// </summary>
        public bool IsBlackList
        {
            get => !optionsPage.IsWhiteList;
            set
            {
                if (value == optionsPage.IsWhiteList)
                {
                    optionsPage.IsWhiteList = !value;
                    OnChange();
                }
            }
        }

        /// <summary>
        /// Used to disable or enable the plugin.
        /// </summary>
        public bool IsEnabled
        {
            get => optionsPage.IsEnabled;
            set
            {
                if (optionsPage.IsEnabled != value)
                {
                    optionsPage.IsEnabled = value;
                    OnChange();
                }
            }
        }

        #endregion

        #region Methods

        private void OnChange()
        {
            foreach (var observer in observers)
            {
                observer.OnNext(this);
            }
        }

        public IDisposable Subscribe(IObserver<INopyCopyConfiguration> observer)
        {
            if (!observers.Contains(observer))
            {
                observers.Add(observer);
            }

            return new Unsubscriber<INopyCopyConfiguration>(observers, observer);
        }

        #endregion
    }
}