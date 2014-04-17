using System;
using System.Threading;
using AppUpdater.Logging;

namespace AppUpdater
{
    public class AutoUpdater
    {
        private readonly ILog log = Logger.For<AutoUpdater>();
        private readonly IUpdateManager updateManager;
        private Thread thread;

        public TimeSpan CheckInterval { get; set; }

        public event EventHandler Updated;

        public AutoUpdater(IUpdateManager updateManager)
        {
            this.updateManager = updateManager;
            CheckInterval = TimeSpan.FromSeconds(3600);
        }

        public void Start()
        {
            if (thread == null || !thread.IsAlive)
            {
                log.Debug("Starting the AutoUpdater.");
                thread = new Thread(CheckForUpdates);
                thread.IsBackground = true;
                thread.Start();
            }
        }

        public void Stop()
        {
            if (thread != null && thread.IsAlive)
            {
                log.Debug("Stopping the AutoUpdater.");
                thread.Abort();
            }

            thread = null;
        }

        private void CheckForUpdates()
        {
            while (true)
            {
                try
                {
                    log.Debug("Checking for updates.");
                    var updateInfo = updateManager.CheckForUpdateAsync(CancellationToken.None).Result;
                    if (updateInfo.HasUpdate)
                    {
                        log.Debug("Updates found. Installing new files.");
                        updateManager.DoUpdateAsync(updateInfo, CancellationToken.None).Wait();
                        log.Debug("Update is ready.");
                        RaiseUpdated();
                    }
                    else
                    {
                        log.Debug("No updates found.");
                    }
                }
                catch (ThreadAbortException)
                {
                    throw;
                }
                catch (Exception err)
                {
                    log.Error(err.Message);
                }

                Thread.Sleep((int) CheckInterval.TotalMilliseconds);
            }
        }

        private void RaiseUpdated()
        {
            var updated = Updated;
            if (updated != null)
            {
                updated(this, EventArgs.Empty);
            }
        }
    }
}
