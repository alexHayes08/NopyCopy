namespace NopyCopyV2.Modals
{
    /// <summary>
    /// Used to determine which files to target for the Override
    /// </summary>
    public enum OverrideType
    {
        /// <summary>
        /// Will only override a file whose path exactly matches this path.
        /// </summary>
        AbsolutePath,

        /// <summary>
        /// Will only override a file whose path matches this path (relative
        /// to the solution folder).
        /// </summary>
        RelativePath,

        /// <summary>
        /// Will override all files whose relative paths match the expression.
        /// </summary>
        Regex
    }

    /// <summary>
    /// Used to override the destination of a file
    /// </summary>
    public class Override
    {
        /// <summary>
        /// How to interperet the 'Target' property.
        /// </summary>
        public OverrideType Type { get; set; }

        /// <summary>
        /// Whether or not to still copy the file to it's original destination
        /// as well as to its new destination.
        /// </summary>
        public bool CopyToOriginalDestination { get; set; }

        /// <summary>
        /// A string which is either an absolute path, relative path (relative
        /// to the solution folder), or a regex expression (also relative to
        /// the solution folder).
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// A string which is a relative path to the solution folder. A file 
        /// name is optional.
        /// </summary>
        public string Destination { get; set; }
    }
}