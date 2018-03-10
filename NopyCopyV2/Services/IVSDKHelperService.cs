using EnvDTE;

namespace NopyCopyV2.Services
{
    public interface IVSDKHelperService
    {
        string GetSolutionFileName();
        Project GetProjectsInSolution(bool includeNestedProjects);
    }
}
