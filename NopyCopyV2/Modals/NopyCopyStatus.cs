namespace NopyCopyV2.Modals
{
    public class NopyCopyStatus : INopyCopyStatus
    {
        public bool IsNopCommerceSolution { get; set; }
        public bool SolutionLoaded { get; set; }
        public bool IsDebugging { get; set; }
        public string SolutionName { get; set; }
        public NopyCopyConfiguration Configuration { get; set; }
    }
}
