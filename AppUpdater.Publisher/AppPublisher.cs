﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using AppUpdater.Utils;
using AppUpdater.Manifest;
using System.Xml;
using AppUpdater.Delta;

namespace AppUpdater.Publisher
{
    public class AppPublisher
    {
        public void Publish(string sourceDirectory, string destionationDirectory, string version, int numberOfVersionsAsDelta)
        {
            sourceDirectory = PathUtils.AddTrailingSlash(sourceDirectory);
            destionationDirectory = PathUtils.AddTrailingSlash(destionationDirectory);
            var destinationVersionDirectory = PathUtils.AddTrailingSlash(Path.Combine(destionationDirectory, version));

            CopyFiles(sourceDirectory, destinationVersionDirectory);
            var manifest = VersionManifest.GenerateFromDirectory(version, sourceDirectory);
            GenerateDeltas(manifest, sourceDirectory, destionationDirectory, version, numberOfVersionsAsDelta);
            manifest.SaveToFile(Path.Combine(destinationVersionDirectory, "manifest.xml"));
            SaveConfigFile(destionationDirectory, version);
        }

        private void GenerateDeltas(VersionManifest manifest, string sourceDirectory, string destionationDirectory, string newVersion, int numberOfVersionsAsDelta)
        {
            var newVersionDirectory = Path.Combine(destionationDirectory, newVersion);
            var publishedVersions = Directory.EnumerateDirectories(destionationDirectory)
                                             .Select(x => x.Remove(0, destionationDirectory.Length))
                                             .Except(new[] { newVersion })
                                             .OrderByDescending(x => x)
                                             .Take(numberOfVersionsAsDelta);

            foreach (var version in publishedVersions)
            {
                var versionDirectory = Path.Combine(destionationDirectory, version);
                var manifestFile = Path.Combine(versionDirectory, "manifest.xml");
                var versionManifest = VersionManifest.LoadVersionFile(version, manifestFile);
                foreach (var file in manifest.Files)
                {
                    var fileInVersion = versionManifest.Files.FirstOrDefault(x => x.Name.Equals(file.Name, StringComparison.CurrentCultureIgnoreCase));
                    if (fileInVersion != null && fileInVersion.Checksum != file.Checksum)
                    {
                        if (file.GetDeltaFrom(fileInVersion.Checksum) == null)
                        {
                            var oldFile = Path.Combine(versionDirectory, fileInVersion.Name + ".deploy");
                            var decompressedOldFile = Path.GetTempFileName();
                            var data = File.ReadAllBytes(oldFile);
                            data = DataCompressor.Decompress(data);
                            File.WriteAllBytes(decompressedOldFile, data);

                            var decompressedNewFile = Path.Combine(sourceDirectory, fileInVersion.Name);
                            var deltaFilename = String.Format("deltas\\{0}.{1}.deploy", fileInVersion.Name, GetShortChecksum(fileInVersion.Checksum));
                            var deltaFile = Path.Combine(newVersionDirectory, deltaFilename);
                            Directory.CreateDirectory(Path.GetDirectoryName(deltaFile));
                            DeltaAPI.CreateDelta(decompressedOldFile, decompressedNewFile, deltaFile, true);
                            File.Delete(decompressedOldFile);

                            var deltaInfo = new VersionManifestDeltaFile(deltaFilename, fileInVersion.Checksum, new FileInfo(deltaFile).Length);
                            file.Deltas.Add(deltaInfo);
                        }
                    }
                }
            }

        }

        private static string GetShortChecksum(string checksum)
        {
            if (checksum == null || checksum.Length < 5)
            {
                return checksum + string.Empty;
            }

            return checksum.Substring(0, 5);
        }

        private static void CopyFiles(string sourceDirectory, string destinationVersionDirectory)
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

        private static void SaveConfigFile(string destionationDirectory, string version)
        {
            var doc = new XmlDocument();
            doc.LoadXml("<config><version></version></config>");
            doc.SelectSingleNode("config/version").InnerText = version;
            doc.Save(Path.Combine(destionationDirectory, "version.xml"));
        }

        private static void CreateDeployFile(string sourceFile, string destinationFile)
        {
            using (var streamSource = File.OpenRead(sourceFile))
            {
                using (var streamDestination = File.OpenWrite(destinationFile))
                {
                    DataCompressor.Compress(streamSource, streamDestination);
                }
            }
        }
    }
}
