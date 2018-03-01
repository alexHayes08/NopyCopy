using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.IO;
using System.Linq;
using static Microsoft.VisualStudio.VSConstants;

namespace NopyCopyV2.Extensions
{
    public static class IVsHierarchyExtensions
    {
        public static string GetSystemNameFromDescription(this IVsHierarchy hierarchy)
        {
            string systemName = null;
            var project = hierarchy.ToEnvProject();
            const string SYSTEMNAME = "SystemName: ";

            for(var i = 0; i < project.ProjectItems.Count; i++)
            {
                var item = project.ProjectItems.Item(i);
                if (item.Name == "Description.txt")
                {
                    var lines = File.ReadAllLines(item.Document.FullName);
                    systemName = lines
                        .FirstOrDefault(l => l.StartsWith(SYSTEMNAME))
                        .Replace(SYSTEMNAME, "");

                    break;
                }
            }

            return systemName;
        }

        public static EnvDTE.Project ToEnvProject(this IVsHierarchy hierarchy)
        {
            EnvDTE.Project project = null;

            hierarchy.GetProperty(VSITEMID_ROOT,
                (int)__VSHPROPID.VSHPROPID_ExtObject,
                out object objProp);

            hierarchy.GetProperty(VSITEMID_ROOT, 
                (int)__VSHPROPID.VSHPROPID_ExtObject,
                out object objProp2);

            if (objProp is EnvDTE.Project)
            {
                project = objProp as EnvDTE.Project;
            }

            return project;
        }
    }
}
