﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NopyCopyV2.Modals
{
    public class NopyCopyConfiguration : INopyCopyConfiguration, INotifyPropertyChanged
    {
        #region Fields

        private ObservableCollection<string> listedFileExtensions;
        private bool isWhiteList;
        private bool isEnabled;

        #endregion

        #region Ctor(s)

        public NopyCopyConfiguration()
        {
            isWhiteList = false;
            isEnabled = false;
            listedFileExtensions = new ObservableCollection<string>();
            listedFileExtensions.CollectionChanged += ListedFileExtensions_CollectionChanged;
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
                // Remove and add the event listener for when changes occur.
                listedFileExtensions.CollectionChanged -= ListedFileExtensions_CollectionChanged;
                listedFileExtensions = new ObservableCollection<string>(value);
                listedFileExtensions.CollectionChanged += ListedFileExtensions_CollectionChanged;
            }
        }

        /// <summary>
        /// Determines whether the file extensions in 'ListedFileExtensions' 
        /// are whitelisted or blacklisted.
        /// </summary>
        public bool IsWhiteList { get; set; }

        /// <summary>
        /// Used to disable or enable the plugin.
        /// </summary>
        public bool IsEnabled { get; set; }

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
            PropertyChanged(this, new PropertyChangedEventArgs("ListedFileCollection"));
        }

        #endregion
    }
}