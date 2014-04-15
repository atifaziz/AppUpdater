using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AppUpdater.Runner
{
    static class Program
    {
        static void Main(string[] args)
        {
            var dir = Path.GetDirectoryName(typeof(Program).Assembly.Location); // ReSharper disable once AssignNullToNotNullAttribute
            var config = XDocument.Load(Path.Combine(dir, "config.xml")).Root;  // ReSharper disable once PossibleNullReferenceException

            var version     = (string) config.Element("version");
            var lastVersion = (string) config.Element("last_version");
            var executable  = (string) config.Element("executable");

            var runLast = args.Any(x => x.Equals("-last", StringComparison.CurrentCultureIgnoreCase));
            if (runLast && lastVersion == null)
            {
                Console.WriteLine("Last version is not defined.");
                runLast = false;
            }

            if (runLast)
            {
                ExecuteApplication(dir, lastVersion, executable, args);
            }
            else
            {
                ExecuteApplication(dir, version, executable, args);
            }
        }

        private static void ExecuteApplication(string baseDir, string version, string executable, string[] args)
        {
            string path = Path.Combine(baseDir, Path.Combine(version, executable));
            Process.Start(path, String.Join(" ", args));
        }
    }
}
