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

        public override string ToString()
        {
            if (HasError)
            {
                return "Didn't copy file from: " +
                    $"'{SavedFile?.FullName ?? ""}' " +
                    $"to:'{CopiedTo?.FullName ?? ""}' " +
                    $"because: '{Reason}'.";
            }
            else
            {
                return "Copied file from: " +
                    $"'{SavedFile?.FullName ?? ""}' " +
                    $"to:'{CopiedTo?.FullName ?? ""}'";
            }
        }
    }
}
