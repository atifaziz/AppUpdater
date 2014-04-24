namespace AppUpdater.Publisher
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Delta;

    #endregion

    static class AppPublisher
    {
        public static void Publish(string sourceDirectory, string destionationDirectory, Version version, int numberOfVersionsAsDelta)
        {
            sourceDirectory = PathUtils.AddTrailingSlash(sourceDirectory);
            destionationDirectory = PathUtils.AddTrailingSlash(destionationDirectory);
            var destinationVersionDirectory = PathUtils.AddTrailingSlash(Path.Combine(destionationDirectory, version.ToString()));

            CopyFiles(sourceDirectory, destinationVersionDirectory);
            var manifest = VersionManifest.GenerateFromDirectory(version, sourceDirectory);
            if (numberOfVersionsAsDelta > 0)
            {
                var files = GenerateDeltas(manifest, sourceDirectory, destionationDirectory, version, numberOfVersionsAsDelta);
                manifest = new VersionManifest(manifest.Version, files.ToArray());
            }
            manifest.SaveToFile(Path.Combine(destinationVersionDirectory, "manifest.xml"));
            SaveConfigFile(destionationDirectory, version);
        }

        static IEnumerable<VersionManifestFile> GenerateDeltas(VersionManifest manifest, string sourceDirectory, string destionationDirectory, Version newVersion, int numberOfVersionsAsDelta)
        {
            var newVersionDirectory = Path.Combine(destionationDirectory, newVersion.ToString());
            var files =
                from vms in new[]
                {
                    from vds in new[]
                    {
                        from dir in new[] { new DirectoryInfo(destionationDirectory) }
                        select dir.EnumerateDirectories() into subdirs
                        from subdir in subdirs
                        where (subdir.Attributes & (FileAttributes.Hidden | FileAttributes.System)) == 0
                        select new
                        {
                            Version = new Version(subdir.Name),
                            Directory = subdir,
                        } into vd
                        where vd.Version != newVersion
                        orderby vd.Version descending 
                        select vd
                    }
                    from vd in vds.Take(numberOfVersionsAsDelta)
                    let manifestPath = Path.Combine(vd.Directory.FullName, "manifest.xml")
                    select new
                    {
                        VersionManifest.LoadVersionFile(vd.Version, manifestPath).Files,
                        vd.Directory,
                    }
                }
                from file in manifest.Files
                select new
                {
                    file.Name,
                    file.Checksum,
                    file.Size,
                    file.Deltas,
                    NewDeltas = 
                        from vm in vms
                        let vmFile = vm.Files.FirstOrDefault(x => x.Name.Equals(file.Name, StringComparison.CurrentCultureIgnoreCase))
                        where vmFile != null
                           && vmFile.Checksum != file.Checksum
                           && file.GetDeltaFrom(vmFile.Checksum) == null
                        let deltaFileName = vmFile.Name 
                                          + "." 
                                          + AbbreviateChecksum(vmFile.Checksum) 
                                          + ".deploy"
                        select new
                        {
                            OldFilePath = Path.Combine(vm.Directory.FullName, vmFile.Name + ".deploy"),
                            DecompressedNewFilePath = Path.Combine(sourceDirectory, vmFile.Name),
                            DeltaFileName = "deltas/" + /* TODO URL encoding */ deltaFileName,
                            DeltaFilePath = Path.Combine(newVersionDirectory, "deltas", deltaFileName),
                            vmFile.Checksum,
                        }                           
                };

            foreach (var file in files)
            {
                var deltas = new List<VersionManifestDeltaFile>();
                foreach (var delta in file.NewDeltas)
                {
                    var decompressedOldFile = Path.GetTempFileName();
                    using (var input = File.OpenRead(delta.OldFilePath))
                    using (var output = File.OpenWrite(decompressedOldFile))
                        DataCompressor.Decompress(input, output);

                    Directory.CreateDirectory(Path.GetDirectoryName(delta.DeltaFilePath));
                    DeltaAPI.CreateDelta(decompressedOldFile, delta.DecompressedNewFilePath, delta.DeltaFilePath, true);
                    File.Delete(decompressedOldFile);

                    var size = new FileInfo(delta.DeltaFilePath).Length;
                    deltas.Add(new VersionManifestDeltaFile(delta.DeltaFileName, delta.Checksum, size));
                }

                yield return new VersionManifestFile(file.Name, file.Checksum, file.Size, 
                                                     file.Deltas.Concat(deltas));
            }
        }

        static string AbbreviateChecksum(string checksum)
        {
            return checksum == null || checksum.Length < 5 
                 ? checksum + string.Empty 
                 : checksum.Substring(0, 5);
        }

        static void CopyFiles(string sourceDirectory, string destinationVersionDirectory)
        {
            Directory.CreateDirectory(destinationVersionDirectory);

            foreach (var sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var sourceFileRelativePath = sourceFile.Remove(0, sourceDirectory.Length);
                var destinationFile = Path.Combine(destinationVersionDirectory, sourceFileRelativePath + ".deploy");

                Directory.CreateDirectory(Path.GetDirectoryName(destinationFile));
                CreateDeployFile(sourceFile, destinationFile);
            }
        }

        static void SaveConfigFile(string destionationDirectory, Version version)
        {
            var doc = new XElement("version", new XAttribute("current", version));
            doc.Save(Path.Combine(destionationDirectory, "version.xml"));
        }

        static void CreateDeployFile(string sourceFile, string destinationFile)
        {
            using (var streamSource = File.OpenRead(sourceFile))
            {
                using (var streamDestination = File.OpenWrite(destinationFile))
                {
                    DataCompressor.Compress(streamSource, streamDestination);
                }
            }
        }

        static class PathUtils // TODO Remove after reviewing assumptions
        {
            public static string AddTrailingSlash(string path)
            {
                return string.IsNullOrEmpty(path)
                     ? path
                     : (path[path.Length - 1] != Path.DirectorySeparatorChar
                     ? path + Path.DirectorySeparatorChar
                     : path);
            }
        }
    }
}
