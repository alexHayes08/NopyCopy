using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NopyCopyV2.Modals
{
    public class NopyCopyConfiguration : INopyCopyConfiguration, INotifyPropertyChanged
    {
        #region Fields

        private ObservableCollection<string> listedFileExtensions;
        private ObservableCollection<Override> overrides;

        // Reference to the options page
        private readonly OptionsPage optionsPage;

        #endregion

        #region Ctor(s)

        public NopyCopyConfiguration(OptionsPage options)
        {
            optionsPage = options;
            optionsPage.LoadSettingsFromStorage();
            optionsPage.Overrides = optionsPage.Overrides ?? new List<Override>();
            optionsPage.ListedFileExtensions = optionsPage.ListedFileExtensions ?? new List<string>();

            // Add common extensions
            if (optionsPage.ListedFileExtensions.Count == 0)
            {
                optionsPage.ListedFileExtensions = new List<string>()
                {
                    ".js",
                    ".json",
                    ".html",
                    ".cshtml",
                    ".css",
                    ".scss",
                    ".txt"
                };
            }

            optionsPage.SaveSettingsToStorage();

            listedFileExtensions = new ObservableCollection<string>(options.ListedFileExtensions);
            overrides = new ObservableCollection<Override>(options.Overrides);

            listedFileExtensions.CollectionChanged += ListedFileExtensions_CollectionChanged;
            overrides.CollectionChanged += Overrides_CollectionChanged;
        }

        #endregion

        #region Properties

        /// <summary>
        /// These file extensions (*.cshtml, *.js, etc...) will automatically 
        /// be copied when debugging.
        /// </summary>
        public IList<string> ListedFileExtensions
        {
            get
            {
                return listedFileExtensions;
            }
            set
            {
                if (value == null)
                    return;

                // Remove and add the event listener for when changes occur.
                listedFileExtensions.CollectionChanged -= ListedFileExtensions_CollectionChanged;
                listedFileExtensions = new ObservableCollection<string>(value);
                optionsPage.ListedFileExtensions = value;
                listedFileExtensions.CollectionChanged += ListedFileExtensions_CollectionChanged;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// A list of custom rules of where to copy files that override the
        /// default behavior.
        /// </summary>
        public IList<Override> Overrides
        {
            get => overrides;
            set
            {
                if (value == null)
                    return;

                overrides.CollectionChanged -= Overrides_CollectionChanged;
                overrides = new ObservableCollection<Override>(value);
                optionsPage.Overrides = value;
                overrides.CollectionChanged -= Overrides_CollectionChanged;
                OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
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
                    OnPropertyChanged();
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// This event is fired off whenever a property is changed. For lists
        /// this includes whenever an item is added/removed.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Event Handlers

        private void ListedFileExtensions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            optionsPage.ListedFileExtensions = ListedFileExtensions;
            PropertyChanged(this, new PropertyChangedEventArgs(nameof(ListedFileExtensions)));
        }

        private void Overrides_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            optionsPage.Overrides = Overrides;
            PropertyChanged(this, new PropertyChangedEventArgs(nameof(Overrides)));
        }

        private void OnPropertyChanged([CallerMemberName]string name = "")
        {
            optionsPage.SaveSettingsToStorage();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}