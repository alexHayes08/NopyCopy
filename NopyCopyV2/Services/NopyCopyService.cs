using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Modals;
using NopyCopyV2.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using static Microsoft.VisualStudio.VSConstants;
using static NopyCopyV2.Extensions.IVsHierarchyExtensions;
using static NopyCopyV2.Extensions.IVsSolutionExtensions;
using static NopyCopyV2.Extensions.NopProjectExtensions;

namespace NopyCopyV2
{
    public class NopyCopyService : SNopyCopyService, INopyCopyService
    {
        #region Fields

        private const string DESCRIPTION_SYSTEM_NAME_LINE_PREFIX = "SystemName:";

        private bool isSolutionLoaded;
        private bool isNopCommerceSolution;
        private bool isDebugging;
        private IList<IObserver<NopyCopyConfiguration>> observers;

        /// <summary>
        /// The key is the project name, and the value is the plugins system 
        /// name.
        /// </summary>
        private IDictionary<string, string> projectRootFolders;

        // Services
        private readonly IServiceProvider _serviceProvider;
        private readonly RunningDocumentTable _runningDocumentTable;
        private readonly DebuggerEvents _debuggerEvents;
        private readonly DTE _dte;
        private readonly IVsSolution2 _solutionService;

        // Cookies
        private uint? debugEventsCookie;
        private uint? runningDocumentTableCookie;
        private uint? solutionEventsCookie;

        #endregion

        #region Constructors

        public NopyCopyService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var dteService = _serviceProvider.GetService(typeof(DTE)) as DTE;
            var runningDocumentTable = new RunningDocumentTable(serviceProvider);
            var solutionService = _serviceProvider.GetService(typeof(IVsSolution)) as IVsSolution2;
            _debuggerEvents = dteService.Events.DebuggerEvents;
            _dte = dteService;
            _runningDocumentTable = runningDocumentTable;
            _solutionService = solutionService;
            IsDebugging = false;
            IsSolutionLoaded = false;
            IsNopCommerceSolution = false;
            observers = new List<IObserver<NopyCopyConfiguration>>();
            projectRootFolders = new Dictionary<string, string>();

            // Check if a solution is currently loaded
            if (_solutionService.IsSolutionLoaded())
            {
                // Check it the loaded solution is a nop commerce solution
                IsSolutionLoaded = true;
                IsNopCommerceSolution = IsStandardNopProject(_solutionService as IVsSolution);
            }
            else
            {
                IsSolutionLoaded = false;
                IsNopCommerceSolution = false;
            }

            // Listen for when debugging starts/ends
            AdviseDebugEvents();

            // Listen for when solution events occur
            AdviseSolutionEvents();

            // Init nopyCopyService
            // TODO: Get configuration from VS options
            Configuration = new NopyCopyConfiguration
            {
                ListedFileExtensions = new ObservableCollection<string>()
                {
                    ".cshtml",
                    ".html",
                    ".js",
                    ".css",
                    ".scss"
                },
                IsWhiteList = true,
                IsEnabled = true
            };

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
                    IsNopCommerceSolution = isNopCommerceSolution,
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
        public bool IsNopCommerceSolution
        {
            get => isNopCommerceSolution;
            private set
            {
                OnNopCommerceSolutionEvent?.Invoke(this, new NopCommerceSolutionEvent
                {
                    IsNopCommerceSolution = value,
                    SolutionLoaded = true
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

            //var document = FindDocument(docCookie);
            //var project = document.ProjectItem;

            if (IsNopCommerceSolution && ShouldCopy(document.FullName))
            {
                var pluginName = document.ProjectItem.ContainingProject.Name;
                var copyingTo = GetFilesCorrespondingWebPluginPath(document.FullName, pluginName);
                File.Copy(document.FullName, copyingTo, true);

                OnFileSavedEvent(this, new FileSavedEvent
                {
                    SavedFile = new FileInfo(document.FullName),
                    CopiedTo = new FileInfo(copyingTo)
                });
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
            IsNopCommerceSolution = IsStandardNopProject(_solutionService);
            return S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            IsNopCommerceSolution = IsStandardNopProject(_solutionService);
            return S_OK;
        }

        #endregion

        #region IVsSolutionEvents

        // TODO: On each project load/unload add to projectRootFolders dictionary
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            try
            {
                var project = pStubHierarchy.ToEnvProject();
                var systemName = pStubHierarchy.GetSystemNameFromDescription();

                if (!string.IsNullOrEmpty(systemName) 
                    && !projectRootFolders.ContainsKey(project.Name))
                {
                    projectRootFolders.Add(project.Name, systemName);
                }
            }
            catch(Exception e)
            {
                // TODO: Add some way of error handling
                Console.WriteLine(e);
            }

            return S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            try
            {
                var project = pStubHierarchy.ToEnvProject();
                var systemName = pStubHierarchy.GetSystemNameFromDescription();

                if (!string.IsNullOrEmpty(systemName))
                {
                    if (projectRootFolders.ContainsKey(project.Name))
                    {
                        projectRootFolders.Remove(project.Name);
                    }
                }
            }
            catch (Exception e)
            {
                // TODO: Add some way of error handling
                Console.WriteLine(e);
            }

            return S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            IsSolutionLoaded = true;
            IsNopCommerceSolution = IsStandardNopProject(_solutionService);

            var projects = _solutionService.GetProjects();
            foreach (var project in projects)
            {
                // Get the Plugins folder
                if (project.Name.ToLower() == "plugins")
                {
                    var fullName = project.FullName;

                    // Get all projects in this folder
                    foreach (ProjectItem pluginProject in project.ProjectItems)
                    {
                        // Check if the project is a plugin
                        if (pluginProject.TryGetSystemNameOfProjectItem(
                            out string systemName))
                        {
                            if (!projectRootFolders.ContainsKey(project.Name))
                            {
                                projectRootFolders.Add(project.Name, systemName);
                            }
                        }
                    }
                }
            }

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

            foreach (var key in projectRootFolders.Keys)
                projectRootFolders.Remove(key);

            return S_OK;
        }

        #endregion

        #region DebuggerEvents

        private void _debuggerEvents_OnEnterRunMode(dbgEventReason Reason)
        {
            IsDebugging = true;

            // If the solution is a NopCommerceSolution then begin listening 
            // for file changes
            isNopCommerceSolution = IsStandardNopProject(_solutionService);
            if (isNopCommerceSolution)
                AdviseRunningDocumentEvents();
        }

        private void _debuggerEvents_OnEnterDesignMode(dbgEventReason Reason)
        {
            IsDebugging = false;
            UnadviseDocumentEvents();
        }

        #endregion

        #endregion

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
                if (Configuration.ListedFileExtensions.Contains(
                    Path.GetExtension(item.Name)))
                {
                    whiteListedItems.Add(item);
                }
            }

            return whiteListedItems;
        }

        private bool ShouldCopy(string path)
        {
            var ext = Path.GetExtension(path);

            if (string.IsNullOrEmpty(ext))
            {
                return false;
            }

            var containsExtension = Configuration.ListedFileExtensions.Contains(ext);

            if (Configuration.IsWhiteList)
                return containsExtension;
            else
                return !containsExtension;
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
