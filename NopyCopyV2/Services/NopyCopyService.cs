﻿using CacheManager.Core;
using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Extensions;
using NopyCopyV2.Modals;
using NopyCopyV2.Modals.Extensions;
using NopyCopyV2.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using static Microsoft.VisualStudio.VSConstants;
using static NopyCopyV2.Extensions.IVsSolutionExtensions;
using static NopyCopyV2.Extensions.NopProjectExtensions;
using IAsyncServiceProvider = Microsoft.VisualStudio.Shell.IAsyncServiceProvider;
using NopyCopyConfiguration = NopyCopyV2.Modals.NopyCopyConfiguration;
using Task = System.Threading.Tasks.Task;

namespace NopyCopyV2
{
    public class NopyCopyService : SNopyCopyService, INopyCopyService
    {
        #region Fields

        private const string NOPYCOPY_BUILD_INFO_FILENAME = ".nopycopy.build.info";
        private const string DESCRIPTION_SYSTEM_NAME_LINE_PREFIX = "SystemName:";
        private const string BUILD_MACRO =
            "if exist $(ProjectDir).nopycopy.build.info (del " +
            "$(ProjectDir).nopycopy.build.info) & echo $(OutDir) >> " +
            "$(ProjectDir).nopycopy.build.info & echo $(TargetDir) >> " +
            "$(ProjectDir).nopycopy.build.info";

        private readonly IAsyncServiceProvider serviceProvider;
        private readonly Dictionary<string, Uri> projectUniqueNameToOutDirMapping;

        private bool isSolutionLoaded;
        private bool isDebugging;
        private Guid outputPaneGuid;

        /// <summary>
        /// The key is the project name, and the value is the plugins system 
        /// name.
        /// </summary>
        private ICacheManager<object> cacheManager;

        // Services
        private RunningDocumentTable runningDocumentTable;
        private DebuggerEvents debuggerEvents;
        private BuildEvents buildEvents;
        private DTE dte;
        private NopyCopyConfiguration configuration;
        private IVsSolution2 solutionService;
        private IVsStatusbar statusBar;
        private IVsOutputWindowPane outputPane;

        // Cookies
        private uint? debugEventsCookie;
        private uint? runningDocumentTableCookie;
        private uint? solutionEventsCookie;

        #region Cache Keys

        public const string SYSTEM_RUNTIME_CACHE_KEY = "system.runtime.cache";
        public const string PROJECT_SYSTEMNAMES_CACHE_KEY = "project.systemnames";

        #endregion

        #endregion

        #region Constructors

        public NopyCopyService(IAsyncServiceProvider provider, OptionsPage options)
        {
            projectUniqueNameToOutDirMapping = new Dictionary<string, Uri>();
            serviceProvider = provider;
            Configuration = new NopyCopyConfiguration(options);
        }

        #endregion

        #region Finalizer

        ~NopyCopyService()
        {
            //UnadviseDebugEvents();
#pragma warning disable VSTHRD110 // Observe result of async calls
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            UnadviseSolutionEventsAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#pragma warning restore VSTHRD110 // Observe result of async calls
        }

        #endregion

        #region Properties

        public bool IsSolutionLoaded
        {
            get => isSolutionLoaded;
            private set
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                isSolutionLoaded = value;
                OnSolutionEvent?.Invoke(
                    this,
                    new SolutionEvent
                    {
                        SolutionName = SolutionName,
                        SolutionLoaded = isSolutionLoaded
                    });
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(IsSolutionLoaded)));
            }
        }

        public bool IsDebugging
        {
            get => isDebugging;
            private set
            {
                isDebugging = value;
                OnDebugEvent?.Invoke(
                    this,
                    new DebugEvent
                    {
                        IsDebugging = value
                    });
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(IsDebugging)));
            }
        }

        public NopyCopyConfiguration Configuration
        {
            get => configuration;
            set
            {
                configuration = value;
                PropertyChanged?.Invoke(
                    this,
                    new PropertyChangedEventArgs(nameof(Configuration)));
            }
        }

        public string SolutionName
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var name = solutionService.GetProperty(
                    (int)__VSPROPID.VSPROPID_SolutionBaseName,
                    out object pvar);

                if (pvar is string caption)
                {
                    return caption;
                }
                else
                {
                    return "Unable to determine the solution name.";
                }
            }
        }

        #endregion

        #region Events

        public event EventHandler<DebugEvent> OnDebugEvent;
        public event EventHandler<SolutionEvent> OnSolutionEvent;
        public event EventHandler<FileSavedEvent> OnFileSavedEvent;
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        #region EventHandlers

        #region IVsRunningDocTableEvents3

        public int OnAfterFirstDocumentLock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return S_OK;
        }

        public int OnBeforeLastDocumentUnlock(uint docCookie, uint dwRDTLockType, uint dwReadLocksRemaining, uint dwEditLocksRemaining)
        {
            return S_OK;
        }

        public int OnAfterSave(uint docCookie)
        {
#pragma warning disable VSTHRD110 // Observe result of async calls
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            OnAfterSaveAsync(docCookie);

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#pragma warning restore VSTHRD110 // Observe result of async calls

            return S_OK;
        }

        private async Task OnAfterSaveAsync(uint docCookie)
        {
            // Ignore if disabled or not debugging.
            if (!IsDebugging)
                return;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var document = await FindDocumentAsync(docCookie);
            var project = document.ProjectItem.ContainingProject;
            var fullPath = document.FullName;

            // If there is no project associated with the doc, ignore.
            if (project == null)
                return;

            // TODO: Check if the file is either a 'local' file or the 'copied'
            // file. Currently only checking if the file is the local version.

            if (await ShouldCopyAsync(document))
            {
                FileSavedEvent projectItemInfoModel = null;

                var key = $"document-key-{document.FullName}";
                if (cacheManager.Exists(key))
                {
                    projectItemInfoModel = cacheManager
                        .Get<FileSavedEvent>(key);
                }
                else
                {
                    string reason = null;
                    string copyingTo = null;

                    // Do-while loop is only being used so the code can 'break'
                    // on the first false condition.
                    do
                    {
                        if (!project.Properties.TryGetProperty("URL",
                            out string projectPath))
                        {
                            reason = "Failed to locate the local path of the " +
                                "project.";
                            break;
                        }

                        if (!project.Properties.TryGetProperty("FullPath",
                            out string projectFolderPath))
                        {
                            reason = "Failed to locate the local path of the " +
                                "project.";
                            break;
                        }

                        // Find the OutputPath.
                        if (!project.ConfigurationManager.ActiveConfiguration
                            .Properties
                            .TryGetProperty("OutputPath", out string outputPath))
                        {
                            reason = "Failed to get the output path of " +
                                "the project.";
                            break;
                        }

                        var projectPathUri = new Uri(projectPath);
                        var fullPathUri = new Uri(fullPath);
                        var diff = projectPathUri.MakeRelativeUri(fullPathUri);

                        // Get the path to copy the file to.
                        copyingTo = Path.Combine(projectFolderPath,
                            outputPath,
                            Uri.UnescapeDataString(diff.ToString()));

                        // Before copying verify the 'copied' already file exists.
                        if (!File.Exists(copyingTo))
                        {
                            reason = "No file exists at the copy location. If " +
                                "the project is a DotNet Core project, verify " +
                                "the .csproj file targets element has the " +
                                "following elements set to false: " +
                                "AppendTargetFrameworkToOutputPath and" +
                                "AppendRuntimeIdentifierToOutputPath";
                            break;
                        }
                    } while (false);

                    projectItemInfoModel = new FileSavedEvent
                    {
                        SavedFile = new FileInfo(document.FullName),
                        CopiedTo = new FileInfo(copyingTo),
                        Reason = reason
                    };

                    cacheManager.Add(key, projectItemInfoModel);
                }

                if (!projectItemInfoModel.HasError)
                {
                    // Copy the file & emit the event.
                    File.Copy(projectItemInfoModel.SavedFile.FullName,
                        projectItemInfoModel.CopiedTo.FullName,
                        true);
                }

                Log(projectItemInfoModel.ToString());
                OnFileSavedEvent(this, projectItemInfoModel);
            }
        }

        public int OnAfterAttributeChange(uint docCookie, uint grfAttribs)
        {
            return S_OK;
        }

        public int OnBeforeDocumentWindowShow(uint docCookie, int fFirstShow, IVsWindowFrame pFrame)
        {
            return S_OK;
        }

        public int OnAfterDocumentWindowHide(uint docCookie, IVsWindowFrame pFrame)
        {
            return S_OK;
        }

        public int OnAfterAttributeChangeEx(uint docCookie, uint grfAttribs, IVsHierarchy pHierOld, uint itemidOld, string pszMkDocumentOld, IVsHierarchy pHierNew, uint itemidNew, string pszMkDocumentNew)
        {
            return S_OK;
        }

        public int OnBeforeSave(uint docCookie)
        {
            return S_OK;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return S_OK;
        }

        #endregion

        #region IVsSolutionEvents

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            Dispatcher.CurrentDispatcher.VerifyAccess();
            IsSolutionLoaded = true;

            return S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            Dispatcher.CurrentDispatcher.VerifyAccess();
            IsSolutionLoaded = false;
            cacheManager.Clear();

            return S_OK;
        }

        #endregion

        #region DebuggerEvents

        private void _debuggerEvents_OnEnterRunMode(dbgEventReason Reason)
        {
            IsDebugging = true;

            // On debug clear the cache manager
            cacheManager.Clear();

            // If the solution is a NopCommerceSolution then begin listening 
            // for file changes
            AdviseRunningDocumentEvents();
        }

        private void _debuggerEvents_OnEnterDesignMode(dbgEventReason Reason)
        {
            IsDebugging = false;
            //UnadviseDocumentEvents(); // TODO: This is commented out only
            // for debugging purposes.

            // Clear the cache here as well
            cacheManager.Clear();
        }

        #endregion

        #endregion

        public async Task InitializeServiceAsync(CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dteService = ServiceProvider.GlobalProvider
                .GetService(typeof(DTE)) as DTE;
            Assumes.Present(dteService);

            var runningDocumentTable = new RunningDocumentTable(
                ServiceProvider.GlobalProvider);
            Assumes.Present(runningDocumentTable);

            var solutionService = ServiceProvider.GlobalProvider
                .GetService(typeof(IVsSolution)) as IVsSolution2;
            Assumes.Present(solutionService);

            var statusBar = ServiceProvider.GlobalProvider
                .GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            Assumes.Present(statusBar);

            var outputPaneService = ServiceProvider.GlobalProvider
                .GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            Assumes.Present(outputPaneService);

            outputPaneGuid = new Guid();
            outputPane = outputPaneService.CreatePaneHelper(
                outputPaneGuid,
                "NopyCopy",
                true,
                true);

            debuggerEvents = dteService.Events.DebuggerEvents;
            buildEvents = dteService.Events.BuildEvents;
            dte = dteService;
            this.runningDocumentTable = runningDocumentTable;
            this.solutionService = solutionService;
            this.statusBar = statusBar;
            IsDebugging = false;
            IsSolutionLoaded = false;

            cacheManager = CacheFactory.Build(PROJECT_SYSTEMNAMES_CACHE_KEY,
                settings =>
                {
                    settings.WithSystemRuntimeCacheHandle(SYSTEM_RUNTIME_CACHE_KEY);
                });

            // Check if a solution is currently loaded
            IsSolutionLoaded = this.solutionService.IsSolutionLoaded();

            // Listen for when debugging starts/ends
            AdviseDebugEvents();

            // Listen for when solution events occur
            await AdviseSolutionEventsAsync();

            AdviseRunningDocumentEvents();
        }

        /// <summary>
        ///     Instead of calling the SetText(...) directly on the _statusBar
        ///     service call this instead to avoid freeze related errors.
        /// </summary>
        /// <param name="message"></param>
        private void Log(string message)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var formattedMsg = String.Format("[{0}] - {1}{2}",
                DateTime.Now.ToString("hh:mm:ss tt"),
                message,
                Environment.NewLine);

            outputPane.OutputStringThreadSafe(formattedMsg);
        }

        private async Task AdviseSolutionEventsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // First check that events aren't already registered
            if (solutionEventsCookie.HasValue)
                return;

            if (S_OK != solutionService.AdviseSolutionEvents(this,
                out uint tempCookie))
            {
                // Error happened registering to the events
                throw new Exception("Error occurred while attempting to " +
                    "listen to solution events.");
            }
            else
            {
                // No error occurred
                solutionEventsCookie = tempCookie;
            }
        }

        private async Task UnadviseSolutionEventsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (solutionEventsCookie.HasValue)
                {
                    try
                    {
                        solutionService.UnadviseSolutionEvents(solutionEventsCookie.Value);
                    }
                    catch (Exception)
                    { }
                    solutionEventsCookie = null;
                }
            });
        }

        private void AdviseDebugEvents()
        {
            // First check that events aren't already registered
            if (debugEventsCookie.HasValue)
            {
                return;
            }

            debuggerEvents.OnEnterDesignMode += _debuggerEvents_OnEnterDesignMode;
            debuggerEvents.OnEnterRunMode += _debuggerEvents_OnEnterRunMode;
            debugEventsCookie = 1;
        }

        private void UnadviseDebugEvents()
        {
            if (debugEventsCookie.HasValue)
            {
                debuggerEvents.OnEnterDesignMode -= _debuggerEvents_OnEnterDesignMode;
                debuggerEvents.OnEnterRunMode -= _debuggerEvents_OnEnterRunMode;
                debugEventsCookie = null;
            }
        }

        private void AdviseRunningDocumentEvents()
        {
            // Check that events aren't already being listened to.
            if (runningDocumentTableCookie.HasValue)
            {
                return;
            }

            runningDocumentTableCookie = runningDocumentTable.Advise(this);
        }

        private void UnadviseDocumentEvents()
        {
            if (runningDocumentTableCookie.HasValue)
            {
                runningDocumentTable.Unadvise(runningDocumentTableCookie.Value);
                runningDocumentTableCookie = null;
            }
        }

        private async Task<IEnumerable<ProjectItem>> GetWhiteListedItemsAsync(
            Project plugin)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var whiteListedItems = new List<ProjectItem>();

            foreach (ProjectItem item in plugin.ProjectItems)
            {
                if (Configuration.GetWatchedFileExensions().Contains(
                    Path.GetExtension(item.Name)))
                {
                    whiteListedItems.Add(item);
                }
            }

            return whiteListedItems;
        }

        private async Task<bool> ShouldCopyAsync(Document document)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (Configuration.EnableFileExtensions)
            {
                var ext = Path.GetExtension(document.FullName);

                // Check extension
                if (string.IsNullOrEmpty(ext))
                    return false;

                var containsExtension = Configuration
                    .GetWatchedFileExensions()
                    .Contains(ext) && Configuration.IsWhiteList;

                if (!containsExtension)
                    return false;
            }

            // Check if 'CopyToOutput' is valid.
            if (!ItemHasCopiedToOutputPropertyAsTrue(document.ProjectItem))
                return false;

            return true;
        }

        private async Task<Document> FindDocumentAsync(uint documentCookie)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var documentInfo = runningDocumentTable.GetDocumentInfo(documentCookie);

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            return dte
                .Documents
                .Cast<Document>()
                .FirstOrDefault(doc => doc.FullName == documentInfo.Moniker);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        }

        private bool ItemExistsInOutputPath(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var project = document.ProjectItem.ContainingProject;
            var srcFullPath = document.FullName;

            if (project == null)
            {
                return false;
            }

            if (!project.ConfigurationManager.ActiveConfiguration.Properties
                .TryGetProperty("OutputPath", out string outputPath))
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}
