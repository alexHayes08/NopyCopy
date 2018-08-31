using EnvDTE;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using NopyCopyV2.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static Microsoft.VisualStudio.VSConstants;

namespace NopyCopyV2.Services
{
    public class VSDKHelperService : Package, SVSDKHelperService, IVSDKHelperService
    {
        #region Fields

        private IVsEnumHierarchyItemsFactory _enumHierarchyItemsFactory;
        private IVsSolution _solutionService;

        #endregion

        #region Ctor(s)

        public VSDKHelperService()
        { }

        #endregion

        #region Properties

        #endregion

        #region Methods

        protected override void Initialize()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.Initialize();

            // Now retrieve all needed services
            _enumHierarchyItemsFactory = Package.GetGlobalService(typeof(SVsEnumHierarchyItemsFactory)) as IVsEnumHierarchyItemsFactory;
            _solutionService = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;
        }

        private IList<uint> GetProjectItems(IVsHierarchy pHier)
        {
            // Start with the project root and walk all expandable nodes in the project
            return GetProjectItems(pHier, VSITEMID_ROOT);
        }

        private IList<uint> GetProjectItems(IVsHierarchy pHier, uint startItemId)
        {
            List<uint> projectNodes = new List<uint>();

            // The method does a breadth-first traversal of the project's hierarchy tree
            Queue<uint> nodesToWalk = new Queue<uint>();
            nodesToWalk.Enqueue(startItemId);

            while (nodesToWalk.Count > 0)
            {
                uint node = nodesToWalk.Dequeue();
                projectNodes.Add(node);

                object property = null;
                if (pHier.GetProperty(node, (int)__VSHPROPID.VSHPROPID_FirstChild, out property) == S_OK)
                {
                    uint childnode = (uint)(int)property;
                    if (childnode == VSITEMID_NIL)
                    {
                        continue;
                    }

                    if ((pHier.GetProperty(childnode, (int)__VSHPROPID.VSHPROPID_Expandable, out property) == S_OK && (int)property != 0) ||
                        (pHier.GetProperty(childnode, (int)__VSHPROPID2.VSHPROPID_Container, out property) == S_OK && (bool)property))
                    {
                        nodesToWalk.Enqueue(childnode);
                    }
                    else
                    {
                        projectNodes.Add(childnode);
                    }

                    while (pHier.GetProperty(childnode, (int)__VSHPROPID.VSHPROPID_NextSibling, out property) == S_OK)
                    {
                        childnode = (uint)(int)property;
                        if (childnode == VSITEMID_NIL)
                        {
                            break;
                        }

                        if ((pHier.GetProperty(childnode, (int)__VSHPROPID.VSHPROPID_Expandable, out property) == S_OK && (int)property != 0) ||
                            (pHier.GetProperty(childnode, (int)__VSHPROPID2.VSHPROPID_Container, out property) == S_OK && (bool)property))
                        {
                            nodesToWalk.Enqueue(childnode);
                        }
                        else
                        {
                            projectNodes.Add(childnode);
                        }
                    }
                }
            }
            return projectNodes;
        }

        private IList<string> GetProjectFiles(IVsSccProject2 pscp2Project, uint startItemId)
        {
            IList<string> projectFiles = new List<string>();
            IVsHierarchy hierProject = pscp2Project as IVsHierarchy;
            IList<uint> projectItems = GetProjectItems(hierProject, startItemId);

            foreach (uint itemid in projectItems)
            {
                IList<string> sccFiles = GetNodeFiles(pscp2Project, itemid);
                foreach (string file in sccFiles)
                {
                    projectFiles.Add(file);
                }
            }

            return projectFiles;
        }

        /// <summary>
        /// Returns a list of files associated with each specified node.
        /// </summary>
        /// <param name="hier"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private IList<string> GetNodeFiles(IVsHierarchy hier, uint itemId)
        {
            IVsSccProject2 pscp2 = hier as IVsSccProject2;
            return GetNodeFiles(pscp2, itemId);
        }

        /// <summary>
        /// Returns a list of file associated with each specified node.
        /// </summary>
        /// <param name="pscp2"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        private IList<string> GetNodeFiles(IVsSccProject2 pscp2, uint itemId)
        {
            // NOTE: the function returns only a list of files, containing both regular files and special files
            // If you want to hide the special files (similar with solution explorer), you may need to return 
            // the special files in a hastable (key=master_file, values=special_file_list)

            // Initialize output parameters
            IList<string> sccFiles = new List<string>();
            if (pscp2 != null)
            {
                CALPOLESTR[] pathStr = new CALPOLESTR[1];
                CADWORD[] flags = new CADWORD[1];

                if (pscp2.GetSccFiles(itemId, pathStr, flags) == S_OK)
                {
                    for (int elemIndex = 0; elemIndex < pathStr[0].cElems; elemIndex++)
                    {
                        IntPtr pathIntPtr = Marshal.ReadIntPtr(pathStr[0].pElems, elemIndex * IntPtr.Size);
                        String path = Marshal.PtrToStringAuto(pathIntPtr);

                        sccFiles.Add(path);

                        // See if there are special files
                        if (flags.Length > 0 && flags[0].cElems > 0)
                        {
                            int flag = Marshal.ReadInt32(flags[0].pElems, elemIndex * IntPtr.Size);

                            if (flag != 0)
                            {
                                // We have special files
                                CALPOLESTR[] specialFiles = new CALPOLESTR[1];
                                CADWORD[] specialFlags = new CADWORD[1];

                                if (pscp2.GetSccSpecialFiles(itemId, path, specialFiles, specialFlags) == S_OK)
                                {
                                    for (int i = 0; i < specialFiles[0].cElems; i++)
                                    {
                                        IntPtr specialPathIntPtr = Marshal.ReadIntPtr(specialFiles[0].pElems, i * IntPtr.Size);
                                        String specialPath = Marshal.PtrToStringAuto(specialPathIntPtr);

                                        sccFiles.Add(specialPath);
                                        Marshal.FreeCoTaskMem(specialPathIntPtr);
                                    }

                                    if (specialFiles[0].cElems > 0)
                                    {
                                        Marshal.FreeCoTaskMem(specialFiles[0].pElems);
                                    }
                                }
                            }
                        }

                        Marshal.FreeCoTaskMem(pathIntPtr);
                    }
                    if (pathStr[0].cElems > 0)
                    {
                        Marshal.FreeCoTaskMem(pathStr[0].pElems);
                    }
                }
            }

            return sccFiles;
        }

        /// <summary>
        /// Returns the filename of the solution.
        /// </summary>
        /// <returns></returns>
        private string GetSolutionFileName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_solutionService.GetSolutionInfo(
                out string solutionDirectory,
                out string solutionFile,
                out string solutionUserOptions) == S_OK)
            {
                return solutionFile;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the name of the specified project.
        /// </summary>
        /// <param name="pscp2Project"></param>
        /// <returns></returns>
        private string GetProjectFileName(IVsSccProject2 pscp2Project)
        {
            // Note: Solution folders return currently a name like 
            // "NewFolder1{1DBFFC2F-6E27-465A-A16A-1AECEA0B2F7E}.storage"
            //
            // Your provider may consider returning the solution file as the 
            // project name for the solution, if it has to persist some 
            // properties in the "project file"
            //
            // UNDONE: What to return for web projects? They return a folder
            // name, not a filename! Consider returning a pseudo-project 
            // filename instead of folder.

            IVsHierarchy hierProject = (IVsHierarchy)pscp2Project;
            IVsProject project = (IVsProject)pscp2Project;

            // Attempt to get the filename controlled by the root node
            IList<string> sccFiles = GetNodeFiles(pscp2Project, VSITEMID_ROOT);
            if (sccFiles.Count > 0 && sccFiles[0] != null && sccFiles[0].Length > 0)
            {
                return sccFiles[0];
            }

            // If that failed, attempt to get a name from the IVsProject interface
            string bstrMKDocument;
            if (project.GetMkDocument(VSITEMID_ROOT, out bstrMKDocument) == S_OK
                && bstrMKDocument != null
                && bstrMKDocument.Length > 0)
            {
                return bstrMKDocument;
            }

            // If that fails, attempt to get the filename from the solution
            string uniqueName;
            if (_solutionService.GetUniqueNameOfProject(hierProject, out uniqueName) == S_OK
                && uniqueName != null && uniqueName.Length > 0)
            {
                return uniqueName;
            }

            // If that failed, attempt to get the project name from
            string bstrName;
            if (hierProject.GetCanonicalName(VSITEMID_ROOT, out bstrName) == S_OK)
            {
                return bstrName;
            }

            // If everything failed return null
            return null;
        }

        string IVSDKHelperService.GetSolutionFileName()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (GetGlobalService(typeof(SVsSolution)) is IVsSolution solution)
            {
                solution.GetSolutionInfo(out string solutionDir,
                    out string solutionFile,
                    out string userOptsFile);

                return null;
            }
            else
            {
                throw new Exception();
            }
        }

        public IEnumerable<Project> GetProjectsInSolution(bool includeNestedProjects)
        {
            if (GetGlobalService(typeof(SVsSolution)) is IVsSolution solution)
            {
                return solution.GetProjects();
            }
            else
            {
                throw new Exception();
            }
        }

        #endregion
    }
}
