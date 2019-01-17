using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Properties;
using NopyCopyV2.Services;
using NopyCopyV2.Xaml;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace NopyCopyV2
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("7d72209a-143d-4e8d-b2c0-8fab8813ec86")]
    internal class MainWindow : ToolWindowPane
    {
        #region Fields

        public MainWindowControl mainWindow;
        public INopyCopyService NopyCopyService;

        #endregion

        #region Ctor(s)

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow() : base(null)
        {
            Caption = Resources.ApplicationTitle;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            mainWindow = new MainWindowControl();
            mainWindow.DataContext = mainWindow;
            mainWindow.Dispatcher.UnhandledException += Dispatcher_UnhandledException;
            Content = mainWindow;

            //ToolBar = new CommandID(MainWindowCommand.CommandSet, MainWindowCommand.CommandId);
            //ToolBarLocation = (int)VSTWT_LOCATION.VSTWT_TOP;
        }

        #endregion

        #region Properties

        public string ErrorMessage
        {
            get
            {
                return mainWindow.ErrorMessage;
            }
        }

        public IList<string> Logs
        {
            get
            {
                return mainWindow.Logs;
            }
        }

        public IVsUIShell5 ColorService
        {
            get
            {
                return mainWindow.ColorService;
            }
            set
            {
                mainWindow.ColorService = value;
            }
        }

        #endregion

        #region Methods

        public void UpdateColors()
        {
            mainWindow.UpdateColors();
        }

        public void SetupEvents(INopyCopyService nopyCopyService)
        {
            mainWindow.NopyCopyService = nopyCopyService;
            NopyCopyService = nopyCopyService;
        }

        private void Dispatcher_UnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ToString());
            if (e.Exception is InvalidCastException)
            {
                // FIXME: Currently this code assumes that it was caused by
                // the presentation framework.
                mainWindow.ErrorMessage = e.ToString();
                e.Handled = true;
            }
        }

        #endregion
    }
}
