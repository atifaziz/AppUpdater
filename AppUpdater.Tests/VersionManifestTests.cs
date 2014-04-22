namespace AppUpdater.Tests
{
    #region Imports

    using System;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;

    #endregion

    [TestFixture]
    public class VersionManifestTests
    {
        [Test]
        public void LoadVersionData_WithValidData_LoadsTheData()
        {
            var data = @"<manifest>
                                <files>
                                    <file name='teste1.txt' checksum='algo111' size='1000'>
                                        <delta from='AABBCC' size='500' file='teste1.txt.1.deploy' />
                                        <delta from='CCDDEE' size='400' file='teste1.txt.2.deploy' />
                                    </file>
                                    <file name='teste2.txt' checksum='algo222' size='2000' />
                                </files>
                            </manifest>";

            var manifest = VersionManifest.LoadVersionData(new Version("1.2.3"), data);

            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.Version, Is.EqualTo(new Version("1.2.3")));
            Assert.That(manifest.Files, Has.Length.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Name, Is.EqualTo("teste1.txt"));
            Assert.That(manifest.Files.ElementAt(0).Checksum, Is.EqualTo("algo111"));
            Assert.That(manifest.Files.ElementAt(0).Size, Is.EqualTo(1000));

            Assert.That(manifest.Files.ElementAt(0).Deltas.Count(), Is.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Checksum, Is.EqualTo("AABBCC"));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Size, Is.EqualTo(500));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).FileName, Is.EqualTo("teste1.txt.1.deploy"));
        }

        [Test]
        public void LoadVersionFile_LoadsTheData()
        {
            var path = Path.GetTempFileName();
            var data = @"<manifest>
                                <files>
                                    <file name='teste1.txt' checksum='algo111' size='1000'>
                                        <delta from='AABBCC' size='500' file='teste1.txt.1.deploy' />
                                        <delta from='CCDDEE' size='400' file='teste1.txt.2.deploy' />
                                    </file>
                                    <file name='teste2.txt' checksum='algo222' size='2000' />
                                </files>
                            </manifest>";
            File.WriteAllText(path, data);

            var manifest = VersionManifest.LoadVersionFile(new Version("1.2.3"), path);

            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.Version, Is.EqualTo(new Version("1.2.3")));
            Assert.That(manifest.Files, Has.Length.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Name, Is.EqualTo("teste1.txt"));
            Assert.That(manifest.Files.ElementAt(0).Checksum, Is.EqualTo("algo111"));
            Assert.That(manifest.Files.ElementAt(0).Size, Is.EqualTo(1000));

            Assert.That(manifest.Files.ElementAt(0).Deltas.Count(), Is.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Checksum, Is.EqualTo("AABBCC"));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Size, Is.EqualTo(500));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).FileName, Is.EqualTo("teste1.txt.1.deploy"));
        }

        [Test]
        public void UpdateTo_ReturnsARecipe()
        {
            var fileUpdate = new VersionManifestFile("arquivo1.txt", "123", 1000);
            var currentManifest = new VersionManifest(new Version("1.0.0"), new VersionManifestFile[] {  });
            var updateManifest = new VersionManifest(new Version("2.0.0"), new VersionManifestFile[] { fileUpdate });

            var recipe = currentManifest.UpdateTo(updateManifest);

            Assert.That(recipe, Is.Not.Null);
            Assert.That(recipe.CurrentVersion, Is.EqualTo(new Version("1.0.0")));
            Assert.That(recipe.NewVersion, Is.EqualTo(new Version("2.0.0")));
            Assert.That(recipe.Files, Has.Count.EqualTo(1));
        }

        [Test]
        public void UpdateTo_VersionWithEqualFile_SetTheActionAsCopy()
        {
            var fileUpdate = new VersionManifestFile("arquivo1.txt", "123", 1000);
            var currentManifest = new VersionManifest(new Version("1.0.0"), new VersionManifestFile[] { fileUpdate });
            var updateManifest = new VersionManifest(new Version("2.0.0"), new VersionManifestFile[] { fileUpdate });

            var recipe = currentManifest.UpdateTo(updateManifest);

            Assert.That(recipe.Files, Has.Count.EqualTo(1));
            Assert.That(recipe.Files.First().Action, Is.EqualTo(FileUpdateAction.Copy));
        }

        [Test]
        public void UpdateTo_VersionWithoutTheFile_SetTheActionAsDownload()
        {
            var fileUpdate = new VersionManifestFile("arquivo1.txt", "123", 1000);
            var currentManifest = new VersionManifest(new Version("1.0.0"), new VersionManifestFile[] {  });
            var updateManifest = new VersionManifest(new Version("2.0.0"), new VersionManifestFile[] { fileUpdate });

            var recipe = currentManifest.UpdateTo(updateManifest);

            Assert.That(recipe.Files, Has.Count.EqualTo(1));
            Assert.That(recipe.Files.First().Action, Is.EqualTo(FileUpdateAction.Download));
        }

        [Test]
        public void UpdateTo_VersionWithTheFileWithIncorrectChecksum_SetTheActionAsDownload()
        {
            var fileOriginal = new VersionManifestFile("arquivo1.txt", "333", 1000);
            var fileUpdate = new VersionManifestFile("arquivo1.txt", "123", 1000);
            var currentManifest = new VersionManifest(new Version("1.0.0"), new VersionManifestFile[] { fileOriginal });
            var updateManifest = new VersionManifest(new Version("2.0.0"), new VersionManifestFile[] { fileUpdate });

            var recipe = currentManifest.UpdateTo(updateManifest);

            Assert.That(recipe.Files, Has.Count.EqualTo(1));
            Assert.That(recipe.Files.First().Action, Is.EqualTo(FileUpdateAction.Download));
        }

        [Test]
        public void UpdateTo_WithAnIncorrectDelta_SetTheActionAsDownload()
        {
            var fileOriginal = new VersionManifestFile("arquivo1.txt", "333", 1000);
            var fileUpdate = new VersionManifestFile("arquivo1.txt", "123", 1000, new[]
            {
                new VersionManifestDeltaFile("deltas\\arquivo1.txt", "444", 10)
            });
            var currentManifest = new VersionManifest(new Version("1.0.0"), new VersionManifestFile[] { fileOriginal });
            var updateManifest = new VersionManifest(new Version("2.0.0"), new VersionManifestFile[] { fileUpdate });

            var recipe = currentManifest.UpdateTo(updateManifest);

            Assert.That(recipe.Files, Has.Count.EqualTo(1));
            Assert.That(recipe.Files.First().Action, Is.EqualTo(FileUpdateAction.Download));
        }

        [Test]
        public void UpdateTo_WithADeltaAvailable_SetTheActionAsDownloadDelta()
        {
            var fileOriginal = new VersionManifestFile("arquivo1.txt", "333", 1000);
            var fileUpdate = new VersionManifestFile("arquivo1.txt", "123", 1000, new[]
            {
                new VersionManifestDeltaFile("deltas\\arquivo1.txt", "333", 10)                                                                                      
            });
            var currentManifest = new VersionManifest(new Version("1.0.0"), new VersionManifestFile[] { fileOriginal });
            var updateManifest = new VersionManifest(new Version("2.0.0"), new VersionManifestFile[] { fileUpdate });

            var recipe = currentManifest.UpdateTo(updateManifest);

            Assert.That(recipe.Files, Has.Count.EqualTo(1));
            Assert.That(recipe.Files.First().Action, Is.EqualTo(FileUpdateAction.DownloadDelta));
        }

        [Test]
        public void GenerateFromDirectory_GeneratesTheManifest()
        {
            var dir = Path.GetTempFileName() + "_";
            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(Path.Combine(dir, "abc"));
            File.WriteAllText(Path.Combine(dir, "test1.txt"), "some text");
            File.WriteAllText(Path.Combine(dir, "abc\\test2.txt"), "another text");

            var manifest = VersionManifest.GenerateFromDirectory(new Version("1.0.0"), dir);

            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.Version, Is.EqualTo(new Version("1.0.0")));
            Assert.That(manifest.Files, Has.Length.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Name, Is.EqualTo("test1.txt"));
            Assert.That(manifest.Files.ElementAt(0).Checksum, Is.EqualTo("B94F6F125C79E3A5FFAA826F584C10D52ADA669E6762051B826B55776D05AED2"));
            Assert.That(manifest.Files.ElementAt(0).Size, Is.EqualTo(9));
            Assert.That(manifest.Files.ElementAt(1).Name, Is.EqualTo("abc\\test2.txt"));
            Assert.That(manifest.Files.ElementAt(1).Checksum, Is.EqualTo("4895ECC6F0C011072AF486EA30A1239CAA1B297FB61ECACA8AC94D9C2071BE22"));
            Assert.That(manifest.Files.ElementAt(1).Size, Is.EqualTo(12));
        }

        [Test]
        public void SaveToFile_CreatesTheFile()
        {
            var path = Path.GetTempFileName();
            var data = @"<manifest></manifest>";

            var manifest = VersionManifest.LoadVersionData(new Version("1.0.0"), data);
            manifest.SaveToFile(path);

            Assert.That(File.Exists(path), Is.True);
        }

        [Test]
        public void SaveToFile_SavesAllTheInfoToTheFile()
        {
            var path = Path.GetTempFileName();
            var data = @"<manifest>
                                <files>
                                    <file name='test1.txt' checksum='algo111' size='1000' >
                                        <delta from='AABBCC' size='500' file='teste1.txt.1.deploy' />
                                        <delta from='CCDDEE' size='400' file='teste1.txt.2.deploy' />
                                    </file>
                                    <file name='test2.txt' checksum='algo222' size='2000' />
                                </files>
                            </manifest>";

            var originalManifest = VersionManifest.LoadVersionData(new Version("1.0.0"), data);
            originalManifest.SaveToFile(path);

            var savedManifest = VersionManifest.LoadVersionData(new Version("1.0.0"), File.ReadAllText(path));
            Assert.That(savedManifest, Is.Not.Null);
            Assert.That(savedManifest.Files, Has.Length.EqualTo(2));
            Assert.That(savedManifest.Files.ElementAt(0).Name, Is.EqualTo("test1.txt"));
            Assert.That(savedManifest.Files.ElementAt(0).Checksum, Is.EqualTo("algo111"));
            Assert.That(savedManifest.Files.ElementAt(0).Size, Is.EqualTo(1000));
            Assert.That(savedManifest.Files.ElementAt(1).Name, Is.EqualTo("test2.txt"));
            Assert.That(savedManifest.Files.ElementAt(1).Checksum, Is.EqualTo("algo222"));
            Assert.That(savedManifest.Files.ElementAt(1).Size, Is.EqualTo(2000));
            Assert.That(savedManifest.Files.ElementAt(0).Deltas.Count(), Is.EqualTo(2));
            Assert.That(savedManifest.Files.ElementAt(0).Deltas.ElementAt(0).Checksum, Is.EqualTo("AABBCC"));
            Assert.That(savedManifest.Files.ElementAt(0).Deltas.ElementAt(0).Size, Is.EqualTo(500));
            Assert.That(savedManifest.Files.ElementAt(0).Deltas.ElementAt(0).FileName, Is.EqualTo("teste1.txt.1.deploy"));
        }
    }
}
