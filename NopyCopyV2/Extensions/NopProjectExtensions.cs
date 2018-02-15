using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;

namespace NopyCopyV2.Extensions
{
    internal static class NopProjectExtensions
    {
        const string PluginProjectsPrefix = @"Plugins\";
        const string PluginsProjectType = "2150E333-8FDC-42A3-9474-1A3956D46DE8";

        /// <summary>
        /// Checks to see if the solution projects includes the standard 
        /// NopCommerce projects.
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        public static bool IsStandardNopProject(IVsSolution solution)
        {
            var projects = solution.GetProjects();

            const string LibrariesName = "Libraries";
            const string LibrariesFullName = "Libraries";
            bool foundLibraries = false;

            const string PresentationName = "Presentation";
            const string PresentationFullName = "Presentation";
            bool foundPresentation = false;

            const string TestsName = "Tests";
            const string TestsFullName = "Tests";
            bool foundTests = false;

            const string PluginsName = "Plugins";
            const string PluginsFullName = "Plugins";
            bool foundPlugins = false;

            const string Nop_WebName = "Nop.Web";
            const string Nop_WebFullName = @"Presentation\Nop.Web\Nop.Web.csproj";
            bool foundNop_Web = false;

            const string Nop_CoreName = "Nop.Core";
            const string Nop_CoreFullName = @"Libraries\Nop.Data\Nop.Data.csproj";
            bool foundNop_Core = false;

            const string Nop_ServicesName = "Nop.Services";
            const string Nop_ServicesFullName = @"Libraries\Nop.Services\Nop.Services.csproj";
            bool foundNop_Services = false;

            foreach (var project in projects)
            {
                var test_a = project.ExtenderNames;
                var test_b = project.FileName;
                var test_c = project.FullName;
                var test_d = project.UniqueName;
                var test_e = project.Kind;
                var test_f = project.Globals;
                var test_g = project.ParentProjectItem;
                var test_h = project.Object;
                switch (project.Name)
                {
                    case LibrariesName:
                        //foundLibraries = project.FullName == LibrariesFullName;
                        foundLibraries = true;
                        break;

                    case PresentationName:
                        //foundPresentation = project.FullName == PresentationFullName;
                        foundPresentation = true;
                        break;

                    case TestsName:
                        //foundTests = project.FullName == TestsFullName;
                        foundTests = true;
                        break;

                    case PluginsName:
                        //foundPlugins = project.FullName == PluginsFullName;
                        foundPlugins = true;
                        break;

                    case Nop_WebName:
                        //foundNop_Web = project.FullName == Nop_WebFullName;
                        foundNop_Web = true;
                        break;

                    case Nop_CoreName:
                        //foundNop_Core = project.FullName == Nop_CoreFullName;
                        foundNop_Core = true;
                        break;

                    case Nop_ServicesName:
                        //foundNop_Services = project.FullName == Nop_ServicesFullName;
                        foundNop_Services = true;
                        break;
                }

                // Stop checking the projects if we've found all of them
                if (foundLibraries
                    && foundPresentation
                    && foundPlugins
                    && foundTests
                    && foundNop_Web
                    && foundNop_Core
                    && foundNop_Services)
                {
                    return true;
                }
            }

            // If we have reached here then we didn't find all of the projects
            return false;
        }

        public static IList<Project> GetAllPlugins(IVsSolution solution)
        {
            var pluginProjects = new List<Project>();

            foreach (Project project in solution.GetProjects())
            {
                if (project.FullName.StartsWith(PluginProjectsPrefix))
                {
                    pluginProjects.Add(project);
                }
            }

            return pluginProjects;
        }

        public static bool IsFilePathInPlugin(string fullFilePath, string solutionPath)
        {
            return fullFilePath.Contains("Plugins");
        }

        public static string GetFilesCorrespondingWebPluginPath(string fullFilePath)
        {
            var pathSuffix = Path.Combine("Presentation", "Nop.Web", "Plugins");
            var fileInfo = new FileInfo(fullFilePath);
            var splitName = fullFilePath.Split(new string[] { "Plugins" }, StringSplitOptions.RemoveEmptyEntries);

            if (splitName.Length < 2)
            {
                throw new Exception("File wasn't in a folder named plugins");
            }

            var pluginName = Path.Combine(splitName[0], pathSuffix, splitName[1]);

            return pluginName;
        }
    }
}
