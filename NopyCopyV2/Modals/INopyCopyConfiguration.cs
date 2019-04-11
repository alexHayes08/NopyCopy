using System;
using System.Collections.Generic;
using System.Linq;

namespace NopyCopyV2.Modals
{
    public interface INopyCopyConfiguration
    {
        bool EnableFileExtensions { get; }
        bool IsWhiteList { get; }
        string WatchedFileExtensions { get; }
    }

    /// <summary>
    ///     TODO: Refactory into own file.
    /// </summary>
    namespace Extensions
    {
        public static class NopyCopyConfiguration
        {
            public static IEnumerable<string> GetWatchedFileExensions(
                this INopyCopyConfiguration config)
            {
                return config.WatchedFileExtensions
                    .Split(',')
                    .Select(str => str.Trim());
            }

            /// <summary>
            ///     Universal way of converting the list of strings to a single
            ///     string.
            /// </summary>
            /// <param name="watchedFileExtensions"></param>
            /// <returns></returns>
            public static string ToCustomString(
                this IEnumerable<string> watchedFileExtensions)
            {
                return String.Join(", ", watchedFileExtensions);
            }
        }
    }
}
