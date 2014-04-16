using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;

namespace AppUpdater.Publisher
{
    class Program
    {
        private string sourceDirectory;
        private string targetDirectory;
        private string sourceDirectoryPath;
        private string targetDirectoryPath;
        private Version version;
        private int? numberOfVersionsAsDelta = null;

        static int Main(string[] args)
        {
            try
            {
                new Program().Run(args);
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.GetBaseException().Message);
                Trace.TraceError(e.ToString());
                return 0xbad;
            }
        }

        public void Run(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  {0} -source:source_dir -target:target_dir -version:1.0.0 -deltas:2",
                    Path.GetFileName(Environment.GetCommandLineArgs()[0]));
            }
            else
            {
                ProcessArgs(args);
                ValidateArgs();
                PublishVersion();
            }
        }

        private void PublishVersion()
        {
            Console.WriteLine();
            Console.WriteLine("Publishing version \"{0}\"...", version);
            Console.WriteLine("Source directory: {0}", sourceDirectoryPath);
            Console.WriteLine("Target directory: {0}", targetDirectoryPath);
            if (numberOfVersionsAsDelta.HasValue)
            {
                Console.WriteLine("Generating delta information for the latest {0} versions.", numberOfVersionsAsDelta);
            }
            Console.WriteLine();

            AppPublisher.Publish(sourceDirectory, targetDirectoryPath, version, numberOfVersionsAsDelta ?? 0);

            Console.WriteLine("Publish succeeded.");
        }

        private void ProcessArgs(string[] args)
        {
            foreach (var arg in args)
            {
                if (!arg.StartsWith("-"))
                {
                    throw new Exception("Invalid argument: " + arg);
                }

                var argValues = arg.Split(new []{':'}, 2);
                var commandName = argValues[0].Remove(0, 1);
                var commandValue = argValues.Length == 1 ? null : argValues[1];

                switch (commandName.ToLower())
                {
                    case "source":
                        sourceDirectory = commandValue;
                        break;
                    case "target":
                        targetDirectory = commandValue;
                        break;
                    case "version":
                        version = commandValue != null ? new Version(commandValue) : null;
                        break;
                    case "deltas":
                        int deltas;
                        if (!int.TryParse(commandValue, out deltas))
                        {
                            throw new Exception("The 'delta' argument is not a valid number.");
                        }
                        numberOfVersionsAsDelta = deltas;
                        break;
                    default:
                        throw new Exception("Unknown argument: " + arg);
                }
            }
        }

        private void ValidateArgs()
        {
            var sb = new StringBuilder();
            if (String.IsNullOrWhiteSpace(sourceDirectory))
            {
                sb.AppendLine("The 'source' argument is required.");
            }
            else
            {
                sourceDirectoryPath = Path.GetFullPath(sourceDirectory);
                if (!Directory.Exists(sourceDirectoryPath))
                {
                    throw new Exception(String.Format("The directory '{0}' could not be found.", sourceDirectoryPath));
                }
            }

            if (String.IsNullOrWhiteSpace(targetDirectory))
            {
                sb.AppendLine("The 'target' argument is required.");
            }
            else
            {
                targetDirectoryPath = Path.GetFullPath(targetDirectory);
                if (!Directory.Exists(targetDirectoryPath))
                {
                    throw new Exception(String.Format("The directory '{0}' could not be found.", targetDirectoryPath));
                }
            }

            if (version == null)
            {
                sb.AppendLine("The 'version' argument is required.");
            }

            if (sb.Length > 0)
            {
                throw new Exception(sb.ToString());
            }
        }
    }
}
