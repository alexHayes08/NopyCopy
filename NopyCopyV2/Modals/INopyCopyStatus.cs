namespace NopyCopyV2.Modals
{
    public interface INopyCopyStatus
    {
        bool SolutionLoaded { get; }
        bool IsDebugging { get; }
        string SolutionName { get; }
        NopyCopyConfiguration Configuration { get; }
    }
}
