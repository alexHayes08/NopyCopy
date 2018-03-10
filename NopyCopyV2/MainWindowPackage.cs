using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Modals;
using NopyCopyV2.Properties;
using NopyCopyV2.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static Microsoft.VisualStudio.VSConstants;

namespace NopyCopyV2
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [ProvideService(typeof(SNopyCopyService))]
    [ProvideService(typeof(SVSDKHelperService))]
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(MainWindow))]
    //[Guid(GuidList.guidMainWindowPackage)]
    [Guid(MainWindowPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideToolWindow(typeof(MainWindow))]
    [ProvideOptionPage(typeof(OptionsPage), 
        OptionsPage.CATEGORY_NAME, "General", 0, 0, true)]
    public sealed class MainWindowPackage : PackageV2, IVsShellPropertyEvents
    {
        #region Fields

        /// <summary>
        /// MainWindowPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "1af9e209-e5f4-4b5c-853f-6c9f46072d29";

        private uint shellPropertyChangedCookie;
        private IVsShell _vsShell = null;
        private MainWindow toolWindow = null;
        private NopyCopyService nopyCopyService = null;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindowPackage()
        { }

        #endregion

        #region Functions

        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is a  single instance so 
            // this instance is the only one.
            // The last flag is set to true so that if the tool window doesn't exist it will be created
            toolWindow = FindToolWindow(typeof(MainWindow), 0, true) as MainWindow;
            if ((null == toolWindow) || (null == toolWindow.Frame))
            {
                throw new NotSupportedException(Resources.ToolWindowInstantiantionError);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)toolWindow.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            // Detach event handlers

            if (_vsShell != null && shellPropertyChangedCookie != 0)
                _vsShell.UnadviseShellPropertyChanges(shellPropertyChangedCookie);
        }

        #region Package Members

        private object CreateServiceVSDKHelperService(IServiceContainer container, Type serviceType)
        {
            if (typeof(SVSDKHelperService) == serviceType)
                return new VSDKHelperService(this);
            return null;
        }

        private object CreateServiceNopyCopyService(IServiceContainer container, Type serviceType)
        {
            if (typeof(SNopyCopyService) == serviceType)
                return new NopyCopyService(this);
            return null;
        }

        /// <summary>
        /// Initialization of the package; this method is called right after 
        /// the package is sited, so this is the place where you can put all 
        /// the initialization code that rely on services provided by 
        /// VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            var serviceContainer = this as IServiceContainer;

            ServiceCreatorCallback vsdkHelperCallback = new
                ServiceCreatorCallback(CreateServiceVSDKHelperService);
            serviceContainer.AddService(typeof(SVSDKHelperService), vsdkHelperCallback);

            ServiceCreatorCallback nopyCopyCallback =
                new ServiceCreatorCallback(CreateServiceNopyCopyService);
            serviceContainer.AddService(typeof(SNopyCopyService), nopyCopyCallback);

            nopyCopyService = GetService(typeof(SNopyCopyService)) as NopyCopyService;

            base.Initialize();
            MainWindowCommand.Initialize(this);

            // Get shell object
            _vsShell = ServiceProvider.GlobalProvider.GetService(typeof(SVsShell)) as IVsShell;
            if (_vsShell != null)
            {
                // Initialize visual effect values so themes can determine if 
                // various effects are supported by the environment
                object effectsAllowed;
                if (ErrorHandler.Succeeded(_vsShell.GetProperty((int)__VSSPROPID4.VSSPROPID_VisualEffectsAllowed, out effectsAllowed)))
                {
                    Debug.Assert(effectsAllowed is int, "VSSPROPID_VisualEffectsAllowed should be of type int");
                }

                if (S_OK != _vsShell.AdviseShellPropertyChanges(this, out shellPropertyChangedCookie))
                {
                    // Error occurred while trying to listen to shell events
                }
            }

            // Get tool window
            if (toolWindow == null)
            {
                toolWindow = FindToolWindow(typeof(MainWindow), 0, true) as MainWindow;
            }

            // Register needed services
            var colorService = ServiceProvider.GlobalProvider.GetService(typeof(IVsUIShell5)) as IVsUIShell5;
            var dteService = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
            var runningDocumentTable = new RunningDocumentTable(this);
            var solutionService = ServiceProvider.GlobalProvider.GetService(typeof(IVsSolution)) as IVsSolution2;

            toolWindow.ColorService = colorService;
            toolWindow.SetupEvents(nopyCopyService);
        }

        #endregion

        public int OnShellPropertyChange(int propid, object var)
        {
            toolWindow.Logs.Add("OnShellPropertyChange " + propid);
            return S_OK;
        }

        #endregion
    }
}
