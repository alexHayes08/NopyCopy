using NopyCopyV2.Modals.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NopyCopyV2.Modals
{
    public class NopyCopyConfiguration : INopyCopyConfiguration,
        IObservable<INopyCopyConfiguration>
    {
        #region Fields

        private IList<IObserver<INopyCopyConfiguration>> observers;

        private ObservableCollection<string> watchedFileExtensions;

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

            watchedFileExtensions = new ObservableCollection<string>(options
                .GetWatchedFileExensions());
        }

        #endregion

        #region Properties

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