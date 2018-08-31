using EnvDTE;
using System.Collections.Generic;

namespace NopyCopyV2.Services
{
    public interface IVSDKHelperService
    {
        string GetSolutionFileName();
        IEnumerable<Project> GetProjectsInSolution(bool includeNestedProjects);
    }
}
