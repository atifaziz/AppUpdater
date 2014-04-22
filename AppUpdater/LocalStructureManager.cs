namespace AppUpdater
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using Delta;

    #endregion
    
    public class LocalStructureManager : ILocalStructureManager
    {
        public string baseDir;
        public static Func<string> GetExecutablePath = GetExecutingAssemblyLocation;

        public LocalStructureManager(string baseDir)
        {
            this.baseDir = baseDir;
        }

        public void CreateVersionDir(Version version)
        {
            Directory.CreateDirectory(GetVersionDir(version));
        }

        public void DeleteVersionDir(Version version)
        {
            Directory.Delete(GetVersionDir(version), true);
        }

        public IEnumerable<Version> GetInstalledVersions()
        {
            return from dir in new DirectoryInfo(baseDir).EnumerateDirectories()
                   select new Version(dir.Name);
        }

        public VersionManifest LoadManifest(Version version)
        {
            var versionDir = GetVersionDir(version);
            return VersionManifest.GenerateFromDirectory(version, versionDir);
        }

        public Version GetCurrentVersion()
        {
            var value = GetConfigValue("version");
            return !string.IsNullOrEmpty(value) ? new Version(value) : null;
        }

        public void SetCurrentVersion(Version version)
        {
            SetConfigValue("version", version);
        }

        public Version GetLastValidVersion()
        {
            var value = GetConfigValue("last_version");
            return !string.IsNullOrEmpty(value) ? new Version(value) : null;
        }

        public void SetLastValidVersion(Version version)
        {
            SetConfigValue("last_version", version);
        }

        public Version GetExecutingVersion()
        {
            return new Version(Directory.GetParent(GetExecutablePath()).Name);
        }

        public bool HasVersionFolder(Version version)
        {
            return Directory.Exists(GetVersionDir(version));
        }

        public void CopyFile(Version originVersion, Version destinationVersion, string filename)
        {
            var originFilename = Path.Combine(GetVersionDir(originVersion), filename);
            var destinationFilename = Path.Combine(GetVersionDir(destinationVersion), filename);

            File.Copy(originFilename, destinationFilename, true);
        }

        public void SaveFile(Version version, string filename, byte[] data)
        {
            var destinationFilename = Path.Combine(GetVersionDir(version), filename);
            File.WriteAllBytes(destinationFilename, data);
        }

        public void ApplyDelta(Version originalVersion, Version newVersion, string filename, byte[] deltaData)
        {
            var originalFile = GetFilename(originalVersion, filename);
            var newFile = GetFilename(newVersion, filename);
            var deltaFile = Path.GetTempFileName();
            File.WriteAllBytes(deltaFile, deltaData);

            DeltaAPI.ApplyDelta(originalFile, newFile, deltaFile);
        }

        public Uri GetUpdateServerUri()
        {
            var configFilename = Path.Combine(baseDir, "config.xml");
            var doc = XDocument.Load(configFilename);
            return new Uri((string) doc.Elements("config").Elements("updateServer").Single());
        }

        private string GetVersionDir(Version version)
        {
            return Path.Combine(baseDir, version.ToString());
        }

        private string GetFilename(Version version, string filename)
        {
            return Path.Combine(GetVersionDir(version), filename);
        }

        private static string GetExecutingAssemblyLocation()
        {
            return Assembly.GetExecutingAssembly().Location;
        }

        private string GetConfigValue(string name)
        {
            var configFilename = Path.Combine(baseDir, "config.xml");
            var doc = XDocument.Load(configFilename);
            var configValue = (string) doc.Elements("config").Elements(name).SingleOrDefault();
            return configValue ?? string.Empty;
        }

        private void SetConfigValue(string name, object value)
        {
            var configFilename = Path.Combine(baseDir, "config.xml");
            var doc = XDocument.Load(configFilename);
            // ReSharper disable once PossibleNullReferenceException
            doc.Root.SetElementValue(name, string.Format(CultureInfo.InvariantCulture, "{0}", value));
            doc.Save(configFilename);
        }
    }
}
