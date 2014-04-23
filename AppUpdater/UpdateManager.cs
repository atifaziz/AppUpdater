namespace AppUpdater
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    #endregion

    public class UpdateManager : IUpdateManager
    {
        readonly ILog log = Logger.For<UpdateManager>();

        protected bool Initialized { get; private set; }
        protected IUpdateServer UpdateServer { get; private set; }
        protected ILocalStructureManager LocalStructureManager { get; private set; }
        protected IUpdaterChef UpdaterChef { get; private set; }

        public Version CurrentVersion { get; private set; }

        static UpdateManager defaultInstance;

        public static UpdateManager Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    var baseDir = Path.Combine(Path.GetDirectoryName(typeof(UpdateManager).Assembly.Location), "..\\");
                    ILocalStructureManager manager = new LocalStructureManager(baseDir);
                    IUpdateServer updateServer = new UpdateServer(manager.GetUpdateServerUrl());
                    defaultInstance = new UpdateManager(updateServer, manager, new UpdaterChef(manager, updateServer));
                    defaultInstance.Initialize();
                }

                return defaultInstance;
            }
        }

        public UpdateManager(IUpdateServer updateServer, ILocalStructureManager localStructureManager, IUpdaterChef updaterChef)
        {
            if (updateServer == null) throw new ArgumentNullException("updateServer");
            if (localStructureManager == null) throw new ArgumentNullException("localStructureManager");
            if (updaterChef == null) throw new ArgumentNullException("updaterChef");

            UpdateServer = updateServer;
            LocalStructureManager = localStructureManager;
            UpdaterChef = updaterChef;
        }

        public virtual void Initialize()
        {
            CurrentVersion = LocalStructureManager.GetCurrentVersion();
            Initialized = true;
        }

        public virtual Task<UpdateInfo> CheckForUpdateAsync(CancellationToken cancellationToken)
        {
            CheckInitialized();            
            return UpdateServer.GetCurrentVersionAsync(cancellationToken).ContinueWith(t =>
            {
                var serverCurrentVersion = t.Result;
                var hasUpdate = CurrentVersion != serverCurrentVersion;
                return new UpdateInfo(hasUpdate, serverCurrentVersion);

            }, cancellationToken);
        }

        public virtual Task DoUpdateAsync(UpdateInfo updateInfo, CancellationToken cancellationToken)
        {
            CheckInitialized();
            return TaskHelpers.Iterate(DoUpdateAsyncImpl(updateInfo, cancellationToken), cancellationToken);
        }

        IEnumerable<Task> DoUpdateAsyncImpl(UpdateInfo updateInfo, CancellationToken cancellationToken)
        {
            var currentVersionManifest = LocalStructureManager.LoadManifest(CurrentVersion);
            Task<VersionManifest> manifestTask;
            yield return manifestTask = UpdateServer.GetManifestAsync(updateInfo.Version, cancellationToken);
            var recipe = currentVersionManifest.UpdateTo(manifestTask.Result);
            yield return UpdaterChef.CookAsync(recipe, cancellationToken);
            LocalStructureManager.SetLastValidVersion(LocalStructureManager.GetExecutingVersion());
            LocalStructureManager.SetCurrentVersion(updateInfo.Version);
            CurrentVersion = updateInfo.Version;
            DeleteOldVersions();
        }

        void DeleteOldVersions()
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

        void CheckInitialized()
        {
            if (Initialized) return;
            var message = string.Format("{0} has not been initialzed.", GetType().FullName);
            throw new InvalidOperationException(message);
        }
    }
}
