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
        private string sourceDir;
        private string destinationDir;

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
            Assert.That(manifest.Files.ElementAt(0).Checksum, Is.EqualTo("A475EC7E8BDCC9B7F017B29A760A9010C8A9B6F2A9E1550A58BF77783F5C9319"));
            Assert.That(manifest.Files.ElementAt(0).Size, Is.EqualTo(15));
            Assert.That(manifest.Files.ElementAt(1).Name, Is.EqualTo("another\\test2.txt"));
            Assert.That(manifest.Files.ElementAt(1).Checksum, Is.EqualTo("16AF4D078042175206C6F05228475FA391E7DF98BF9AF599BC775EFCDB86D784"));
            Assert.That(manifest.Files.ElementAt(1).Size, Is.EqualTo(15));
        }

        [Test]
        public void Publish_ChangesTheCurrentVersion()
        {
            AppPublisher.Publish(sourceDir, destinationDir, new Version("1.1.0"), 0);

            var version = (string) XDocument.Load(Path.Combine(destinationDir, "version.xml"))
                                            .Elements("config")
                                            .Elements("version")
                                            .SingleOrDefault();

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
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Checksum, Is.EqualTo("B21A7D77034B2A1120A5E7E803AFACB52F14D6BF7C833A3F0E5B1FD10380AF3D"));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Size, Is.EqualTo(23));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Filename, Is.EqualTo("deltas\\test1.txt.B21A7.deploy"));
        }

        private void CreateVersionFiles(int diferenciator=0)
        {
            File.WriteAllText(Path.Combine(sourceDir, "test1.txt"), "test1 content " + diferenciator);
            Directory.CreateDirectory(Path.Combine(sourceDir, "another"));
            File.WriteAllText(Path.Combine(sourceDir, "another\\test2.txt"), "test2 content " + diferenciator);
        }
    }
}
