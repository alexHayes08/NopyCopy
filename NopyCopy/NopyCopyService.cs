using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static NopyCopyV2.Extensions.IVsSolutionExtensions;
using static NopyCopyV2.Extensions.NopProjectExtensions;

namespace NopyCopyV2
{
    internal class IsBusyEventArgs : EventArgs
    {
        public bool IsBusy { get; set; }
    }

    internal class FileCopyingEventArgs : EventArgs
    {
        bool FinishedCopy { get; set; }
        public string CopiedFileOldPath { get; set; }
        public string CopiedFileNewPath { get; set; }
    }

    internal class NopyCopyService : IDisposable
    {
        #region Fields

        private readonly Configuration configuration;
        private readonly Solution _solutionService;
        private readonly SolutionEvents _solutionEventsService;
        private readonly IVsSccProjectEvents _projectEventsService;

        #endregion

        #region Constructors

        public NopyCopyService(Configuration configuration,
            Solution solutionService,
            SolutionEvents solutionEventsService,
            IVsSccProjectEvents projectEventsService)
        {
            this.configuration = configuration;
            _solutionService = solutionService;
            _solutionEventsService = solutionEventsService;
            _projectEventsService = projectEventsService;

            // Init properties
            IsSolutionLoaded = false;
            IsNopCommerceSolution = false;
            IsCurrentlyBusy = false;
            MontitoredProjects = new List<Project>();
            FileWatchers = new List<FileSystemWatcher>();

            // Add event listeners
            _solutionEventsService.Opened += OnSolutionLoaded;
            _solutionEventsService.BeforeClosing += OnSolutionUnloaded;
            _solutionEventsService.ProjectAdded += OnProjectAdded;
            _solutionEventsService.ProjectRemoved += OnProjectRemoved;
            _solutionEventsService.ProjectRenamed += OnProjectRenamed;
        }

        #endregion

        #region Finalizer

        ~NopyCopyService()
        {
            _solutionEventsService.Opened -= OnSolutionLoaded;
            _solutionEventsService.BeforeClosing -= OnSolutionUnloaded;
            _solutionEventsService.ProjectAdded -= OnProjectAdded;
            _solutionEventsService.ProjectRemoved -= OnProjectedRemoved;
        }

        #endregion

        #region Properties

        private IList<Project> MontitoredProjects { get; set; }
        private IList<FileSystemWatcher> FileWatchers { get; set; }
        private bool IsCurrentlyBusy { get; set; }
        private bool IsNopCommerceSolution { get; set; }
        private bool IsSolutionLoaded { get; set; }

        #endregion

        #region Events

        public event EventHandler<IsBusyEventArgs> IsBusy;
        public event EventHandler<FileCopyingEventArgs> CopyingFiles;

        #endregion

        #region EventHandlers

        protected virtual void OnIsBusy(IsBusyEventArgs e)
        {
            EventHandler<IsBusyEventArgs> handler = IsBusy;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnCopyingFiles(FileCopyingEventArgs e)
        {
            EventHandler<FileCopyingEventArgs> handler = CopyingFiles;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnSolutionLoaded()
        {
            IsSolutionLoaded = true;
            var pluginsProjects = GetAllPlugins(_solutionService);
        }

        protected virtual void OnSolutionUnloaded()
        {
            IsSolutionLoaded = false;
            IsNopCommerceSolution = false;
        }

        protected virtual void OnProjectAdded(Project e)
        {
            
        }

        protected virtual void OnProjectRemoved(Project e)
        {

        }

        protected virtual void OnProjectRenamed(Project e, string OldName)
        {

        }

        protected virtual int OnProjectOnLoaded(EventArgs e)
        {
            return VSConstants.S_OK;
        }

        protected virtual int OnDebug(EventArgs e)
        {
            return VSConstants.S_OK;
        }

        protected virtual int OnDebugStop(EventArgs e)
        {
            return VSConstants.S_OK;
        }

        private void NopyCopyEventHandler(object sender, FileSystemEventArgs e)
        {
            if (!ShouldCopy(e.FullPath))
            {
                return;
            }

            var baseStr = "[" + DateTime.Now + ": " +
                          e.ChangeType + " " + e.FullPath + "]";

            var item = MonitoredDirectories.FirstOrDefault(q => e.FullPath.ToLower().Contains(q.From));

            if (item == null)
            {
                Console.WriteLine(baseStr + ": couldn't find matching item?!");
                return;
            }

            var relativePath = e.FullPath.ToLower().Replace(item.From, string.Empty);
            var copyTo = item.To + relativePath;// Path.Combine(item.To, relativePath);

            try
            {
                File.SetAttributes(e.FullPath, FileAttributes.Normal);
                // File.SetAttributes(copyTo, FileAttributes.Normal);

                File.Copy(e.FullPath, copyTo, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("{0} || File copied! From {1} to {2}", baseStr, item.From, item.To);
        }

        #endregion

        #region Functions

        private IEnumerable<ProjectItem> GetWhiteListedItems(Project plugin)
        {
            var whiteListedItems = new List<ProjectItem>();

            foreach (ProjectItem item in plugin.ProjectItems)
            {
                if (configuration.WhiteListedFileExtensions.Contains(
                    Path.GetExtension(item.Name)))
                {
                    whiteListedItems.Add(item);
                }
            }

            return whiteListedItems;
        }

        private void AddWatcher(string rootDirectory)
        {
            var watcher = new FileSystemWatcher(rootDirectory)
            {
                IncludeSubdirectories = true,
                Filter = ""
            };

            watcher.Changed += NopyCopyEventHandler;
            watcher.Created += Created;
            watcher.Deleted += Deleted;
            watcher.Renamed += NopyCopyEventHandler;
            watcher.EnableRaisingEvents = true;
        }

        private bool ShouldCopy(string path)
        {
            var ext = Path.GetExtension(path);

            if (string.IsNullOrEmpty(ext))
            {
                return false;
            }

            switch (ext.ToLower())
            {
                case ".cshtml":
                case ".js":
                case ".css":
                    return true;
                default:
                    return false;
            }
        }


        private void Deleted(object sender, FileSystemEventArgs e)
        {
            if (!ShouldCopy(e.FullPath))
            {
                return;
            }

            Console.WriteLine("Delete event occurred: path:{0}, changeType: {1}", e.FullPath, e.ChangeType);
        }

        private void Created(object sender, FileSystemEventArgs e)
        {
            if (!ShouldCopy(e.FullPath))
            {
                return;
            }

            Console.WriteLine("Create event occurred: path:{0}, changeType: {1}", e.FullPath, e.ChangeType);
        }

        private void RemoveAllFileWatchers()
        {
            foreach (var fileWatcher in FileWatchers)
            {
                fileWatcher.Changed -= NopyCopyEventHandler;
            }
        }

        private void AddFileWatcherToPlugin(Project project)
        {
            var fileWatcher = new FileSystemWatcher(project.FullName);

            FileWatchers.Add(fileWatcher);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~NopyCopyService() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        #endregion
    }
}
