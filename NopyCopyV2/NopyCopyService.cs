using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Modals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Microsoft.VisualStudio.VSConstants;
using static NopyCopyV2.Extensions.IVsSolutionExtensions;
using static NopyCopyV2.Extensions.NopProjectExtensions;

namespace NopyCopyV2
{
    public class NopyCopyService : IVsRunningDocTableEvents3, IVsSolutionEvents
    {
        #region Fields

        // Services
        private readonly NopyCopyConfiguration configuration;
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
            UnadviseDebugEvents();
            UnadviseSolutionEvents();
        }

        #endregion

        #region Properties

        public bool IsNopCommerceSolution { get; private set; }
        private bool IsSolutionLoaded { get; set; }
        private bool IsDebugging { get; set; }

        #endregion

        #region Events

        public event EventHandler<DebugEvent> OnDebugEvent;
        public event EventHandler<EnableToggledEvent> OnEnableToggledEvent;
        public event EventHandler<NopCommerceSolutionEvent> OnNopCommerceSolutionEvent;
        public event EventHandler<FileSavedEvent> OnFileSavedEvent;

        #endregion

        #region Functions

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
            OnFileSaved(document.FullName);

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
            OnSolutionOpened();
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
            OnSolutionClosed();
            return S_OK;
        }

        #endregion

        #region DebuggerEvents

        private void _debuggerEvents_OnEnterRunMode(dbgEventReason Reason)
        {
            IsDebugging = true;

            // If the solution is a NopCommerceSolution then begin listening 
            // for file changes
            if (IsNopCommerceSolution)
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
                _solutionService.UnadviseSolutionEvents(solutionEventsCookie.Value);
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

        private void OnSolutionOpened()
        {
            IsSolutionLoaded = true;
            IsNopCommerceSolution = IsStandardNopProject(_solutionService);
        }

        private void OnSolutionClosed()
        {
            IsSolutionLoaded = false;
            IsNopCommerceSolution = false;
        }

        private void OnFileSaved(string pathToFile)
        {
            // Return if not disabled or if not debugging
            if (!configuration.IsEnabled || !IsDebugging)
                return;

            if (IsNopCommerceSolution && ShouldCopy(pathToFile))
            {
                Console.WriteLine(pathToFile);
                var copyingTo = GetFilesCorrespondingWebPluginPath(pathToFile);

                OnFileSavedEvent(this, new FileSavedEvent
                {
                    SavedFile = new FileInfo(pathToFile),
                    CopiedTo = new FileInfo(copyingTo)
                });
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

        #endregion
    }
}
