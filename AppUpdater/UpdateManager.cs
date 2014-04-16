using System.IO;
using AppUpdater.LocalStructure;
using AppUpdater.Manifest;
using AppUpdater.Recipe;
using AppUpdater.Server;
using AppUpdater.Chef;
using AppUpdater.Log;
using System.Linq;
using System;

namespace AppUpdater
{
    public class UpdateManager : IUpdateManager
    {
        private ILog log = Logger.For<UpdateManager>();
        protected IUpdateServer UpdateServer { get; private set; }
        protected ILocalStructureManager LocalStructureManager { get; private set; }
        protected IUpdaterChef UpdaterChef { get; private set; }

        public Version CurrentVersion { get; private set; }

        private static UpdateManager defaultInstance;

        public static UpdateManager Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    var baseDir = Path.Combine(Path.GetDirectoryName(typeof(UpdateManager).Assembly.Location), "..\\");
                    ILocalStructureManager manager = new DefaultLocalStructureManager(baseDir);
                    IUpdateServer updateServer = new DefaultUpdateServer(manager.GetUpdateServerUri());
                    defaultInstance = new UpdateManager(updateServer, manager, new UpdaterChef(manager, updateServer));
                    defaultInstance.Initialize();
                }

                return defaultInstance;
            }
        }

        public UpdateManager(IUpdateServer updateServer, ILocalStructureManager localStructureManager, IUpdaterChef updaterChef)
        {
            this.UpdateServer = updateServer;
            this.LocalStructureManager = localStructureManager;
            this.UpdaterChef = updaterChef;
        }

        public virtual void Initialize()
        {
            this.CurrentVersion = LocalStructureManager.GetCurrentVersion();
        }

        public virtual UpdateInfo CheckForUpdate()
        {
            var serverCurrentVersion = UpdateServer.GetCurrentVersion();
            var hasUpdate = CurrentVersion != serverCurrentVersion;
            return new UpdateInfo(hasUpdate, serverCurrentVersion);
        }

        public virtual void DoUpdate(UpdateInfo updateInfo)
        {
            var currentVersionManifest = LocalStructureManager.LoadManifest(this.CurrentVersion);
            var newVersionManifest = UpdateServer.GetManifest(updateInfo.Version);
            var recipe = currentVersionManifest.UpdateTo(newVersionManifest);

            UpdaterChef.Cook(recipe);

            LocalStructureManager.SetLastValidVersion(LocalStructureManager.GetExecutingVersion());
            LocalStructureManager.SetCurrentVersion(updateInfo.Version);
            CurrentVersion = updateInfo.Version;

            DeleteOldVersions();
        }

        private void DeleteOldVersions()
        {
            var executingVersion = LocalStructureManager.GetExecutingVersion();
            var installedVersions = LocalStructureManager.GetInstalledVersions();
            var versionsInUse = new[] { executingVersion, CurrentVersion };

            foreach (var version in installedVersions.Except(versionsInUse))
            {
                try
                {
                    LocalStructureManager.DeleteVersionDir(version);
                }
                catch (Exception err)
                {
                    log.Error("Error deleting old version ({0}). {1}", version, err.Message);
                }
            }
        }
    }
}
