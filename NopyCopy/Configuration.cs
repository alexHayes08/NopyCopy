using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NopyCopyV2
{
    internal class Configuration
    {
        #region Properties

        /// <summary>
        /// These file extensions (*.cshtml, *.js, etc...) will automatically 
        /// be copied when debugging
        /// </summary>
        public IList<string> WhiteListedFileExtensions { get; set; }

        #endregion
    }
}
