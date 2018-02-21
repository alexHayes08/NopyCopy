namespace NopyCopyV2.Xaml
{
    using Microsoft.VisualStudio.Shell.Interop;
    using NopyCopyV2.Modals;
    using System;
    using System.Collections.Generic;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for MainWindowControl.
    /// </summary>
    public partial class MainWindowControl : UserControl, IObserver<NopyCopyConfiguration>
    {
        #region Fields

        private const string DEFAULT_SOLUTION_NAME_PLACEHOLDER = "No solution loaded";
        private NopyCopyService nopyCopyService;
        private bool attachedHandlers = false;

        #endregion

        #region Ctor(s)

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowControl"/> class.
        /// </summary>
        public MainWindowControl()
        {
            InitializeComponent();
            Logs = new List<string>();
            ListView_Log.ItemsSource = Logs;
        }

        #endregion

        #region Finalizer

        ~MainWindowControl()
        {
            DetachEventHanlders();
        }

        #endregion

        #region Properties

        public bool LoadedService { get; set; }
        public IList<string> Logs { get; private set; }
        public NopyCopyService NopyCopyService
        {
            get
            {
                return nopyCopyService;
            }
            set
            {
                DetachEventHanlders();
                nopyCopyService = value;
                AttachEventHandlers();
            }
        }
        public IVsUIShell5 ColorService { get; set; }
        public bool IsWhiteList
        {
            get
            {
                if (NopyCopyService == null)
                    return false;
                else
                    return NopyCopyService.Configuration.IsWhiteList;
            }
        }
        public string SolutionName
        {
            get
            {
                if (NopyCopyService != null && !string.IsNullOrEmpty(NopyCopyService.SolutionName))
                    return NopyCopyService.SolutionName;
                else
                    return DEFAULT_SOLUTION_NAME_PLACEHOLDER;
            }
        }

        #endregion

        #region Methods

        #region EventHandlers

        private void Button_AddItem_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (nopyCopyService == null)
                return;

            nopyCopyService.Configuration.ListedFileExtensions.Add("Enter file extension (Ex: *.txt");
        }

        private void Button_DeleteItems_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (nopyCopyService == null)
                return;

            var selectedItems = ListView_ListedFileExtensions.SelectedItems;
            foreach (var item in selectedItems)
            {
                if (item is string)
                    nopyCopyService.Configuration.ListedFileExtensions.Remove(item as string);
            }
        }

        private void Checkbox_Enable_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            nopyCopyService.Configuration.IsEnabled = true;
        }

        private void Checkbox_Enable_Unchecked(object sender, System.Windows.RoutedEventArgs e)
        {
            nopyCopyService.Configuration.IsEnabled = false;
        }

        private void DebugEventHandler(object sender, DebugEvent e)
        {
            if (e.IsDebugging)
            {
                Logs.Add("Started debugging");
            }
            else
            {
                Logs.Add("Stopped debugging");
            }
        }

        private void EnableToggleEventHandler(object sender, ConfigUpdatedEvent e)
        {
            Checkbox_Enable.IsChecked = e.IsEnabled;

            if (e.IsEnabled)
            {
                Checkbox_Enable.IsEnabled = true;
                Logs.Add("Enabled plugin");
            }
            else
            {
                Checkbox_Enable.IsEnabled = false;
                Logs.Add("Disabled plugin");
            }
        }

        private void NopCommerceSolutionEventHandler(object sender, NopCommerceSolutionEvent e)
        {
            Checkbox_IsNopCommerceProject.IsChecked = e.SolutionLoaded;

            if (e.SolutionLoaded)
            {
                Logs.Add("NopCommerce solution loaded");
            }
            else
            {
                Logs.Add("The solution was unloaded");
            }
        }

        private void FileSavedEventHandler(object sender, FileSavedEvent e)
        {
            Logs.Add("Saved and copied " + e.SavedFile.Name + " to " + e.CopiedTo.FullName);
        }

        #endregion

        public void UpdateColors()
        {
            // TODO
            Logs.Add("UpdateColors is still a WIP");
        }

        private void AttachEventHandlers()
        {
            if (nopyCopyService == null || attachedHandlers)
                return;

            nopyCopyService.OnDebugEvent += DebugEventHandler;
            nopyCopyService.OnConfigUpdatedEvent += EnableToggleEventHandler;
            nopyCopyService.OnNopCommerceSolutionEvent += NopCommerceSolutionEventHandler;
            nopyCopyService.OnFileSavedEvent += FileSavedEventHandler;

            attachedHandlers = true;
        }

        private void DetachEventHanlders()
        {
            if (nopyCopyService == null || !attachedHandlers)
                return;

            nopyCopyService.OnDebugEvent -= DebugEventHandler;
            nopyCopyService.OnConfigUpdatedEvent -= EnableToggleEventHandler;
            nopyCopyService.OnNopCommerceSolutionEvent -= NopCommerceSolutionEventHandler;
            nopyCopyService.OnFileSavedEvent -= FileSavedEventHandler;

            attachedHandlers = false;
        }

        public void OnNext(NopyCopyConfiguration value)
        {
            this.
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}