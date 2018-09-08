﻿using CacheManager.Core;
using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using NopyCopyV2.Extensions;
using NopyCopyV2.Modals;
using NopyCopyV2.Modals.Extensions;
using NopyCopyV2.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.VisualStudio.VSConstants;
using static NopyCopyV2.Extensions.IVsSolutionExtensions;
using static NopyCopyV2.Extensions.NopProjectExtensions;
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

        private readonly IServiceProvider serviceProvider;

        private bool isSolutionLoaded;
        private bool isDebugging;

        /// <summary>
        /// The key is the project name, and the value is the plugins system 
        /// name.
        /// </summary>
        private ICacheManager<object> cacheManager;

        // Services
        private readonly RunningDocumentTable _runningDocumentTable;
        private readonly ShellSettingsManager _shellSettingsManager;
        private readonly DebuggerEvents _debuggerEvents;
        private readonly BuildEvents _buildEvents;
        private readonly DTE _dte;
        private readonly IVsSolution2 _solutionService;
        private readonly IVsStatusbar _statusBar;
        //private readonly IVSDKHelperService _vsdkHelpers;

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

        public NopyCopyService(IServiceProvider serviceProvider, OptionsPage options)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            this.serviceProvider = serviceProvider;

            var dteService = ServiceProvider.GlobalProvider
                .GetService(typeof(DTE)) as DTE;
            Assumes.Present(dteService);

            var runningDocumentTable = new RunningDocumentTable(
                ServiceProvider.GlobalProvider);
            var solutionService = ServiceProvider.GlobalProvider
                .GetService(typeof(IVsSolution)) as IVsSolution2;
            var shellSettingsService = new ShellSettingsManager(
                ServiceProvider.GlobalProvider);
            var statusBar = ServiceProvider.GlobalProvider
                .GetService(typeof(SVsStatusbar)) as IVsStatusbar;

            _debuggerEvents = dteService.Events.DebuggerEvents;
            _BuildEvents = dteService.Events.BuildEvents;
            _dte = dteService;
            _runningDocumentTable = runningDocumentTable;
            _solutionService = solutionService;
            _statusBar = statusBar;
            IsDebugging = false;
            IsSolutionLoaded = false;

            cacheManager = CacheFactory.Build(PROJECT_SYSTEMNAMES_CACHE_KEY,
                settings =>
                {
                    settings.WithSystemRuntimeCacheHandle(SYSTEM_RUNTIME_CACHE_KEY);
                });

            // Check if a solution is currently loaded
            if (_solutionService.IsSolutionLoaded())
            {
                // Check it the loaded solution is a nop commerce solution
                IsSolutionLoaded = true;
            }
            else
            {
                IsSolutionLoaded = false;
            }

            // Listen for when debugging starts/ends
            AdviseDebugEvents();

            // Listen for when solution events occur
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
#pragma warning disable VSTHRD110 // Observe result of async calls
            AdviseSolutionEventsAsync();
#pragma warning restore VSTHRD110 // Observe result of async calls
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            // Init nopyCopyService
            Configuration = new NopyCopyConfiguration(options);

            AdviseRunningDocumentEvents();
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
                isSolutionLoaded = value;
                OnSolutionEvent?.Invoke(this, new SolutionEvent
                {
                    SolutionName = SolutionName,
                    SolutionLoaded = isSolutionLoaded
                });
            }
        }

        public bool IsDebugging
        {
            get => isDebugging;
            private set
            {
                isDebugging = value;
                OnDebugEvent?.Invoke(this, new DebugEvent
                {
                    IsDebugging = value
                });
            }
        }

        public NopyCopyConfiguration Configuration { get; set; }

        public string SolutionName
        {
            get
            {
                //_dte.Solution?.FileName;

                ThreadHelper.ThrowIfNotOnUIThread();

                var name = _solutionService.GetProperty(
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
            // UNCOMMENT THIS
            // Ignore if disabled or not debugging.
            //if (!Configuration.IsEnabled || !IsDebugging)
            //{
            //    return;
            //}

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var document = await FindDocumentAsync(docCookie);
            var project = document.ProjectItem.ContainingProject;
            var fullPath = document.FullName;

            // Retrieve the build info
            var buildInfoCacheKey = $"{project.UniqueName}-buildinfo-file";
            BuildModel buildFileInfo = null;

            if (cacheManager.Exists(buildInfoCacheKey))
            {
                buildFileInfo = cacheManager.Get<BuildModel>(buildInfoCacheKey);
            }
            else
            {
                var fileInfo = await project.TryGetFileInfoAsync(
                    NOPYCOPY_BUILD_INFO_FILENAME);
                var lines = File.ReadAllLines(fileInfo.FullName);

                // If there aren't two lines return.
                if (lines.Length != 2)
                {
                    return;
                }

                buildFileInfo = new BuildModel
                {
                    OutDir = lines[0],
                    ProjectName = lines[1]
                };
                cacheManager.Add(buildInfoCacheKey, buildFileInfo);
            }

            if (buildFileInfo != null && await ShouldCopyAsync(document))
            {
                if (!project.Properties.TryGetProperty("LocalPath",
                    out string localPath))
                {
                    throw new Exception("Failed to get the local path of the" +
                        " project.");
                }

                // Try and find the OutDir first.
                if (!project.ConfigurationManager.ActiveConfiguration
                    .Properties
                    .TryGetProperty("OutDir", out string outputPath))
                {
                    // If the OutDir was not found use the OutputPath.
                    if (!project.ConfigurationManager.ActiveConfiguration
                        .Properties
                        .TryGetProperty("OutputPath", out outputPath))
                    {
                        throw new Exception("Failed to get the output path of " +
                            "the project.");
                    }
                }

                var localPathUri = new Uri(localPath);
                var diff = localPathUri.MakeRelativeUri(fullPath);

                // Get the path to copy the file to.
                var copyingTo = Path.Combine(localPath,
                    outputPath,
                    Uri.UnescapeDataString(diff.ToString()));

                // Copy the file & emit the event.
                File.Copy(document.FullName, copyingTo, true);
                OnFileSavedEvent(this, new FileSavedEvent
                {
                    SavedFile = new FileInfo(document.FullName),
                    CopiedTo = new FileInfo(copyingTo)
                });

                await PrintToStatusBarAsync($"Copied file from:'{document.FullName}' to:'{copyingTo}'");
            }
            else
            {
                OnFileSavedEvent(this, new FileSavedEvent
                {
                    SavedFile = new FileInfo(document.FullName),
                    CopiedTo = null
                });
            }
        }

        private void BuildEvents_OnBuildProjConfigDone(string Project,
            string ProjectConfig,
            string Platform,
            string SolutionConfig,
            bool Success)
        {
            throw new NotImplementedException();
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

        /// <summary>
        ///     Instead of calling the SetText(...) directly on the _statusBar
        ///     service call this instead to avoid freeze related errors.
        /// </summary>
        /// <param name="message"></param>
        private async Task PrintToStatusBarAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _statusBar.IsFrozen(out int frozen);
            if (frozen != 0)
            {
                _statusBar.FreezeOutput(0);
            }

            _statusBar.SetText(message);
        }

        private async Task AdviseSolutionEventsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // First check that events aren't already registered
            if (solutionEventsCookie.HasValue)
            {
                return;
            }

            if (S_OK != _solutionService.AdviseSolutionEvents(this,
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
                        _solutionService.UnadviseSolutionEvents(solutionEventsCookie.Value);
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

            _debuggerEvents.OnEnterDesignMode += _debuggerEvents_OnEnterDesignMode;
            _debuggerEvents.OnEnterRunMode += _debuggerEvents_OnEnterRunMode;
            debugEventsCookie = 1;
        }

        private void UnadviseDebugEvents()
        {
            if (debugEventsCookie.HasValue)
            {
                _debuggerEvents.OnEnterDesignMode -= _debuggerEvents_OnEnterDesignMode;
                _debuggerEvents.OnEnterRunMode -= _debuggerEvents_OnEnterRunMode;
                debugEventsCookie = null;
            }
        }

        private void AdviseBuildEvents()
        {
            _buildEvents.OnBuildProjConfigBegin += _buildEvents_OnBuildProjConfigBegin;
            _buildEvents.OnBuildProjConfigDone += _buildEvents_OnBuildProjConfigDone;
            _buildEvents.OnBuildBegin += _buildEvents_OnBuildBegin;
            _buildEvents.OnBuildDone += _buildEvents_OnBuildDone;
        }

        private void _buildEvents_OnBuildProjConfigBegin(string Project,
            string ProjectConfig,
            string Platform,
            string SolutionConfig)
        {
            throw new NotImplementedException();
        }

        private void _buildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            throw new NotImplementedException();
        }

        private void _buildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            throw new NotImplementedException();
        }

        private void _buildEvents_OnBuildProjConfigDone(string Project,
            string ProjectConfig,
            string Platform,
            string SolutionConfig,
            bool Success)
        {
            if (Success)
            {

            }
        }

        private void AdviseRunningDocumentEvents()
        {
            // Check that events aren't already being listened to.
            if (runningDocumentTableCookie.HasValue)
            {
                return;
            }

            runningDocumentTableCookie = _runningDocumentTable.Advise(this);
        }

        private void UnadviseDocumentEvents()
        {
            if (runningDocumentTableCookie.HasValue)
            {
                _runningDocumentTable.Unadvise(runningDocumentTableCookie.Value);
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

            var ext = Path.GetExtension(document.FullName);

            // Check extension
            if (string.IsNullOrEmpty(ext))
            {
                return false;
            }

            if (Configuration.EnableFileExtensions)
            {
                var containsExtension = Configuration
                    .GetWatchedFileExensions()
                    .Contains(ext) && Configuration.IsWhiteList;

                if (!containsExtension)
                {
                    return false;
                }
            }

            // Check if 'CopyToOutput' is valid.
            var result = IsItemCopiedToOutput(document.ProjectItem);

            return result;
        }

        private async Task<Document> FindDocumentAsync(uint documentCookie)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var documentInfo = _runningDocumentTable.GetDocumentInfo(documentCookie);

#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            return _dte
                .Documents
                .Cast<Document>()
                .FirstOrDefault(doc => doc.FullName == documentInfo.Moniker);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        }

        #endregion
    }
}
