namespace NopyCopyV2.Xaml
{
    using Microsoft.VisualStudio.Shell.Interop;
    using NopyCopyV2.Modals;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for MainWindowControl.
    /// </summary>
    public partial class MainWindowControl : UserControl
    {
        #region Fields

        private const string DEFAULT_SOLUTION_NAME_PLACEHOLDER = 
            "No solution loaded";
        private const string CHECKBOX_ENABLE_TOOLTIP_DISABLED_MESSAGE = 
            "The current solution does not appear to be a NopCommerce " +
            "project. Cannot enable the plugin.";
        private const string CHECKBOX_ENABLE_TOOLTIP_ENABLED_MESSAGE = 
            "If checked then when debugging, modifying and saving files " +
            "(such as views) will be copied to their corresponding ouput " +
            "plugin directory.";
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

            Checkbox_Enable.ToolTip = CHECKBOX_ENABLE_TOOLTIP_DISABLED_MESSAGE;
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

                // Have the message box display debug message
                Label_DebugMessageBox.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                Logs.Add("Stopped debugging");

                // Have the message box hide debug message
                Label_DebugMessageBox.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void EnableToggleEventHandler(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(nopyCopyService.Configuration.IsEnabled):
                    Checkbox_Enable.IsChecked = nopyCopyService.Configuration.IsEnabled;

                    if (Checkbox_Enable.IsChecked ?? false)
                    {
                        Checkbox_Enable.ToolTip = CHECKBOX_ENABLE_TOOLTIP_ENABLED_MESSAGE;
                        Logs.Add("Enabled plugin");
                    }
                    else
                    {
                        Checkbox_Enable.ToolTip = CHECKBOX_ENABLE_TOOLTIP_DISABLED_MESSAGE;
                        Logs.Add("Disabled plugin");
                    }

                    break;
                case nameof(nopyCopyService.Configuration.IsWhiteList):
                    // TODO
                    break;
                case nameof(nopyCopyService.Configuration.ListedFileExtensions):
                    // TODO
                    break;
            }
        }

        private void NopCommerceSolutionEventHandler(object sender, NopCommerceSolutionEvent e)
        {
            Checkbox_IsNopCommerceProject.IsChecked = e.SolutionLoaded;

            if (e.SolutionLoaded)
            {
                Checkbox_Enable.IsEnabled = true;
                Logs.Add("NopCommerce solution loaded");
            }
            else
            {
                Checkbox_Enable.IsEnabled = false;
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
            nopyCopyService.Configuration.PropertyChanged += EnableToggleEventHandler;
            nopyCopyService.OnNopCommerceSolutionEvent += NopCommerceSolutionEventHandler;
            nopyCopyService.OnFileSavedEvent += FileSavedEventHandler;

            attachedHandlers = true;
        }

        private void DetachEventHanlders()
        {
            if (nopyCopyService == null || !attachedHandlers)
                return;

            nopyCopyService.OnDebugEvent -= DebugEventHandler;
            nopyCopyService.Configuration.PropertyChanged -= EnableToggleEventHandler;
            nopyCopyService.OnNopCommerceSolutionEvent -= NopCommerceSolutionEventHandler;
            nopyCopyService.OnFileSavedEvent -= FileSavedEventHandler;

            attachedHandlers = false;
        }

        public void OnNext(NopyCopyConfiguration value)
        {
            this.Checkbox_Enable.IsChecked = value.IsEnabled;
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