using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Services;
using System;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

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
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideService(serviceType: typeof(SNopyCopyService), IsAsyncQueryable = true)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(NopyCopyPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideOptionPage(
        pageType: typeof(OptionsPage),
        categoryName: OptionsPage.CATEGORY_NAME,
        pageName: "General",
        categoryResourceID: 0,
        pageNameResourceID: 0,
        supportsAutomation: true)]
    public sealed class NopyCopyPackage : AsyncPackage
    {
        #region Fields

        /// <summary>
        /// MainWindowPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "1af9e209-e5f4-4b5c-853f-6c9f46072d29";

        private INopyCopyService nopyCopyService = null;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public NopyCopyPackage()
        { }

        #endregion

        #region Methods

        #region Package Members

        private async Task<object> CreateServiceNopyCopyServiceAsync(
            IAsyncServiceContainer container,
            CancellationToken cancellationToken,
            Type serviceType)
        {
            if (typeof(SNopyCopyService) == serviceType)
            {
                var optionsPage = GetDialogPage(typeof(OptionsPage)) as OptionsPage;
                var service = new NopyCopyService(this, optionsPage);
                await service.InitializeServiceAsync(cancellationToken);
                return service;
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
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Make initial progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Initializing",
                progressText: "Initializing",
                currentStep: 1,
                totalSteps: 4));
            var serviceContainer = this as IServiceContainer;

            await base.InitializeAsync(cancellationToken, progress);

            // Check if cancelled.
            if (cancellationToken.IsCancellationRequested)
                return;

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Registering services",
                progressText: "Initializing",
                currentStep: 2,
                totalSteps: 4));
            AddService(typeof(SNopyCopyService), CreateServiceNopyCopyServiceAsync);

            // Check if cancelled.
            if (cancellationToken.IsCancellationRequested)
                return;

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Retrieving services",
                progressText: "Retrieving NopyCopyService",
                currentStep: 3,
                totalSteps: 4));

            nopyCopyService = await GetServiceAsync(typeof(SNopyCopyService))
                as NopyCopyService;
            Assumes.Present(nopyCopyService);

            var runningDocumentTable = new RunningDocumentTable(this);

            // Make progress report.
            progress.Report(new ServiceProgressData(
                waitMessage: "Finished",
                progressText: "Completed",
                currentStep: 4,
                totalSteps: 4));

            return;
        }

        #endregion

        #endregion
    }
}
