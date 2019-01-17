using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Modals;
using System;
using System.ComponentModel;

namespace NopyCopyV2.Services
{
    /// <summary>
    /// The service used to copy files that are marked with 'Copy to output
    /// directory' as 'Copy if newer' or 'Copy always'.
    /// </summary>
    /// <see cref="https://docs.microsoft.com/en-us/visualstudio/extensibility/how-to-provide-a-service"/>
    public interface INopyCopyService : 
        IVsRunningDocTableEvents3, 
        IVsSolutionEvents,
        INotifyPropertyChanged
    {
        bool IsSolutionLoaded { get; }
        bool IsDebugging { get; }
        string SolutionName { get; }
        NopyCopyConfiguration Configuration { get; set; }
        event EventHandler<DebugEvent> OnDebugEvent;
        event EventHandler<SolutionEvent> OnSolutionEvent;
        event EventHandler<FileSavedEvent> OnFileSavedEvent;
    }
}
