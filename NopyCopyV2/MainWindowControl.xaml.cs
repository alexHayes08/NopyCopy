namespace NopyCopyV2
{
    using Microsoft.VisualStudio.Shell.Interop;
    using NopyCopyV2.Modals;
    using System;
    using System.Collections.Generic;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for MainWindowControl.
    /// </summary>
    public partial class MainWindowControl : UserControl
    {
        #region Fields

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

        #endregion

        #region Functions

        #region EventHandlers

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

        private void EnableToggleEventHandler(object sender, EnableToggledEvent e)
        {
            Checkbox_Enable.IsChecked = e.IsEnabled;

            if (e.IsEnabled)
            {
                Logs.Add("Enabled plugin");
            }
            else
            {
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
        }

        private void AttachEventHandlers()
        {
            if (nopyCopyService == null || attachedHandlers)
                return;

            nopyCopyService.OnDebugEvent += DebugEventHandler;
            nopyCopyService.OnEnableToggledEvent += EnableToggleEventHandler;
            nopyCopyService.OnNopCommerceSolutionEvent += NopCommerceSolutionEventHandler;
            nopyCopyService.OnFileSavedEvent += FileSavedEventHandler;
        }

        private void DetachEventHanlders()
        {
            if (nopyCopyService == null || !attachedHandlers)
                return;

            nopyCopyService.OnDebugEvent -= DebugEventHandler;
            nopyCopyService.OnEnableToggledEvent -= EnableToggleEventHandler;
            nopyCopyService.OnNopCommerceSolutionEvent -= NopCommerceSolutionEventHandler;
            nopyCopyService.OnFileSavedEvent -= FileSavedEventHandler;
        }

        #endregion
    }
}