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
        readonly string baseDir;
        string configFilePath;

        public static Func<string> GetExecutablePath = GetExecutingAssemblyLocation;

        public LocalStructureManager(string baseDir)
        {
            this.baseDir = baseDir;
        }

        public void CreateVersionDir(Version version)
        {
            Directory.CreateDirectory(GetVersionPath(version));
        }

        public void DeleteVersionDir(Version version)
        {
            Directory.Delete(GetVersionPath(version), true);
        }

        public IEnumerable<Version> GetInstalledVersions()
        {
            return from dir in new DirectoryInfo(baseDir).EnumerateDirectories()
                   select new Version(dir.Name);
        }

        public VersionManifest LoadManifest(Version version)
        {
            var versionPath = GetVersionPath(version);
            return VersionManifest.GenerateFromDirectory(version, versionPath);
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
            var value = GetConfigValue("lastVersion");
            return !string.IsNullOrEmpty(value) ? new Version(value) : null;
        }

        public void SetLastValidVersion(Version version)
        {
            SetConfigValue("lastVersion", version);
        }

        public Version GetExecutingVersion()
        {
            return new Version(Directory.GetParent(GetExecutablePath()).Name);
        }

        public bool HasVersionFolder(Version version)
        {
            return Directory.Exists(GetVersionPath(version));
        }

        public void CopyFile(Version originVersion, Version destinationVersion, string fileName)
        {
            var originFilePath = Path.Combine(GetVersionPath(originVersion), fileName);
            var destinationFilePath = Path.Combine(GetVersionPath(destinationVersion), fileName);

            File.Copy(originFilePath, destinationFilePath, true);
        }

        public void SaveFile(Version version, string fileName, byte[] data)
        {
            var destinationFilePath = Path.Combine(GetVersionPath(version), fileName);
            File.WriteAllBytes(destinationFilePath, data);
        }

        public void ApplyDelta(Version originalVersion, Version newVersion, string fileName, byte[] deltaData)
        {
            var originalFile = GetFilePath(originalVersion, fileName);
            var newFile = GetFilePath(newVersion, fileName);
            var deltaFile = Path.GetTempFileName();
            File.WriteAllBytes(deltaFile, deltaData);

            DeltaAPI.ApplyDelta(originalFile, newFile, deltaFile);
        }

        public Uri GetUpdateServerUrl()
        {
            var configFilename = Path.Combine(baseDir, "config.xml");
            var doc = XDocument.Load(configFilename);
            return new Uri((string) doc.Elements("config").Elements("updateServer").Single());
        }

        string GetVersionPath(Version version)
        {
            return Path.Combine(baseDir, version.ToString());
        }

        string GetFilePath(Version version, string fileName)
        {
            return Path.Combine(GetVersionPath(version), fileName);
        }

        private static string GetExecutingAssemblyLocation()
        {
            return Assembly.GetExecutingAssembly().Location;
        }

        string ConfigFilePath
        {
            get { return configFilePath ?? (configFilePath = Path.Combine(baseDir, "config.xml")); }
        }

        private string GetConfigValue(string name)
        {
            var doc = XDocument.Load(ConfigFilePath);
            var configValue = (string) doc.Elements("config").Elements(name).SingleOrDefault();
            return configValue ?? string.Empty;
        }

        private void SetConfigValue(string name, object value)
        {
            var configFilePath = ConfigFilePath;
            var doc = XDocument.Load(configFilePath);
            // ReSharper disable once PossibleNullReferenceException
            doc.Root.SetElementValue(name, string.Format(CultureInfo.InvariantCulture, "{0}", value));
            doc.Save(configFilePath);
        }
    }
}
