﻿namespace AppUpdater.Tests
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
        [Test] // ReSharper disable once InconsistentNaming
        public void LoadVersionData_WithValidData_LoadsTheData()
        {
            var manifest = VersionManifest.LoadVersionData(new Version("1.2.3"), @"
                <manifest>
                    <files>
                        <file name='teste1.txt' checksum='algo111' size='1000'>
                            <delta from='AABBCC' size='500' file='teste1.txt.1.deploy' />
                            <delta from='CCDDEE' size='400' file='teste1.txt.2.deploy' />
                        </file>
                        <file name='teste2.txt' checksum='algo222' size='2000' />
                    </files>
                </manifest>");

            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.Version, Is.EqualTo(new Version("1.2.3")));
            Assert.That(manifest.Files, Has.Count.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Name, Is.EqualTo("teste1.txt"));
            Assert.That(manifest.Files.ElementAt(0).Checksum, Is.EqualTo("algo111"));
            Assert.That(manifest.Files.ElementAt(0).Size, Is.EqualTo(1000));

            Assert.That(manifest.Files.ElementAt(0).Deltas.Count(), Is.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Checksum, Is.EqualTo("AABBCC"));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Size, Is.EqualTo(500));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).FileName, Is.EqualTo("teste1.txt.1.deploy"));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void LoadVersionFile_LoadsTheData()
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, @"
                <manifest>
                    <files>
                        <file name='teste1.txt' checksum='algo111' size='1000'>
                            <delta from='AABBCC' size='500' file='teste1.txt.1.deploy' />
                            <delta from='CCDDEE' size='400' file='teste1.txt.2.deploy' />
                        </file>
                        <file name='teste2.txt' checksum='algo222' size='2000' />
                    </files>
                </manifest>");

            var manifest = VersionManifest.LoadVersionFile(new Version("1.2.3"), path);

            Assert.That(manifest, Is.Not.Null);
            Assert.That(manifest.Version, Is.EqualTo(new Version("1.2.3")));
            Assert.That(manifest.Files, Has.Count.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Name, Is.EqualTo("teste1.txt"));
            Assert.That(manifest.Files.ElementAt(0).Checksum, Is.EqualTo("algo111"));
            Assert.That(manifest.Files.ElementAt(0).Size, Is.EqualTo(1000));

            Assert.That(manifest.Files.ElementAt(0).Deltas.Count(), Is.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Checksum, Is.EqualTo("AABBCC"));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).Size, Is.EqualTo(500));
            Assert.That(manifest.Files.ElementAt(0).Deltas.ElementAt(0).FileName, Is.EqualTo("teste1.txt.1.deploy"));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void UpdateTo_ReturnsARecipe()
        {
            var fileUpdate = new VersionManifestFile("arquivo1.txt", "123", 1000);
            var currentManifest = new VersionManifest(new Version("1.0.0"), new VersionManifestFile[] {  });
            var updateManifest = new VersionManifest(new Version("2.0.0"), new[] { fileUpdate });

            var recipe = currentManifest.UpdateTo(updateManifest);

            Assert.That(recipe, Is.Not.Null);
            Assert.That(recipe.CurrentVersion, Is.EqualTo(new Version("1.0.0")));
            Assert.That(recipe.NewVersion, Is.EqualTo(new Version("2.0.0")));
            Assert.That(recipe.Files, Has.Count.EqualTo(1));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void UpdateTo_VersionWithEqualFile_SetTheActionAsCopy()
        {
            var fileUpdate = new VersionManifestFile("arquivo1.txt", "123", 1000);
            var currentManifest = new VersionManifest(new Version("1.0.0"), new[] { fileUpdate });
            var updateManifest = new VersionManifest(new Version("2.0.0"), new[] { fileUpdate });

            var recipe = currentManifest.UpdateTo(updateManifest);

            Assert.That(recipe.Files, Has.Count.EqualTo(1));
            Assert.That(recipe.Files.First().Action, Is.EqualTo(FileUpdateAction.Copy));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void UpdateTo_VersionWithoutTheFile_SetTheActionAsDownload()
        {
            var fileUpdate = new VersionManifestFile("arquivo1.txt", "123", 1000);
            var currentManifest = new VersionManifest(new Version("1.0.0"), new VersionManifestFile[] {  });
            var updateManifest = new VersionManifest(new Version("2.0.0"), new[] { fileUpdate });

            var recipe = currentManifest.UpdateTo(updateManifest);

            Assert.That(recipe.Files, Has.Count.EqualTo(1));
            Assert.That(recipe.Files.First().Action, Is.EqualTo(FileUpdateAction.Download));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void UpdateTo_VersionWithTheFileWithIncorrectChecksum_SetTheActionAsDownload()
        {
            var fileOriginal = new VersionManifestFile("arquivo1.txt", "333", 1000);
            var fileUpdate = new VersionManifestFile("arquivo1.txt", "123", 1000);
            var currentManifest = new VersionManifest(new Version("1.0.0"), new[] { fileOriginal });
            var updateManifest = new VersionManifest(new Version("2.0.0"), new[] { fileUpdate });

            var recipe = currentManifest.UpdateTo(updateManifest);

            Assert.That(recipe.Files, Has.Count.EqualTo(1));
            Assert.That(recipe.Files.First().Action, Is.EqualTo(FileUpdateAction.Download));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void UpdateTo_WithAnIncorrectDelta_SetTheActionAsDownload()
        {
            var fileOriginal = new VersionManifestFile("arquivo1.txt", "333", 1000);
            var fileUpdate = new VersionManifestFile("arquivo1.txt", "123", 1000, new[]
            {
                new VersionManifestDeltaFile("deltas\\arquivo1.txt", "444", 10)
            });
            var currentManifest = new VersionManifest(new Version("1.0.0"), new[] { fileOriginal });
            var updateManifest = new VersionManifest(new Version("2.0.0"), new[] { fileUpdate });

            var recipe = currentManifest.UpdateTo(updateManifest);

            Assert.That(recipe.Files, Has.Count.EqualTo(1));
            Assert.That(recipe.Files.First().Action, Is.EqualTo(FileUpdateAction.Download));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void UpdateTo_WithADeltaAvailable_SetTheActionAsDownloadDelta()
        {
            var fileOriginal = new VersionManifestFile("arquivo1.txt", "333", 1000);
            var fileUpdate = new VersionManifestFile("arquivo1.txt", "123", 1000, new[]
            {
                new VersionManifestDeltaFile("deltas\\arquivo1.txt", "333", 10)                                                                                      
            });
            var currentManifest = new VersionManifest(new Version("1.0.0"), new[] { fileOriginal });
            var updateManifest = new VersionManifest(new Version("2.0.0"), new[] { fileUpdate });

            var recipe = currentManifest.UpdateTo(updateManifest);

            Assert.That(recipe.Files, Has.Count.EqualTo(1));
            Assert.That(recipe.Files.First().Action, Is.EqualTo(FileUpdateAction.DownloadDelta));
        }

        [Test] // ReSharper disable once InconsistentNaming
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
            Assert.That(manifest.Files, Has.Count.EqualTo(2));
            Assert.That(manifest.Files.ElementAt(0).Name, Is.EqualTo("test1.txt"));
            Assert.That(manifest.Files.ElementAt(0).Checksum, Is.EqualTo("b94f6f125c79e3a5ffaa826f584c10d52ada669e6762051b826b55776d05aed2"));
            Assert.That(manifest.Files.ElementAt(0).Size, Is.EqualTo(9));
            Assert.That(manifest.Files.ElementAt(1).Name, Is.EqualTo("abc\\test2.txt"));
            Assert.That(manifest.Files.ElementAt(1).Checksum, Is.EqualTo("4895ecc6f0c011072af486ea30a1239caa1b297fb61ecaca8ac94d9c2071be22"));
            Assert.That(manifest.Files.ElementAt(1).Size, Is.EqualTo(12));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void SaveToFile_CreatesTheFile()
        {
            var path = Path.GetTempFileName();

            var manifest = VersionManifest.LoadVersionData(new Version("1.0.0"), @"<manifest/>");
            manifest.SaveToFile(path);

            Assert.That(File.Exists(path), Is.True);
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void SaveToFile_SavesAllTheInfoToTheFile()
        {
            var path = Path.GetTempFileName();

            var originalManifest = VersionManifest.LoadVersionData(new Version("1.0.0"), @"
                <manifest>
                    <files>
                        <file name='test1.txt' checksum='algo111' size='1000' >
                            <delta from='AABBCC' size='500' file='teste1.txt.1.deploy' />
                            <delta from='CCDDEE' size='400' file='teste1.txt.2.deploy' />
                        </file>
                        <file name='test2.txt' checksum='algo222' size='2000' />
                    </files>
                </manifest>");
            originalManifest.SaveToFile(path);

            var savedManifest = VersionManifest.LoadVersionData(new Version("1.0.0"), File.ReadAllText(path));
            Assert.That(savedManifest, Is.Not.Null);
            Assert.That(savedManifest.Files, Has.Count.EqualTo(2));
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
