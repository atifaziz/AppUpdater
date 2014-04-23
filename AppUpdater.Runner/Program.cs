namespace AppUpdater.Runner
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    #endregion

    // ReSharper disable once PartialTypeWithSinglePart
    
    static partial class Program
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
 
        static int Run(IEnumerable<string> args)
        {
            var thisAssemblyPath = typeof(Program).Assembly.Location;
            var dir = Path.GetDirectoryName(thisAssemblyPath);
            Debug.Assert(dir != null);
            var config = XDocument.Load(Path.Combine(dir, "config.xml")).Root;  // ReSharper disable once PossibleNullReferenceException

            var version     = (string) config.Element("version");
            var lastVersion = (string) config.Element("lastVersion");
            var executable  = (string) config.Element("executable") ?? Path.GetFileName(thisAssemblyPath);

            var runLast = args.Any(x => x.Equals("-last", StringComparison.CurrentCultureIgnoreCase));
            if (runLast && lastVersion == null)
            {
                Trace.TraceWarning("Last version is not defined.");
                runLast = false;
            }

            var commandLine = Environment.CommandLine;
            var commandLineArgs = GetCommandLineArguments(commandLine);
            var path = Path.Combine(dir, Path.Combine(runLast ? lastVersion : version, executable));

            using (var process = Process.Start(path, commandLineArgs))
            {
                // ReSharper disable once PossibleNullReferenceException
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        static string GetCommandLineArguments(string commandLine)
        {
            Debug.Assert(commandLine != null);
            Debug.Assert(commandLine.Length > 0);

            int spaceIndex;
            var argsIndex = 1 + (commandLine[0] == '\"'         // quoted?
                                 ? commandLine.IndexOf('\"', 1)
                                 : (spaceIndex = commandLine.IndexOf(' ')) < 0
                                 ? commandLine.Length - 1
                                 : spaceIndex);

            return commandLine.Substring(argsIndex + 1).TrimStart();
        }
    }
}
