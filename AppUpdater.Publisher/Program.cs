namespace AppUpdater.Publisher
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    #endregion

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
                Console.Error.WriteLine(e.GetBaseException().Message);
                Trace.TraceError(e.ToString());
                return 0xbad;
            }
        }

        static int Run(IEnumerable<string> args)
        {
            string sourceDirectory;
            string targetDirectory;
            Version version;
            var numberOfVersionsAsDelta = 0;
            
            using (var arg = args.GetEnumerator())
            {
                if (!arg.MoveNext())
                {
                    ShowUsage();
                    return 1;
                }

                if (string.IsNullOrEmpty(sourceDirectory = arg.Current))
                    throw new Exception("Missing source directory path specification.");
                if (!Directory.Exists(sourceDirectory))
                    throw new DirectoryNotFoundException("Source directory not found: " + sourceDirectory);

                if (!arg.MoveNext() || string.IsNullOrEmpty(targetDirectory = arg.Current))
                    throw new Exception("Missing target directory path specification.");
                if (!Directory.Exists(targetDirectory))
                    throw new DirectoryNotFoundException("Target directory not found: " + targetDirectory);

                if (!arg.MoveNext())
                    throw new Exception("Missing version specification.");
                if (!Version.TryParse(arg.Current, out version))
                    throw new Exception(string.Format("'{0}' is not a valid version specification.", arg.Current));

                using (var option = ParseArgs(arg, (nm, cn, val) => new { Name = nm, CanonicalName = cn, Value = val }))
                while (option.MoveNext())
                {
                    switch (option.Current.CanonicalName)
                    {
                        case "deltas":
                            if (!int.TryParse(option.Current.Value, out numberOfVersionsAsDelta))
                                throw new Exception("The 'delta' argument is not a valid number.");
                            break;
                        default:
                            throw new Exception("Unknown argument: " + option.Current.Name);
                    }
                }
            }

            PublishVersion(sourceDirectory, targetDirectory, version, numberOfVersionsAsDelta);
            return 0;
        }

        static void PublishVersion(string sourceDirectory, string targetDirectory, Version version, int numberOfVersionsAsDelta)
        {
            Console.WriteLine();
            Console.WriteLine("Publishing version \"{0}\"...", version);
            Console.WriteLine("Source directory: {0}", sourceDirectory);
            Console.WriteLine("Target directory: {0}", targetDirectory);
            if (numberOfVersionsAsDelta > 0)
                Console.WriteLine("Generating delta information for the latest {0} versions.", numberOfVersionsAsDelta);
            Console.WriteLine();

            AppPublisher.Publish(sourceDirectory, targetDirectory, version, numberOfVersionsAsDelta);

            Console.WriteLine("Publish succeeded.");
        }

        static IEnumerator<T> ParseArgs<T>(IEnumerator<string> e,
            Func<string, string, string, T> selector)
        {
            while (e.MoveNext())
            {
                var arg = e.Current;
                if (!arg.StartsWith("-"))
                    throw new Exception("Invalid argument: " + arg);

                var tokens = arg.Split(new[] { ':' }, 2);
                var name = tokens[0].Remove(0, 1);
                yield return selector(name, 
                                      name.ToLowerInvariant(),
                                      tokens.Length == 1 ? null : tokens[1]);
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  {0} SOURCE_DIR TARGET_DIR VERSION [-deltas:NUM]",
                Path.GetFileName(Environment.GetCommandLineArgs()[0]));
        }
    }
}
