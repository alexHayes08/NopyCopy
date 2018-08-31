using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Modals;
using NopyCopyV2.Modals.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using NopyCopyConfiguration = NopyCopyV2.Modals.NopyCopyConfiguration;

namespace NopyCopyV2.Xaml
{
    /// <summary>
    /// Interaction logic for MainWindowControl.
    /// </summary>
    public partial class MainWindowControl : UserControl, IObserver<INopyCopyConfiguration>
    {
        #region Fields

        private const string DEFAULT_SOLUTION_NAME_PLACEHOLDER =
            "No solution loaded";
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

            Checkbox_Enable.ToolTip = new ToolTip
            {
                Content = DEFAULT_SOLUTION_NAME_PLACEHOLDER
            };
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
        public string ErrorMessage { get; set; }
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

        private void Button_AddOverride_Click(object sender, RoutedEventArgs e)
        {
            if (nopyCopyService == null)
                return;

            SetNewOverrideVisibility(false);
        }

        private void Button_NewOverrideConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (nopyCopyService == null)
                return;

            SetNewOverrideVisibility(true);
        }

        private void Button_NewOverrideCancel_Click(object sender, RoutedEventArgs e)
        {
            if (nopyCopyService == null)
                return;

            SetNewOverrideVisibility(true);
        }

        private void Button_confirmNewExtension_Click(object sender, RoutedEventArgs e)
        {
            if (nopyCopyService == null)
                return;

            var newExtensionName = TextBox_newExtension.Text;
            TextBox_newExtension.Text = "";
            nopyCopyService.Configuration.WatchedFileExtensions += ", " + newExtensionName;

            DockPanel_NewExtensionContainer.Visibility = Visibility.Collapsed;
            StackPanel_AddAndDeleteFileExtBtnsContainer.Visibility = Visibility.Visible;
        }

        private void Button_cancelNewExtension_Click(object sender, RoutedEventArgs e)
        {
            TextBox_newExtension.Text = "";
            DockPanel_NewExtensionContainer.Visibility = Visibility.Collapsed;
            StackPanel_AddAndDeleteFileExtBtnsContainer.Visibility = Visibility.Visible;
        }

        private void Button_AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (nopyCopyService == null)
                return;

            DockPanel_NewExtensionContainer.Visibility = Visibility.Visible;
            StackPanel_AddAndDeleteFileExtBtnsContainer.Visibility = Visibility.Collapsed;
            TextBox_newExtension.Text = "";
        }

        private void Button_DeleteItems_Click(object sender, RoutedEventArgs e)
        {
            if (nopyCopyService == null)
                return;

            while(ListView_WatchedFileExtensions.SelectedItems.Count > 0)
            {
                if (ListView_WatchedFileExtensions.SelectedItems[0] is string item)
                {
                    nopyCopyService
                        .Configuration
                        .GetWatchedFileExensions()
                        .Where(str => str != item);
                }
            }
        }

        //private void Checkbox_Enable_Checked(object sender, RoutedEventArgs e)
        //{
        //    nopyCopyService.Configuration.IsEnabled = true;
        //}

        //private void Checkbox_Enable_Unchecked(object sender, RoutedEventArgs e)
        //{
        //    nopyCopyService.Configuration.IsEnabled = false;
        //}

        private void DebugEventHandler(object sender, DebugEvent e)
        {
            if (e.IsDebugging)
            {
                Logs.Add("Started debugging");

                // Have the message box display debug message
                Label_DebugMessageBox.Visibility = Visibility.Visible;
                DockPanel_StatusesContainer.Visibility = Visibility.Visible;

                if (nopyCopyService.Configuration.IsEnabled)
                {
                    Ellipse_ActiveIndicator.Fill = new SolidColorBrush(Colors.Green);
                    Label_ActiveIndicator.Content = "Active";
                }
                else
                {
                    Ellipse_ActiveIndicator.Fill = new SolidColorBrush(Colors.Orange);
                    Label_ActiveIndicator.Content = "Inactive";
                }
            }
            else
            {
                Logs.Add("Stopped debugging");

                // Have the message box hide debug message
                Label_DebugMessageBox.Visibility = Visibility.Collapsed;
                DockPanel_StatusesContainer.Visibility = Visibility.Collapsed;
            }
        }

        private void ConfigurationUpdatedHandler(object sender, PropertyChangedEventArgs e)
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
                        Checkbox_Enable.ToolTip = DEFAULT_SOLUTION_NAME_PLACEHOLDER;
                        Logs.Add("Disabled plugin");
                    }

                    break;
                case nameof(nopyCopyService.Configuration.IsWhiteList):
                    // TODO
                    break;
                case nameof(nopyCopyService.Configuration.WatchedFileExtensions):
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

        private void SetNewOverrideVisibility(bool showToolbar)
        {
            //if (showToolbar)
            //{
            //    Grid_OverridesToolbar.Visibility = Visibility.Visible;
            //    DockPanel_NewOverrideContainer.Visibility = Visibility.Collapsed;
            //}
            //else
            //{
            //    Grid_OverridesToolbar.Visibility = Visibility.Collapsed;
            //    DockPanel_NewOverrideContainer.Visibility = Visibility.Visible;
            //}
        }

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
            nopyCopyService.Configuration.PropertyChanged += ConfigurationUpdatedHandler;
            nopyCopyService.OnNopCommerceSolutionEvent += NopCommerceSolutionEventHandler;
            nopyCopyService.OnFileSavedEvent += FileSavedEventHandler;

            attachedHandlers = true;

            ListView_WatchedFileExtensions.ItemsSource = nopyCopyService
                .Configuration
                .GetWatchedFileExensions();
            Checkbox_Enable.IsChecked = nopyCopyService.Configuration.IsEnabled;
            //RadioButton_ListedFileExtnesions_IsWhiteList.IsChecked = nopyCopyService.Configuration.IsWhiteList;
            //RadioButton_ListedFileExtnesions_IsBlackList.IsChecked = !nopyCopyService.Configuration.IsWhiteList;
        }

        private void DetachEventHanlders()
        {
            if (nopyCopyService == null || !attachedHandlers)
                return;

            nopyCopyService.OnDebugEvent -= DebugEventHandler;
            nopyCopyService.Configuration.PropertyChanged -= ConfigurationUpdatedHandler;
            nopyCopyService.OnNopCommerceSolutionEvent -= NopCommerceSolutionEventHandler;
            nopyCopyService.OnFileSavedEvent -= FileSavedEventHandler;

            attachedHandlers = false;
        }

        public void OnError(Exception error)
        {
            Logs.Add(error.ToString());
        }

        public void OnCompleted()
        {
            Logs.Add("IObserver<INopyCopyConfiguration>.Completed() called.");
        }

        public void OnNext(INopyCopyConfiguration value)
        {
            ListView_WatchedFileExtensions.ItemsSource = value
                .GetWatchedFileExensions();

            Checkbox_Enable.IsChecked = value.IsEnabled;
        }

        #endregion
    }
}