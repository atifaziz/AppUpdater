using System;
using AppUpdater.LocalStructure;
using AppUpdater.Recipe;
using AppUpdater.Server;
using AppUpdater.Chef;
using NUnit.Framework;
using Rhino.Mocks;
using AppUpdater.Utils;

namespace AppUpdater.Tests
{
    [TestFixture]
    public class UpdaterChefTests
    {
        private UpdaterChef updaterChef;
        private ILocalStructureManager localStructureManager;
        private IUpdateServer updateServer;
        static readonly Version v1 = new Version("1.0.0");
        static readonly Version v2 = new Version("2.0.0");

        [SetUp]
        public void Setup()
        {
            localStructureManager = MockRepository.GenerateStub<ILocalStructureManager>();
            updateServer = MockRepository.GenerateStub<IUpdateServer>();
            updaterChef = new UpdaterChef(localStructureManager, updateServer);
        }

        [Test]
        public void Cook_WithAVersionAlreadyDownloaded_CreatesTheVersionDirectory()
        {
            localStructureManager.Stub(x => x.HasVersionFolder(v2)).Return(true);
            var updateRecipe = new UpdateRecipe(v2, v1, new UpdateRecipeFile[0]);
            updaterChef.Cook(updateRecipe);

            localStructureManager.AssertWasCalled(x => x.DeleteVersionDir(v2));
        }

        [Test]
        public void Cook_CreatesTheVersionDirectory()
        {
            var updateRecipe = new UpdateRecipe(v2, v1, new UpdateRecipeFile[0]);
            updaterChef.Cook(updateRecipe);

            localStructureManager.AssertWasCalled(x => x.CreateVersionDir(v2));
        }

        [Test]
        public void Cook_CopyExistingFiles()
        {
            var file1 = new UpdateRecipeFile("test1.txt", "123", 100, FileUpdateAction.Copy, null);
            var file2 = new UpdateRecipeFile("test2.txt", "123", 100, FileUpdateAction.Download, "test2.txt.deploy");
            var updateRecipe = new UpdateRecipe(v2, v1, new UpdateRecipeFile[] { file1, file2 });
            updaterChef.Cook(updateRecipe);

            localStructureManager.AssertWasCalled(x => x.CopyFile(v1, v2, "test1.txt"));
        }

        [Test]
        public void Cook_SavesNewFiles()
        {
            var data = new byte[]{1,2,3,4,5};
            updateServer.Stub(x => x.DownloadFile(v2, "test2.txt.deploy")).Return(DataCompressor.Compress(data));
            var file1 = new UpdateRecipeFile("test1.txt", "123", 100, FileUpdateAction.Copy, null);
            var file2 = new UpdateRecipeFile("test2.txt", "123", 100, FileUpdateAction.Download, "test2.txt.deploy");
            var updateRecipe = new UpdateRecipe(v2, v1, new UpdateRecipeFile[] { file1, file2 });

            updaterChef.Cook(updateRecipe);

            localStructureManager.AssertWasCalled(x => x.SaveFile(v2, "test2.txt", data));
        }

        [Test]
        public void Cook_ApplyDeltaInModifiedFiles()
        {
            var data = new byte[] { 1, 2, 3, 4, 5 };
            updateServer.Stub(x => x.DownloadFile(v2, "test2.txt.deploy")).Return(data);
            var file1 = new UpdateRecipeFile("test1.txt", "123", 100, FileUpdateAction.Copy, null);
            var file2 = new UpdateRecipeFile("test2.txt", "123", 100, FileUpdateAction.DownloadDelta, "test2.txt.deploy");
            var updateRecipe = new UpdateRecipe(v2, v1, new UpdateRecipeFile[] { file1, file2 });

            updaterChef.Cook(updateRecipe);

            localStructureManager.AssertWasCalled(x => x.ApplyDelta(v1, v2, "test2.txt", data));
        }
    }
}
