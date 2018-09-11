using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Modals;
using NopyCopyV2.Modals.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        private bool attachedHandlers;
        private IDisposable observerRef;

        #endregion

        #region Ctor(s)

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowControl"/> class.
        /// </summary>
        public MainWindowControl()
        {
            // Init component first.
            InitializeComponent();

            attachedHandlers = false;
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

        public IList<string> Logs { get; private set; }

        public string ErrorMessage { get; set; }

        public NopyCopyService NopyCopyService
        {
            get => nopyCopyService;
            set
            {
                DetachEventHanlders();
                nopyCopyService = value;
                AttachEventHandlers();

                if (value != null)
                {
                    Checkbox_Enable.IsEnabled = true;
                    Checkbox_EnableFileExtensions.IsEnabled = true;
                    Checkbox_Enable.IsChecked = nopyCopyService.Configuration.IsEnabled;
                    Checkbox_EnableFileExtensions.IsChecked = nopyCopyService.Configuration.EnableFileExtensions;

                    if (nopyCopyService.IsSolutionLoaded)
                    {
                        Label_SolutionNameLabel.Content = nopyCopyService.SolutionName;
                        Label_SolutionNameLabel.Visibility = Visibility.Visible;
                        Label_NoSolutionLoadedMessage.Visibility = Visibility.Collapsed;
                    }

                    if (nopyCopyService.Configuration.EnableFileExtensions)
                    {
                        ListView_WatchedFileExtensions.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        ListView_WatchedFileExtensions.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    Checkbox_Enable.IsEnabled = false;
                    Checkbox_EnableFileExtensions.IsEnabled = false;
                }
            }
        }

        public IVsUIShell5 ColorService { get; set; }

        #endregion

        #region Methods

        #region EventHandlers

        private void TrimLogs(object sender, RoutedEventArgs e)
        {
            var validAfter = Logs.Count > 10 ? Logs.Count - 10 : 10;
            Logs = Logs.Where((l, i) => i > validAfter).ToList();
        }

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

        private void Checkbox_Enable_Checked(object sender, RoutedEventArgs e)
        {
            nopyCopyService.Configuration.IsEnabled = true;
        }

        private void Checkbox_Enable_Unchecked(object sender, RoutedEventArgs e)
        {
            nopyCopyService.Configuration.IsEnabled = false;
        }

        private void Checkbox_EnableFileExtensions_Checked(object sender, RoutedEventArgs e)
        {
            nopyCopyService.Configuration.EnableFileExtensions = true;
            ListView_WatchedFileExtensions.Visibility = Visibility.Visible;
        }

        private void Checkbox_EnableFileExtensions_Unchecked(object sender, RoutedEventArgs e)
        {
            nopyCopyService.Configuration.EnableFileExtensions = false;
            ListView_WatchedFileExtensions.Visibility = Visibility.Collapsed;
        }

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

        private void SolutionEventHandler(object sender, SolutionEvent e)
        {
            if (e.SolutionLoaded)
            {
                Label_NoSolutionLoadedMessage.Visibility = Visibility.Collapsed;
                Label_SolutionNameLabel.Visibility = Visibility.Visible;
                Label_SolutionNameLabel.Content = e.SolutionName;
                Logs.Add("Solution loaded");
            }
            else
            {
                Label_NoSolutionLoadedMessage.Visibility = Visibility.Visible;
                Label_SolutionNameLabel.Visibility = Visibility.Collapsed;
                Logs.Add("Solution unloaded");
            }
        }

        private void FileSavedEventHandler(object sender, FileSavedEvent e)
        {
            if (e.HasError)
            {
                Logs.Add($"Didn't copy {e.SavedFile} because {e.Reason}.");
            }
            else
            {
                Logs.Add($"Saved and copied {e.SavedFile.Name} to {e.CopiedTo.FullName}.");
            }
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

        // TODO
        public void UpdateColors()
        {
            //Logs.Add("UpdateColors is still a WIP");
        }

        private void AttachEventHandlers()
        {
            if (nopyCopyService == null || attachedHandlers)
                return;

            observerRef = nopyCopyService.Configuration.Subscribe(this);
            nopyCopyService.OnDebugEvent += DebugEventHandler;
            nopyCopyService.OnSolutionEvent += SolutionEventHandler;
            nopyCopyService.OnFileSavedEvent += FileSavedEventHandler;

            attachedHandlers = true;

            ListView_WatchedFileExtensions.ItemsSource = nopyCopyService
                .Configuration
                .GetWatchedFileExensions();
            Checkbox_Enable.IsChecked = nopyCopyService.Configuration.IsEnabled;
        }

        private void DetachEventHanlders()
        {
            if (nopyCopyService == null || !attachedHandlers)
                return;

            observerRef.Dispose();
            nopyCopyService.OnDebugEvent -= DebugEventHandler;
            nopyCopyService.OnSolutionEvent -= SolutionEventHandler;
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
            Checkbox_EnableFileExtensions.IsChecked = value.EnableFileExtensions;

            if (value.EnableFileExtensions)
            {
                ListView_WatchedFileExtensions.Visibility = Visibility.Visible;
            }
            else
            {
                ListView_WatchedFileExtensions.Visibility = Visibility.Collapsed;
            }
        }

        #endregion
    }
}