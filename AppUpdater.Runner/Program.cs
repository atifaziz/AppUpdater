using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AppUpdater.Runner
{
    static class Program
    {
        static int Main(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                return 0xbad;
            }
        }
 
        static int Run(string[] args)
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

            return ExecuteApplication(dir, runLast ? lastVersion : version, executable, args);
        }

        static int ExecuteApplication(string baseDir, string version, string executable, string[] args)
        {
            var path = Path.Combine(baseDir, Path.Combine(version, executable));
            using (var process = Process.Start(path, string.Join(" ", args)))
            {
                // ReSharper disable once PossibleNullReferenceException
                process.WaitForExit();
                return process.ExitCode;
            }
        }
    }
}
