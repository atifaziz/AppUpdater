namespace AppUpdater.Tests.Publisher
{
    #region Imports

    using System;
    using System.Linq;
    using System.Xml.Linq;
    using NUnit.Framework;
    using AppUpdater.Publisher;
    using System.IO;

    #endregion

    [TestFixture]
    public class AppPublisherTests
    {
        string sourceDir;
        string destinationDir;

        [SetUp]
        public void Setup()
        {
            sourceDir = Path.GetTempFileName() + "_";
            destinationDir = Path.GetTempFileName() + "_";
            Directory.CreateDirectory(sourceDir);
            Directory.CreateDirectory(destinationDir);
            CreateVersionFiles();
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(sourceDir, true);
            Directory.Delete(destinationDir, true);
        }

        [Test]
        public void Publish_CreatesADirectoryToTheNewVersion()
        {
            AppPublisher.Publish(sourceDir, destinationDir, new Version("1.0.0"), 0);

            var directoryExists = Directory.Exists(Path.Combine(destinationDir, "1.0.0"));
            Assert.That(directoryExists, Is.True);
        }

        [Test]
        public void Publish_WithValidInfo_CopiesTheFilesToTargetDirWithDeployExtension()
        {
            AppPublisher.Publish(sourceDir, destinationDir, new Version("1.1.0"), 0);

            Assert.That(File.Exists(Path.Combine(destinationDir, "1.1.0\\test1.txt.deploy")), Is.True);
            Assert.That(File.Exists(Path.Combine(destinationDir, "1.1.0\\another\\test2.txt.deploy")), Is.True);
        }

        [Test]
        public void Publish_WithValidInfo_CompressTheDeployFiles()
        {
            AppPublisher.Publish(sourceDir, destinationDir, new Version("1.1.0"), 0);

            var destinationFile = Path.Combine(destinationDir, "1.1.0\\test1.txt.deploy");
            var sourceFile = Path.Combine(sourceDir, "test1.txt");
            var originalData = File.ReadAllBytes(sourceFile);
            var compressedData = File.ReadAllBytes(destinationFile);
            var decompressedData = DataCompressor.Decompress(compressedData);
            Assert.That(decompressedData, Is.EqualTo(originalData));
        }

        [Test]
        public void Publish_WithValidInfo_GeneratesTheManifest()
        {
            var manifestFilename = Path.Combine(destinationDir, "1.1.0\\manifest.xml");

            AppPublisher.Publish(sourceDir, destinationDir, new Version("1.1.0"), 0);

            Assert.That(File.Exists(manifestFilename), Is.True);
        }

        [Test]
        public void Publish_WithValidInfo_SetsTheManifestData()
        {
            var manifestFilename = Path.Combine(destinationDir, "1.1.0\\manifest.xml");

            AppPublisher.Publish(sourceDir, destinationDir, new Version("1.1.0"), 0);

            var manifest = VersionManifest.LoadVersionData(new Version("1.1.0"), File.ReadAllText(manifestFilename));
            Assert.That(manifest.Files.Count(), Is.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Name, Is.EqualTo("test1.txt"));
            Assert.That(manifest.Files.ElementAt(0).Checksum, Is.EqualTo("a475ec7e8bdcc9b7f017b29a760a9010c8a9b6f2a9e1550a58bf77783f5c9319"));
            Assert.That(manifest.Files.ElementAt(0).Size, Is.EqualTo(15));
            Assert.That(manifest.Files.ElementAt(1).Name, Is.EqualTo("another\\test2.txt"));
            Assert.That(manifest.Files.ElementAt(1).Checksum, Is.EqualTo("16af4d078042175206c6f05228475fa391e7df98bf9af599bc775efcdb86d784"));
            Assert.That(manifest.Files.ElementAt(1).Size, Is.EqualTo(15));
        }

        [Test]
        public void Publish_ChangesTheCurrentVersion()
        {
            AppPublisher.Publish(sourceDir, destinationDir, new Version("1.1.0"), 0);

            var version = (string) XDocument.Load(Path.Combine(destinationDir, "version.xml"))
                                            .Elements("version").Take(1)
                                            .Attributes("current")
                                            .FirstOrDefault();

            Assert.That(version, Is.EqualTo("1.1.0"));
        }

        [Test]
        public void Publish_WithTwoDelta_GeneratesTheDeltaForTheLatestTwoVersion()
        {
            CreateVersionFiles(1);
            AppPublisher.Publish(sourceDir, destinationDir, new Version("1.0.0"), 0);
            CreateVersionFiles(2);
            AppPublisher.Publish(sourceDir, destinationDir, new Version("2.0.0"), 0);
            CreateVersionFiles(3);
            AppPublisher.Publish(sourceDir, destinationDir, new Version("3.0.0"), 0);
            CreateVersionFiles(4);

            AppPublisher.Publish(sourceDir, destinationDir, new Version("4.0.0"), 2);

            Assert.That(File.Exists(Path.Combine(destinationDir, "4.0.0\\deltas\\test1.txt.B21A7.deploy")), Is.True);
            Assert.That(File.Exists(Path.Combine(destinationDir, "4.0.0\\deltas\\another\\test2.txt.C031C.deploy")), Is.True);
            Assert.That(File.Exists(Path.Combine(destinationDir, "4.0.0\\deltas\\test1.txt.AF6C5.deploy")), Is.True);
            Assert.That(File.Exists(Path.Combine(destinationDir, "4.0.0\\deltas\\another\\test2.txt.ACC2A.deploy")), Is.True);
        }

        [Test]
        public void Publish_WithTwoDelta_SavesTheInfoInTheManifest()
        {
            var manifestFilename = Path.Combine(destinationDir, "4.0.0\\manifest.xml");
            CreateVersionFiles(1);
            AppPublisher.Publish(sourceDir, destinationDir, new Version("1.0.0"), 0);
            CreateVersionFiles(2);
            AppPublisher.Publish(sourceDir, destinationDir, new Version("2.0.0"), 0);
            CreateVersionFiles(3);
            AppPublisher.Publish(sourceDir, destinationDir, new Version("3.0.0"), 0);
            CreateVersionFiles(4);

            AppPublisher.Publish(sourceDir, destinationDir, new Version("4.0.0"), 2);

            var manifest = VersionManifest.LoadVersionData(new Version("4.0.0"), File.ReadAllText(manifestFilename));
            Assert.That(manifest.Files.ElementAt(0).Deltas.Count(), Is.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Checksum, Is.EqualTo("b21a7d77034b2a1120a5e7e803afacb52f14d6bf7c833a3f0e5b1fd10380af3d"));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Size, Is.EqualTo(23));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).FileName, Is.EqualTo("deltas/test1.txt.b21a7.deploy"));
        }

        void CreateVersionFiles(int diferenciator = 0)
        {
            File.WriteAllText(Path.Combine(sourceDir, "test1.txt"), "test1 content " + diferenciator);
            Directory.CreateDirectory(Path.Combine(sourceDir, "another"));
            File.WriteAllText(Path.Combine(sourceDir, "another\\test2.txt"), "test2 content " + diferenciator);
        }
    }
}
