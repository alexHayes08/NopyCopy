using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Modals;

namespace NopyCopyV2.Services
{
    /// <summary>
    ///     I think this is the interface that describes the service interface?
    /// </summary>
    /// <remarks>
    ///     According to the docs all VS services need two interfaces, one that 
    ///     describes the service and one that describes the service interface?
    /// </remarks>
    /// <see cref="https://docs.microsoft.com/en-us/visualstudio/extensibility/how-to-provide-a-service"/>
    public interface INopyCopyService : 
        IVsRunningDocTableEvents3, 
        IVsSolutionEvents
    {
        bool IsSolutionLoaded { get; }
        bool IsDebugging { get; }
        string SolutionName { get; }
        NopyCopyConfiguration Configuration { get; set; }
    }
}
