using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Properties;
using NopyCopyV2.Services;
using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using static Microsoft.VisualStudio.VSConstants;

namespace NopyCopyV2
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// <param name="">
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </param>
    /// </remarks>
    [ProvideAutoLoad(PackageGuidString, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideService(typeof(SNopyCopyService))]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(MainWindow))]
    //[Guid(GuidList.guidMainWindowPackage)]
    [Guid(MainWindowPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideToolWindow(typeof(MainWindow))]
    [ProvideOptionPage(typeof(OptionsPage),
        OptionsPage.CATEGORY_NAME, "General", 0, 0, true)]
    public sealed class MainWindowPackage : AsyncPackage, IVsShellPropertyEvents
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

        private async void ShowToolWindowAsync(object sender, EventArgs e)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            // Get the instance number 0 of this tool window. This window is a single instance so 
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
            if (ThreadHelper.JoinableTaskContext.IsOnMainThread)
            {
                // Detach event handlers
                if (_vsShell != null && shellPropertyChangedCookie != 0)
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                    _vsShell.UnadviseShellPropertyChanges(shellPropertyChangedCookie);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            }

            base.Dispose(disposing);
        }

        #region Package Members

        private object CreateServiceVSDKHelperService(IServiceContainer container, Type serviceType)
        {
            if (typeof(SVSDKHelperService) == serviceType)
                return new VSDKHelperService();
            return null;
        }

        private object CreateServiceNopyCopyService(IServiceContainer container, Type serviceType)
        {
            if (typeof(SNopyCopyService) == serviceType)
            {
                var optionsPage = GetDialogPage(typeof(OptionsPage)) as OptionsPage;
                return new NopyCopyService(this, optionsPage);
            }
            return null;
        }

        /// <summary>
        /// Initialization of the package; this method is called right after 
        /// the package is sited, so this is the place where you can put all 
        /// the initialization code that rely on services provided by 
        /// VisualStudio.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        protected override async System.Threading.Tasks.Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            // Make initial progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Initializing",
                progressText: "Initializing",
                currentStep: 0,
                totalSteps: 10));
            var serviceContainer = this as IServiceContainer;

            // Check if cancelled.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Calling base.InitializeAsync",
                progressText: "Initializing",
                currentStep: 1,
                totalSteps: 10));
            await base.InitializeAsync(cancellationToken, progress);
            MainWindowCommand.Initialize(this);

            // Check if cancelled.
            if (cancellationToken.IsCancellationRequested)
                return;

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Registering services",
                progressText: "Initializing",
                currentStep: 2,
                totalSteps: 10));
            ServiceCreatorCallback nopyCopyCallback =
                new ServiceCreatorCallback(CreateServiceNopyCopyService);
            serviceContainer.AddService(typeof(SNopyCopyService), nopyCopyCallback);

            // Check if cancelled.
            if (cancellationToken.IsCancellationRequested)
                return;

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Retrieving services",
                progressText: "Retrieving NopyCopyService",
                currentStep: 3,
                totalSteps: 10));
            nopyCopyService = await GetServiceAsync(typeof(SNopyCopyService))
                as NopyCopyService;

            // Check if cancelled.
            if (cancellationToken.IsCancellationRequested)
                return;

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Retrieving services",
                progressText: "Retrieving IVsShell",
                currentStep: 4,
                totalSteps: 10));

            // Get IVsShell service.
            _vsShell = ServiceProvider.GlobalProvider
                .GetService(typeof(SVsShell)) as IVsShell;

            // Check if cancelled.
            if (cancellationToken.IsCancellationRequested)
                return;

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

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Retrieving services",
                progressText: "Retrieving ToolWindow",
                currentStep: 5,
                totalSteps: 10));

            // Get tool window
            if (toolWindow == null)
            {
                toolWindow = FindToolWindow(typeof(MainWindow), 0, true) as MainWindow;
            }

            // Check if cancelled.
            if (cancellationToken.IsCancellationRequested)
                return;

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Retrieving services",
                progressText: "Retrieving IVsUIShell5",
                currentStep: 6,
                totalSteps: 10));
            var colorService = ServiceProvider.GlobalProvider.GetService(typeof(IVsUIShell5))
                as IVsUIShell5;

            // Check if cancelled.
            if (cancellationToken.IsCancellationRequested)
                return;

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Retrieving services",
                progressText: "Retrieving DTE",
                currentStep: 7,
                totalSteps: 10));
            var dteService = ServiceProvider.GlobalProvider.GetService(typeof(DTE))
                as DTE;

            // Check if cancelled.
            if (cancellationToken.IsCancellationRequested)
                return;

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Retrieving services",
                progressText: "Retrieving DTE",
                currentStep: 8,
                totalSteps: 10));
            var runningDocumentTable = new RunningDocumentTable(this);

            // Check if cancelled.
            if (cancellationToken.IsCancellationRequested)
                return;

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Retrieving services",
                progressText: "Retrieving DTE",
                currentStep: 9,
                totalSteps: 10));
            var solutionService = ServiceProvider.GlobalProvider.GetService(typeof(IVsSolution)) as IVsSolution2;

            // Check if cancelled.
            if (cancellationToken.IsCancellationRequested)
                return;

            toolWindow.ColorService = colorService;
            toolWindow.SetupEvents(nopyCopyService);

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Finished",
                progressText: "Completed",
                currentStep: 10,
                totalSteps: 10));
            return;
        }

        #endregion

        public int OnShellPropertyChange(int propid, object var)
        {
            return S_OK;
        }

        #endregion
    }
}
