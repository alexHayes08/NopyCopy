using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NopyCopyV2.Extensions
{
    internal static class NopProjectExtensions
    {
        const string DESCRIPTION_NAME = "Description.txt";
        const string PLUGIN_PROJECT_PREFIX = @"Plugins\";
        const string PLUGIN_PROJECT_REGEX = @"\\Plugins\\";
        const string PLUGIN_PROJECT_TYPE = "2150E333-8FDC-42A3-9474-1A3956D46DE8";
        const string SYSTEM_NAME_PREFIX = "SystemName: ";

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
            //const string LibrariesFullName = "Libraries";
            bool foundLibraries = false;

            const string PresentationName = "Presentation";
            //const string PresentationFullName = "Presentation";
            bool foundPresentation = false;

            const string TestsName = "Tests";
            //const string TestsFullName = "Tests";
            bool foundTests = false;

            const string PluginsName = "Plugins";
            //const string PluginsFullName = "Plugins";
            bool foundPlugins = false;

            const string Nop_WebName = "Nop.Web";
            //const string Nop_WebFullName = @"Presentation\Nop.Web\Nop.Web.csproj";
            bool foundNop_Web = false;

            const string Nop_CoreName = "Nop.Core";
            //const string Nop_CoreFullName = @"Libraries\Nop.Data\Nop.Data.csproj";
            bool foundNop_Core = false;

            const string Nop_ServicesName = "Nop.Services";
            //const string Nop_ServicesFullName = @"Libraries\Nop.Services\Nop.Services.csproj";
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
                if (project.FullName.StartsWith(PLUGIN_PROJECT_PREFIX))
                {
                    pluginProjects.Add(project);
                }
            }

            return pluginProjects;
        }

        /// <summary>
        ///     Tries to get the 'SystemName' property from the description.txt
        ///     file.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="systemName"></param>
        /// <returns></returns>
        public static bool TryGetSystemNameFromDocument(this Document document,
            out string systemName)
        {
            var foundSystemName = false;
            systemName = null;

            do
            {
                var containingProject = document?.ProjectItem?.ContainingProject;

                if (containingProject == null)
                    break;

                // Test if the project is in the plugins folder
                if (!Regex.IsMatch(containingProject.FullName, PLUGIN_PROJECT_REGEX))
                    break;

                // Iterate over all items
                foreach (ProjectItem projItem in containingProject.ProjectItems)
                {
                    if (projItem.Name.ToLowerInvariant() == "description.txt")
                    {
                        // Found the file
                        var fullFileName = projItem.FileNames[0];
                        var textContent = File.ReadAllLines(fullFileName);

                        // Locate system name in the text file
                        systemName = textContent
                            .First(line => line.StartsWith(SYSTEM_NAME_PREFIX))
                            .Replace(SYSTEM_NAME_PREFIX, "")
                            .Trim();

                        foundSystemName = true;
                        break;
                    }
                }
            } while (false);

            return foundSystemName;
        }

        /// <summary>
        /// The project item must be a Project!
        /// </summary>
        /// <param name="projectItem"></param>
        /// <param name="systemName"></param>
        /// <returns></returns>
        public static bool TryGetSystemNameOfProjectItem(
            this ProjectItem projectItem, 
            out string systemName)
        {
            systemName = null;

            //var currentProjectRoot = projectItem.Docum
            var dte = projectItem.DTE;
            var activeProjects = dte.ActiveSolutionProjects as Project[];

            foreach (var item in projectItem.ProjectItems)
            {
                if (item is ProjectItem)
                {
                    var pi_2 = item as ProjectItem;
                }
                else if (item is Document)
                {
                    var d = item as Document;
                }
            }

            // Return if there are no ProjectItems
            if (projectItem.ProjectItems == null)
                return false;

            ProjectItem descFile = null;

            for (var i = 0; i < projectItem.ProjectItems.Count; i++)
            {
                var currentItem = projectItem.ProjectItems.Item(i);
                if (currentItem is ProjectItem)
                {
                    var projItem = currentItem as ProjectItem;
                    if (projItem.Name.ToLower() == DESCRIPTION_NAME)
                    {
                        descFile = projItem;
                        break;
                    }
                }
            }

            if (descFile != null)
            {
                // Check if path for the file is valid
                var path = projectItem.Document.FullName;
                if (File.Exists(path))
                {
                    var lines = File.ReadAllLines(path);
                    systemName = lines
                        .FirstOrDefault(line => line.StartsWith(SYSTEM_NAME_PREFIX));

                    if (!string.IsNullOrEmpty(systemName))
                    {
                        systemName = systemName
                            .Replace(SYSTEM_NAME_PREFIX, "")
                            .Trim();
                    }
                }
            }

            return !string.IsNullOrEmpty(systemName);
        }

        /// <summary>
        ///     Warning: If the plugin has an invalid Description.txt then
        ///     systemName will be set to null.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="systemName"></param>
        /// <returns></returns>
        public static bool TryGetSystemNameOfProject(this Project project, out string systemName)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            systemName = null;

            foreach (ProjectItem item in project.ProjectItems)
            {
                if (item.Name == DESCRIPTION_NAME)
                {
                    // Check if path for the file is valid
                    var path = item.Document.FullName;
                    if (File.Exists(path))
                    {
                        var lines = File.ReadAllLines(path);
                        systemName = lines
                            .FirstOrDefault(l => l.StartsWith(SYSTEM_NAME_PREFIX));

                        // Check that systemName was found
                        if (!string.IsNullOrEmpty(systemName))
                        {
                            systemName = systemName
                                .Replace(SYSTEM_NAME_PREFIX, "")
                                .Trim();
                        }
                    }
                }
            }

            return !string.IsNullOrEmpty(systemName);
        }

        /// <summary>
        ///     Searches all project items for the first file whose filename
        ///     matches the argument. Will return null if no file found.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static async Task<FileInfo> TryGetFileInfoAsync(this Project project, string fileName)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            FileInfo fileInfo = null;

            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                var name = projectItem.Name;
                if (0 == string.Compare(name,
                    fileName,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    fileInfo = new FileInfo(projectItem.Document.FullName);
                    break;
                }
            }

            return fileInfo;
        }

        /// <summary>
        ///     WIP: Update summary
        ///     FIXME: This function doesn't work, need to pass in the output 
        ///     projectName name as well!
        ///     Gets the output path of a file.
        /// </summary>
        /// <param name="fullFilePath"></param>
        /// <param name="projectName">
        ///     This is just the name of the output project folder found in the
        ///     descriptions folder.
        /// </param>
        /// <returns></returns>
        /// <exception cref="FileNotFoundException">
        ///     When the fullFilePath points to a non-existant file.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///     Thrown when the fullFilePath doesn't contain either the
        ///     projectName or a folder named 'plugins' (case insensitive).
        /// </exception>
        public static string GetFilesCorrespondingWebPluginPath(
            string fullFilePath, 
            string projectName, 
            string projectSystemName)
        {
            var fileInfo = new FileInfo(fullFilePath);
            var replacementPartialPath = Path.Combine("Presentation",
                "Nop.Web", 
                "Plugins", 
                projectSystemName);

            // Verify the file exists, contains a plugins folder, and contains
            // the project name in the path.
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException();
            }
            else if (!fullFilePath.ToLowerInvariant().Contains("plugins"))
            {
                throw new InvalidOperationException("Can only process file " +
                    "paths that contain a 'Plugins' folder.");
            }
            else if (!fullFilePath.Contains(projectName))
            {
                throw new InvalidOperationException("The 'projectName' " +
                    "wasn't a part of the fullFilePath.");
            }

            // Assert that plugins starts with an uppercase letter
            fullFilePath = fullFilePath.Replace("plugins", "Plugins");

            return fullFilePath.Replace($"Plugins{Path.DirectorySeparatorChar}{projectName}", replacementPartialPath);
        }

        /// <summary>
        ///     Retrieves the path where output files are put when building. This
        ///     will reflect the active solution configuration (debug/release).
        /// </summary>
        /// <param name="project"></param>
        /// <param name="outputPath"></param>
        /// <returns></returns>
        public static bool TryGetOutputPath(this Project project, out string outputPath)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                project.Properties.TryGetProperty("FullPath",
                    out string fullPath);
                project.ConfigurationManager.ActiveConfiguration.Properties
                    .TryGetProperty("OutputPath", out string outputDir);

                outputPath = Path.Combine(fullPath, outputDir);
                return true;
            }
            catch (Exception)
            {
                outputPath = null;
                return false;
            }
        }

        public static bool TryGetProperty<T>(this EnvDTE.Properties properties,
            string propertyName,
            out T value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            value = default(T);
            var foundProperty = false;

            for (var i = 1; i < properties.Count; i++)
            {
                try
                {
                    Property p = properties.Item(i);
                    if (0 == String.Compare(p.Name,
                        propertyName,
                        StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (p.Value is T casted)
                        {
                            foundProperty = true;
                            value = casted;
                        }

                        break;
                    }
                }
                catch
                { }
            }

            return foundProperty;
        }

        public static Dictionary<string, object> CastToDictionary(this EnvDTE.Properties properties)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var dictionary = new Dictionary<string, object>();

            // Properties is not 0-based, it starts at 1.
            for (int i = 1; i < properties.Count; i++)
            {
                try
                {
                    var propObj = properties.Item(i);
                    Property prop = properties.Item(i);
                    string name = null;
                    object value = null;
                    try
                    {
                        name = prop.Name;
                    }
                    catch
                    {
                        // Skip if we can't get the name.
                        continue;
                    }

                    try
                    {
                        value = prop.Value;
                        var valType = prop?.Value?.GetType();
                    }
                    catch
                    { }

                    dictionary[name] = value;
                }
                catch
                {
                    continue;
                }
            }

            return dictionary;
        }

        /// <summary>
        ///     Checks if an item has the property 'CopyToOutput' is to 'Always' or
        ///     'PreserveNewest'. Returns false if not set or set to 'Never'.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool ItemHasCopiedToOutputPropertyAsTrue(ProjectItem item)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (item.Properties.TryGetProperty("CopyToOutputDirectory",
                out uint value))
            {
                return value != 0;
            }
            else
            {
                return false;
            }
        }
    }
}
