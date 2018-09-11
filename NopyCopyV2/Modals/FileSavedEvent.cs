using System;
using System.IO;

namespace NopyCopyV2.Modals
{
    public class FileSavedEvent : EventArgs
    {
        public FileInfo SavedFile { get; set; }
        public FileInfo CopiedTo { get; set; }
        public string Reason { get; set; }

        public bool HasError => !String.IsNullOrEmpty(Reason);
    }
}
