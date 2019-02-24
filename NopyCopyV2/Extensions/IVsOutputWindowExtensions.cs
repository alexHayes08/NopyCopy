using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace NopyCopyV2.Extensions
{
    public static class IVsOutputWindowExtensions
    {
        /// <summary>
        /// Creates the pane.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="paneGuid">The pane unique identifier.</param>
        /// <param name="title">The title.</param>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        /// <param name="clearWithSolution">if set to <c>true</c> [clear with solution].</param>
        /// <returns></returns>
        public static IVsOutputWindowPane CreatePaneHelper(
            this IVsOutputWindow output,
            Guid paneGuid,
            string title,
            bool visible,
            bool clearWithSolution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            IVsOutputWindowPane pane;

            // Create a new pane.
            output.CreatePane(
                ref paneGuid,
                title,
                Convert.ToInt32(visible),
                Convert.ToInt32(clearWithSolution));

            // Retrieve the new pane.
            output.GetPane(ref paneGuid, out pane);

            pane.OutputString($"Initialized {title} \n");

            return pane;
        }
    }
}
