﻿using CacheManager.Core;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using NopyCopyV2.Modals;
using NopyCopyV2.Modals.Extensions;
using NopyCopyV2.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Microsoft.VisualStudio.VSConstants;
using static NopyCopyV2.Extensions.IVsSolutionExtensions;
using static NopyCopyV2.Extensions.NopProjectExtensions;
using NopyCopyConfiguration = NopyCopyV2.Modals.NopyCopyConfiguration;

namespace NopyCopyV2
{
    public class NopyCopyService : SNopyCopyService, INopyCopyService
    {
        #region Fields

        private const string DESCRIPTION_SYSTEM_NAME_LINE_PREFIX = "SystemName:";

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
            var dteService = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE;
            var runningDocumentTable = new RunningDocumentTable(ServiceProvider.GlobalProvider);
            var solutionService = ServiceProvider.GlobalProvider.GetService(typeof(IVsSolution)) as IVsSolution2;
            var shellSettingsService = new ShellSettingsManager(ServiceProvider.GlobalProvider);
            var statusBar = ServiceProvider.GlobalProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar;

            _debuggerEvents = dteService.Events.DebuggerEvents;
            _dte = dteService;
            _runningDocumentTable = runningDocumentTable;
            _solutionService = solutionService;
            _statusBar = statusBar;
            IsDebugging = false;
            IsSolutionLoaded = false;
            cacheManager = CacheFactory.Build(PROJECT_SYSTEMNAMES_CACHE_KEY, settings =>
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
            AdviseSolutionEvents();

            // Init nopyCopyService
            Configuration = new NopyCopyConfiguration(options);

            AdviseRunningDocumentEvents();
        }

        #endregion

        #region Finalizer

        ~NopyCopyService()
        {
            //UnadviseDebugEvents();
            UnadviseSolutionEvents();
        }

        #endregion

        #region Properties

        public bool IsSolutionLoaded
        {
            get => isSolutionLoaded;
            private set
            {
                isSolutionLoaded = value;
                OnNopCommerceSolutionEvent?.Invoke(this, new NopCommerceSolutionEvent
                {
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
        public IObservable<string> SolutionNameV2 { get; set; }
        public string SolutionName => _dte.Solution?.FileName;

        #endregion

        #region Events

        public event EventHandler<DebugEvent> OnDebugEvent;
        public event EventHandler<NopCommerceSolutionEvent> OnNopCommerceSolutionEvent;
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
            var document = FindDocument(docCookie);
            var project = document.ProjectItem;
            var fullPath = document.FullName;

            // Return if not disabled or if not debugging
            if (!Configuration.IsEnabled || !IsDebugging)
                return S_OK;

            if (ShouldCopy(document))
            {
                // Fetch the plugin name and it's corresponding system name
                var pluginName = document.ProjectItem.ContainingProject.Name;
                var systemName = (string)cacheManager.GetOrAdd(pluginName, key =>
                {
                    if (document.TryGetSystemNameFromDocument(out string sysName))
                    {
                        return sysName;
                    }
                    else
                    {
                        return null;
                    }
                });

                // Get the path to copy the file to
                var copyingTo = GetFilesCorrespondingWebPluginPath(
                    document.FullName,
                    pluginName,
                    systemName);

                // Copy the file & emit the event
                File.Copy(document.FullName, copyingTo, true);
                OnFileSavedEvent(this, new FileSavedEvent
                {
                    SavedFile = new FileInfo(document.FullName),
                    CopiedTo = new FileInfo(copyingTo)
                });
                PrintToStatusBar($"Copied file from:'{document.FullName}' to:'{copyingTo}'");
            }

            return S_OK;
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
            IsNopCommerceSolution = false;

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
            UnadviseDocumentEvents();

            // Clear the cache here as well
            cacheManager.Clear();
        }

        #endregion

        #endregion

        /// <summary>
        /// Instead of calling the SetText(...) directly on the _statusBar
        /// service call this instead to avoid freeze related errors.
        /// </summary>
        /// <param name="message"></param>
        private void PrintToStatusBar(string message)
        {
            _statusBar.IsFrozen(out int frozen);
            if (frozen != 0)
            {
                _statusBar.FreezeOutput(0);
            }

            _statusBar.SetText(message);
        }

        private void AdviseSolutionEvents()
        {
            // First check that events aren't already registered
            if (solutionEventsCookie.HasValue)
                return;

            if (S_OK != _solutionService.AdviseSolutionEvents(this, out uint tempCookie))
            {
                // Error happened registering to the events
                throw new Exception("Error occurred while attempting to listen to solution events.");
            }
            else
            {
                // No error occurred
                solutionEventsCookie = tempCookie;
            }
        }

        private void UnadviseSolutionEvents()
        {
            if (solutionEventsCookie.HasValue)
            {
                try
                {
                    _solutionService.UnadviseSolutionEvents(solutionEventsCookie.Value);
                } catch(Exception)
                { }
                solutionEventsCookie = null;
            }
        }

        private void AdviseDebugEvents()
        {
            // First check that events aren't already registered
            if (debugEventsCookie.HasValue)
                return;

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

        private void AdviseRunningDocumentEvents()
        {
            // Check that events aren't already being listened to.
            if (runningDocumentTableCookie.HasValue)
                return;

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

        private IEnumerable<ProjectItem> GetWhiteListedItems(Project plugin)
        {
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

        private bool ShouldCopy(Document document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var result = false;
            var ext = Path.GetExtension(document.FullName);

            // Check extension
            if (string.IsNullOrEmpty(ext))
            {
                return false;
            }

            var containsExtension = Configuration
                .GetWatchedFileExensions()
                .Contains(ext) && Configuration.IsWhiteList;

            // Check if 'CopyToOutput' is valid.
            var project = document.ProjectItem.ContainingProject;
            _solutionService.GetProjectOfUniqueName(project.UniqueName,
                out IVsHierarchy hierarchy);
            result = IsItemCopiedToOutput(project, document.ProjectItem);

            return result;
        }

        private Document FindDocument(uint documentCookie)
        {
            var documentInfo = _runningDocumentTable.GetDocumentInfo(documentCookie);

            return _dte
                .Documents
                .Cast<Document>()
                .FirstOrDefault(doc => doc.FullName == documentInfo.Moniker);
        }

        #endregion
    }
}
