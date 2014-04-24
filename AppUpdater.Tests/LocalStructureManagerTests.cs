namespace AppUpdater.Tests
{
    #region Imports

    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using Delta;
    using NUnit.Framework;

    #endregion

    [TestFixture]
    public class LocalStructureManagerTests
    {
        LocalStructureManager structureManager;
        string baseDir;

        [SetUp]
        public void Setup()
        {
            baseDir = Path.GetTempFileName() + "_";
            Directory.CreateDirectory(baseDir);
            structureManager = new LocalStructureManager(baseDir);
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void CreateVersionDir_CreatesADirWithTheVersionName()
        {
            structureManager.CreateVersionDir(new Version("1.0.0"));

            var exists = Directory.Exists(Path.Combine(baseDir, "1.0.0"));

            Assert.That(exists, Is.True);
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void GetInstalledVersions_ReturnsAllInstalledVersions()
        {
            var expectedVersions = new[] { new Version("1.0.0"), new Version("2.0.0"), new Version("3.1.1") };

            Array.ForEach(expectedVersions, structureManager.CreateVersionDir);

            var versions = structureManager.GetInstalledVersions();

            Assert.That(versions, Is.EqualTo(expectedVersions));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void DeleteVersionDir_DeletesTheDirectory()
        {
            var dir = Path.Combine(baseDir, "1.0.0");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "a.txt"), "test");

            structureManager.DeleteVersionDir(new Version("1.0.0"));

            var exists = Directory.Exists(Path.Combine(baseDir, "1.0.0"));
            Assert.That(exists, Is.False);
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void LoadManifest_GeneratesTheManifest()
        {
            var dir = Path.Combine(baseDir, "1.0.0");
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(Path.Combine(dir, "abc")); 
            File.WriteAllText(Path.Combine(dir, "test1.txt"), "some text");
            File.WriteAllText(Path.Combine(dir, "abc\\test2.txt"), "another text");

            var manifest = structureManager.LoadManifest(new Version("1.0.0"));

            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.Version, Is.EqualTo(new Version("1.0.0")));
            Assert.That(manifest.Files, Has.Count.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Name, Is.EqualTo("test1.txt"));
            Assert.That(manifest.Files.ElementAt(0).Checksum, Is.EqualTo("b94f6f125c79e3a5ffaa826f584c10d52ada669e6762051b826b55776d05aed2"));
            Assert.That(manifest.Files.ElementAt(0).Size, Is.EqualTo(9));
            Assert.That(manifest.Files.ElementAt(1).Name, Is.EqualTo("abc\\test2.txt"));
            Assert.That(manifest.Files.ElementAt(1).Checksum, Is.EqualTo("4895ecc6f0c011072af486ea30a1239caa1b297fb61ecaca8ac94d9c2071be22"));
            Assert.That(manifest.Files.ElementAt(1).Size, Is.EqualTo(12));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void GetCurrentVersion_ReturnsTheVersion()
        {
            var data = @"<config><version current='1.2.3' /></config>";
            File.WriteAllText(Path.Combine(baseDir, "config.xml"), data);

            var currentVersion = structureManager.GetCurrentVersion();

            Assert.That(currentVersion, Is.EqualTo(new Version("1.2.3")));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void GetCurrentVersion_WithoutCurrentVersionDefined_ReturnsNull()
        {
            var data = @"<config></config>";
            File.WriteAllText(Path.Combine(baseDir, "config.xml"), data);

            var version = structureManager.GetCurrentVersion();

            Assert.That(version, Is.Null);
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void SetCurrentVersion_UpdatesTheConfigFile()
        {
            var data = @"<config><version current='1.2.3' /></config>";
            var configFilename = Path.Combine(baseDir, "config.xml");
            File.WriteAllText(configFilename, data);

            structureManager.SetCurrentVersion(new Version("3.4.5"));

            var version = (string) XDocument.Load(configFilename)
                                            .Elements("config").Take(1)
                                            .Elements("version").Take(1)
                                            .Attributes("current").First();

            Assert.That(version, Is.EqualTo("3.4.5"));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void SetCurrentVersion_KeepsTheLastVersion()
        {
            var data = @"<config><version>1.2.3</version><last_version>3.0.1</last_version></config>";
            var configFilename = Path.Combine(baseDir, "config.xml");
            File.WriteAllText(configFilename, data);

            structureManager.SetCurrentVersion(new Version("3.4.5"));

            var lastVersion = (string) XDocument.Load(configFilename)
                                                .Elements("config")
                                                .Elements("last_version")
                                                .SingleOrDefault();
            Assert.That(lastVersion, Is.Not.Null);
            Assert.That(lastVersion, Is.EqualTo("3.0.1"));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void GetLastValidVersion_WithoutLastVersionDefined_ReturnsNull()
        {
            var data = @"<config><version>1.2.3</version></config>";
            File.WriteAllText(Path.Combine(baseDir, "config.xml"), data);

            var lastVersion = structureManager.GetLastValidVersion();

            Assert.That(lastVersion, Is.Null);
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void GetLastValidVersion_ReturnsTheVersion()
        {
            var data = @"<config><version current='1.2.3' last='3.1.1' /></config>";
            File.WriteAllText(Path.Combine(baseDir, "config.xml"), data);

            var lastVersion = structureManager.GetLastValidVersion();

            Assert.That(lastVersion, Is.EqualTo(new Version("3.1.1")));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void SetLastValidVersion_WithAnUndefinedVersion_UpdatesTheConfigFile()
        {
            var data = @"<config><version current='1.2.3' /></config>";
            var configFilename = Path.Combine(baseDir, "config.xml");
            File.WriteAllText(configFilename, data);

            structureManager.SetLastValidVersion(new Version("3.3.4"));

            var lastVersion = (string) XDocument.Load(configFilename)
                                                .Elements("config").Take(1)
                                                .Elements("version").Take(1)
                                                .Attributes("last")
                                                .FirstOrDefault();
            Assert.That(lastVersion, Is.Not.Null);
            Assert.That(lastVersion, Is.EqualTo("3.3.4"));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void SetLastValidVersion_UpdatesTheConfigFile()
        {
            var data = @"<config><version current='1.2.3' last='1.2.0' /></config>";
            var configFilename = Path.Combine(baseDir, "config.xml");
            File.WriteAllText(configFilename, data);

            structureManager.SetLastValidVersion(new Version("3.3.4"));

            var lastVersion = (string) XDocument.Load(configFilename)
                                                .Elements("config").Take(1)
                                                .Elements("version").Take(1)
                                                .Attributes("last")
                                                .FirstOrDefault();
            Assert.That(lastVersion, Is.Not.Null);
            Assert.That(lastVersion, Is.EqualTo("3.3.4"));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void SetLastValidVersion_KeepsTheVersion()
        {
            var data = @"<config><version>1.2.3</version><last_version>3.0.1</last_version></config>";
            var configFilename = Path.Combine(baseDir, "config.xml");
            File.WriteAllText(configFilename, data);

            structureManager.SetLastValidVersion(new Version("3.4.5"));

            var version = (string) XDocument.Load(configFilename)
                                            .Elements("config")
                                            .Elements("version")
                                            .SingleOrDefault();
            Assert.That(version, Is.Not.Null);
            Assert.That(version, Is.EqualTo("1.2.3"));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void GetExecutingVersion_ReturnsTheVersionThatIsBeingExecuted()
        {
            LocalStructureManager.GetExecutablePath = () => @"C:\Test\AppRoot\1.4.5\app.exe";

            var executingVersion = structureManager.GetExecutingVersion();

            Assert.That(executingVersion, Is.EqualTo(new Version("1.4.5")));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void HasVersionFolder_WithTheFolder_ReturnsTrue()
        {
            Directory.CreateDirectory(Path.Combine(baseDir, "4.5.6"));

            var hasFolder = structureManager.HasVersionFolder(new Version("4.5.6"));

            Assert.That(hasFolder, Is.True);
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void HasVersionFolder_WithoutTheFolder_ReturnsFalse()
        {
            var hasFolder = structureManager.HasVersionFolder(new Version("9.9.9"));

            Assert.That(hasFolder, Is.False);
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void CopyFile_CopyTheFileFromTheOriginalVersion()
        {
            Directory.CreateDirectory(Path.Combine(baseDir, "1.2.3"));
            Directory.CreateDirectory(Path.Combine(baseDir, "4.5.6"));
            File.WriteAllText(Path.Combine(baseDir, "1.2.3\\test.txt"), "some value");

            structureManager.CopyFile(new Version("1.2.3"), new Version("4.5.6"), "test.txt");

            var destinationFile = Path.Combine(baseDir, "4.5.6\\test.txt");
            Assert.That(File.Exists(destinationFile), Is.True);
            Assert.That(File.ReadAllText(destinationFile), Is.EqualTo("some value"));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void SaveFile_SavesTheFileInTheVersionDirectory()
        {
            var data = new byte[] { 4, 5, 6 };
            Directory.CreateDirectory(Path.Combine(baseDir, "1.2.3"));

            structureManager.SaveFile(new Version("1.2.3"), "test.txt", data);

            var destinationFile = Path.Combine(baseDir, "1.2.3\\test.txt");
            Assert.That(File.Exists(destinationFile), Is.True);
            Assert.That(File.ReadAllBytes(destinationFile), Is.EqualTo(data));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void ApplyDelta_SavesThePatchedFile()
        {
            Directory.CreateDirectory(Path.Combine(baseDir, "1.2.3"));
            Directory.CreateDirectory(Path.Combine(baseDir, "2.0.0"));
            var originalData = new byte[] { 4, 5, 6, 5, 4 };
            var newData = new byte[] { 4, 5, 6, 5, 4 };
            var originalFile = Path.Combine(baseDir, "1.2.3\\test1.dat");
            var newFile = Path.GetTempFileName();
            var deltaFile = Path.GetTempFileName();
            var patchedFile = Path.GetTempFileName();
            File.WriteAllBytes(originalFile, originalData);
            File.WriteAllBytes(newFile, newData);
            DeltaAPI.CreateDelta(originalFile, newFile, deltaFile, true);
            var deltaData = File.ReadAllBytes(deltaFile);

            structureManager.ApplyDelta(new Version("1.2.3"), new Version("2.0.0"), "test1.dat", deltaData);

            Assert.That(File.Exists(Path.Combine(baseDir, "2.0.0\\test1.dat")), Is.True);
            var patchedData = File.ReadAllBytes(Path.Combine(baseDir, "2.0.0\\test1.dat"));
            Assert.That(patchedData, Is.EqualTo(newData));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void GetUpdateServerUri_ReturnsTheUri()
        {
            var data = @"<config><updateServer url='http://localhost:8080/update/' /></config>";
            var configFilename = Path.Combine(baseDir, "config.xml");
            File.WriteAllText(configFilename, data);
            
            var uri = structureManager.GetUpdateServerUrl();

            Assert.That(uri.ToString(), Is.EqualTo("http://localhost:8080/update/"));
        }
    }
}
