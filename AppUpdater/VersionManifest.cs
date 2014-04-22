namespace AppUpdater
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.IO;
    using Delta;

    #endregion

    public class VersionManifest
    {
        public Version Version { get; private set; }
        public IEnumerable<VersionManifestFile> Files { get; private set; }

        public VersionManifest(Version version, IEnumerable<VersionManifestFile> files)
        {
            Version = version;
            Files = files;
        }

        public static VersionManifest LoadVersionFile(Version version, string filename)
        {
            var doc = new XmlDocument();
            doc.Load(filename);

            return LoadData(version, doc);
        }

        public static VersionManifest LoadVersionData(Version version, string data)
        {
            var doc = new XmlDocument();
            doc.LoadXml(data);

            return LoadData(version, doc);
        }

        private static VersionManifest LoadData(Version version, XmlDocument doc)
        {
            var files = new List<VersionManifestFile>();
            foreach (XmlNode fileNode in doc.SelectNodes("manifest/files/file"))
            {
                var filename = fileNode.Attributes["name"].Value;
                var checksum = fileNode.Attributes["checksum"].Value;
                var size = long.Parse(fileNode.Attributes["size"].Value);
                var deltas = new List<VersionManifestDeltaFile>();
                foreach (XmlNode deltaNode in fileNode.SelectNodes("delta"))
                {
                    var deltaFilename = deltaNode.Attributes["file"].Value;
                    var deltaChecksum = deltaNode.Attributes["from"].Value;
                    var deltaSize = long.Parse(deltaNode.Attributes["size"].Value);
                    deltas.Add(new VersionManifestDeltaFile(deltaFilename, deltaChecksum, deltaSize));
                }

                files.Add(new VersionManifestFile(filename, checksum, size, deltas));
            }

            return new VersionManifest(version, files);
        }

        public UpdateRecipe UpdateTo(VersionManifest newVersionManifest)
        {
            var recipeFiles = new List<UpdateRecipeFile>();
            foreach (var file in newVersionManifest.Files)
            {
                var originalFile = Files.FirstOrDefault(x => x.Name.Equals(file.Name, StringComparison.CurrentCultureIgnoreCase));
                var action = FileUpdateAction.Download;
                var fileToDownload = file.DeployedName;
                var size = file.Size;
                if (originalFile != null)
                {
                    if (originalFile.Checksum == file.Checksum)
                    {
                        action = FileUpdateAction.Copy;
                    }
                    else if (DeltaAPI.IsSupported())
                    {
                        var delta = file.GetDeltaFrom(originalFile.Checksum);
                        if (delta != null)
                        {
                            action = FileUpdateAction.DownloadDelta;
                            fileToDownload = delta.Filename;
                            size = delta.Size;
                        }
                    }
                }

                recipeFiles.Add(new UpdateRecipeFile(file.Name, file.Checksum, size, action, fileToDownload));
            }

            return new UpdateRecipe(newVersionManifest.Version, Version, recipeFiles);
        }

        public static VersionManifest GenerateFromDirectory(Version version, string directory)
        {
            var files =
                from dir in new[] { directory.TrimEnd(Path.DirectorySeparatorChar, 
                                                      Path.AltDirectorySeparatorChar) }
                select new DirectoryInfo(dir) into dir
                from file in dir.EnumerateFiles("*", SearchOption.AllDirectories)
                // TODO filter hidden and system files
                select new VersionManifestFile(file.FullName.Substring(dir.FullName.Length + 1), 
                               Checksum.Calculate(file.FullName), 
                               file.Length);
            
            return new VersionManifest(version, files.ToArray());
        }

        public void SaveToFile(string filename)
        {
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            using (var xml = XmlWriter.Create(filename, settings))
            {
                xml.WriteStartElement("manifest");
                xml.WriteStartElement("files");
                foreach (var file in Files)
                {
                    xml.WriteStartElement("file");
                    xml.WriteAttributeString("name", file.Name);
                    xml.WriteAttributeString("checksum", file.Checksum);
                    xml.WriteAttributeString("size", file.Size.ToString());
                    foreach (var delta in file.Deltas)
                    {
                        xml.WriteStartElement("delta");
                        xml.WriteAttributeString("from", delta.Checksum);
                        xml.WriteAttributeString("size", delta.Size.ToString());
                        xml.WriteAttributeString("file", delta.Filename);
                        xml.WriteEndElement();
                    }
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
                xml.WriteEndElement();
            }
        }
    }
}
