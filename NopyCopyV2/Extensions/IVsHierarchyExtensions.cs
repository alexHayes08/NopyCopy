using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static Microsoft.VisualStudio.VSConstants;

namespace NopyCopyV2.Extensions
{
    public static class IVsHierarchyExtensions
    {
        public static string GetProjectDirectory(this IVsHierarchy hierarchy)
        {
            string fullPath = null;

            hierarchy.GetProperty((uint)VSITEMID.Root,
                (int)__VSHPROPID.VSHPROPID_ProjectDir,
                out object objProp);

            if (objProp is string)
            {
                fullPath = objProp as string;
            }

            return fullPath;
        }

        public static EnvDTE.Project ToEnvProject(this IVsHierarchy hierarchy)
        {
            EnvDTE.Project project = null;

            hierarchy.GetProperty(VSITEMID_ROOT,
                (int)__VSHPROPID.VSHPROPID_ExtObject,
                out object objProp);

            if (objProp is EnvDTE.Project)
            {
                project = objProp as EnvDTE.Project;
            }

            return project;
        }

        /// <summary>
        /// Returns a list of files associated with the current node.
        /// </summary>
        /// <param name="hier"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static IList<string> GetNodeFiles(this IVsHierarchy hier, uint itemId)
        {
            IVsSccProject2 pscp2 = hier as IVsSccProject2;
            return pscp2.GetNodeFiles(itemId);
        }
    }

    public static class IVsSccProject2Extensions
    {
        /// <summary>
        /// Returns a list of files associated with the current node.
        /// </summary>
        /// <param name="hier"></param>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public static IList<string> GetNodeFiles(this IVsSccProject2 pscp2, uint itemId)
        {
            // NOTE: the function returns only a list of files, containing 
            // both regular files and special files. If you want to hide the
            // special files (similar with solution explorer), you may need to
            // return the special files in a hastable (key=master_file, 
            // values =special_file_list)

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
                        string path = Marshal.PtrToStringAuto(pathIntPtr);

                        sccFiles.Add(path);

                        // See if there are special files?
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
                                        string specialPath = Marshal.PtrToStringAuto(specialPathIntPtr);

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

        // https://github.com/Microsoft/VSSDK-Extensibility-Samples/blob/646de671c1a65ca49e9fce397baefe217e9123e8/Source_Code_Control_Provider/C%23/SccProvider.cs

        /// <summary>
        /// Gets the filename of the specified project
        /// </summary>
        /// <param name="pscp2Project"></param>
        /// <returns></returns>
        //public static string GetProjectFileName(this IVsSccProject2 pscp2Project)
        //{
        //    // Note: Solution folders return currently a name like 
        //    // "NewFolder1{1DBFFC2F-6E27-465A-A16A-1AECEA0B2F7E}.storage"
        //    //
        //    // Your provider may consider returning the solution file as the 
        //    // project name for the solution, if it has to persist some 
        //    // properties in the "project file"
        //    //
        //    // UNDONE: What to return for web projects? They return a folder
        //    // name, not a filename! Consider returning a pseudo-project 
        //    // filename instead of folder.

        //    IVsHierarchy hierProject = (IVsHierarchy)pscp2Project;
        //    IVsProject project = (IVsProject)pscp2Project;

        //    // Attempt to get the filename controlled by the root node
        //    IList<string> sccFiles = GetNodeFiles(pscp2Project, VSITEMID_ROOT);
        //    if (sccFiles.Count > 0 && sccFiles[0] != null && sccFiles[0].Length > 0)
        //    {
        //        return sccFiles[0];
        //    }

        //    // If that failed, attempt to get a name from the IVsProject interface
        //    string bstrMKDocument;
        //    if (project.GetMkDocument(VSITEMID_ROOT, out bstrMKDocument) == S_OK
        //        && bstrMKDocument != null 
        //        && bstrMKDocument.Length > 0)
        //    {
        //        return bstrMKDocument;
        //    }

        //    // If that fails, attempt to get the filename from the solution
        //    //IVsSolution = (IVsSolution)GetService(typeof(SVsSolution));
        //}
    }
}
