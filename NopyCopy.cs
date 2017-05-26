using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NopyCopy
{
    class Item
    {
        public Item(string from, string to)
        {
            From = from;
            To = to;
        }

        public string From { get; set; }
        public string To { get; set; }
    }
    class Program
    {
        static List<Item> MonitoredDirectories;
        static void Main(string[] args)
        {
            MonitoredDirectories = new List<Item>();
            while (true)
            {
                Console.Write("Enter plugin directory to watch: ");
                var projectRoot = Console.ReadLine();

                if (!Directory.Exists(projectRoot))
                {
                    Console.WriteLine("Unfound.");
                    continue;
                }


                Console.Write("Enter directory to copy to: ");

                var copyTo = Console.ReadLine();

                if (!Directory.Exists(copyTo))
                {
                    Console.WriteLine("Unfound.");
                    continue;
                }

                projectRoot = projectRoot.TrimEnd('\\');
                copyTo = copyTo.TrimEnd('\\');

                var item = new Item(projectRoot.ToLower(), copyTo.ToLower());
                MonitoredDirectories.Add(item);

                AddWatcher(projectRoot);
            }

        }



        private static void AddWatcher(string rootDirectory)
        {
            var watcher = new FileSystemWatcher(rootDirectory);
            watcher.IncludeSubdirectories = true;
            watcher.Filter = "";
            watcher.Changed += NopyCopy;
            watcher.Created += NopyCopy;
            watcher.Deleted += NopyCopy;

            watcher.EnableRaisingEvents = true;
        }

        private static bool ShouldCopy(string path)
        {
            var ext = Path.GetExtension(path);

            if (string.IsNullOrEmpty(ext))
            {
                return false;
            }

            switch (ext.ToLower())
            {
                case ".cshtml":
                case ".js":
                case ".css":
                    return true;
                default:
                    return false;
            }
        }




        private static void NopyCopy(object sender, FileSystemEventArgs e)
        {
            if (!ShouldCopy(e.FullPath))
            {
                return;
            }

            var baseStr = "[" + DateTime.Now + ": " +
                          e.ChangeType + " " + e.FullPath + "]";

            var item = MonitoredDirectories.FirstOrDefault(q => e.FullPath.ToLower().Contains(q.From));

            if (item == null)
            {
                Console.WriteLine(baseStr + ": couldn't find matching item?!");
                return;
            }

            var relativePath = e.FullPath.ToLower().Replace(item.From, string.Empty);
            var copyTo = item.To + relativePath;// Path.Combine(item.To, relativePath);

            try
            {
                File.SetAttributes(e.FullPath, FileAttributes.Normal);
                File.SetAttributes(copyTo, FileAttributes.Normal);

                File.Copy(e.FullPath, copyTo, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            Console.WriteLine("{0} || File copied! From {1} to {2}", baseStr, item.From, item.To);
        }
    }
}
