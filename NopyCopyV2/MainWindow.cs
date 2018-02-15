namespace NopyCopyV2
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using NopyCopyV2.Properties;

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
        private MainWindowControl mainWindow;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow() : base(null)
        {
            this.Caption = Resources.ApplicationTitle;

            // TODO: Get NopyCopyConfiguration object from VS options
            var config = new NopyCopyConfiguration
            {
                ListedFileExtensions = new List<string>()
                {
                    ".cshtml",
                    ".js",
                    ".html",
                    ".css",
                    ".scss"
                }
            };

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            mainWindow = new MainWindowControl();
            Content = mainWindow;
        }

        #region Properties

        public bool? Enable
        {
            get
            {
                return mainWindow.Checkbox_Enable.IsChecked;
            }
            set
            {
                mainWindow.Checkbox_Enable.IsChecked = value;
            }
        }

        public bool? IsNopCommerceProject
        {
            get
            {
                return mainWindow.Checkbox_IsNopCommerceProject.IsChecked;
            }
            set
            {
                mainWindow.Checkbox_IsNopCommerceProject.IsChecked = value;
            }
        }

        public IList<string> Logs
        {
            get
            {
                return mainWindow.Logs;
            }
        }

        #endregion

        #region Methods

        public void SetupEvents(NopyCopyService nopyCopyService)
        {
            mainWindow.NopyCopyService = nopyCopyService;
        }

        #endregion
    }
}
