using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Modals;
using NopyCopyV2.Services;
using System;
using System.Collections.Generic;
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
        private IDictionary<string, string> projectRootFolders;

        // Services
        private NopyCopyConfiguration configuration;
        private readonly Microsoft.VisualStudio.OLE.Interop.IServiceProvider _serviceProvider;
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

        public NopyCopyService(Microsoft.VisualStudio.OLE.Interop.IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public NopyCopyService(NopyCopyConfiguration configuration,
            RunningDocumentTable runningDocumentTable,
            DTE dte,
            IVsSolution solutionService)
        {
            // Init fields
            this.configuration = configuration;
            _debuggerEvents = dte.Events.DebuggerEvents;
            _dte = dte;
            _runningDocumentTable = runningDocumentTable;
            _solutionService = solutionService as IVsSolution2;
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
        public NopyCopyConfiguration Configuration
        {
            get => configuration;
            set
            {
                configuration = value;
                OnConfigUpdatedEvent?.Invoke(this, 
                    new ConfigUpdatedEvent(Configuration));
            }
        }
        public string SolutionName => _dte.Solution?.FileName;

        #endregion

        #region Events

        public event EventHandler<DebugEvent> OnDebugEvent;
        public event EventHandler<ConfigUpdatedEvent> OnConfigUpdatedEvent;
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
            // Return if not disabled or if not debugging
            if (!configuration.IsEnabled || !IsDebugging)
                return S_OK;

            var document = FindDocument(docCookie);

            if (IsNopCommerceSolution && ShouldCopy(document.FullName))
            {
                var copyingTo = GetFilesCorrespondingWebPluginPath(document.FullName, "FIXME");
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

                if (!string.IsNullOrEmpty(systemName))
                    projectRootFolders.Add(project.Name, systemName);
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
                if (configuration.ListedFileExtensions.Contains(
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

            var containsExtension = configuration.ListedFileExtensions.Contains(ext);

            if (configuration.IsWhiteList)
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

        public IDisposable Subscribe(IObserver<NopyCopyConfiguration> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);

            return new Unsubscriber(observers, observer);
        }

        #endregion

        #region Nested Class Unsubscriber

        private class Unsubscriber : IDisposable
        {
            private IList<IObserver<NopyCopyConfiguration>> _observers;
            private IObserver<NopyCopyConfiguration> _observer;

            public Unsubscriber(IList<IObserver<NopyCopyConfiguration>> observers, IObserver<NopyCopyConfiguration> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }

        #endregion
    }
}
