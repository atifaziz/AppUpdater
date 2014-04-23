namespace AppUpdater
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Xml.Linq;
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

        public static VersionManifest LoadVersionFile(Version version, string path)
        {
            return LoadData(version, XDocument.Load(path));
        }

        public static VersionManifest LoadVersionData(Version version, string data)
        {
            return LoadData(version, XDocument.Parse(data));
        }

        static VersionManifest LoadData(Version version, XDocument doc)
        {
            var files =
                from f in doc.Elements("manifest")
                             .Elements("files").Take(1)
                             .Elements("file")
                let deltas = 
                    from d in f.Elements("delta")
                    select new VersionManifestDeltaFile(
                            (string) d.Attribute("file"),
                            (string) d.Attribute("from"),
                            (long)   d.Attribute("size"))
                select new VersionManifestFile
                (
                        (string) f.Attribute("name"),
                        (string) f.Attribute("checksum"),
                        (long)   f.Attribute("size"), 
                        deltas.ToArray());
            
            return new VersionManifest(version, files.ToArray());
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
                            fileToDownload = delta.FileName;
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
                               File.OpenRead(file.FullName).Using(Checksum.Calculate),
                               file.Length);
            
            return new VersionManifest(version, files.ToArray());
        }

        public void SaveToFile(string path)
        {
            var manifest =
                new XElement("manifest",
                    new XElement("files",
                        from file in Files
                        select new XElement("file",
                            new XAttribute("name", file.Name),
                            new XAttribute("checksum", file.Checksum),
                            new XAttribute("size", file.Size),
                            from delta in file.Deltas
                            select new XElement("delta",
                                new XAttribute("from", delta.Checksum),
                                new XAttribute("size", delta.Size),
                                new XAttribute("file", delta.FileName)))));

            manifest.Save(path);
        }
    }
}
