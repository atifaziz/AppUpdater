namespace AppUpdater
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Timer = System.Timers.Timer;

    #endregion

    public class AutoUpdater
    {
        readonly ILog log = Logger.For<AutoUpdater>();
        readonly IUpdateManager updateManager;
        Timer timer;
        CancellationTokenSource stopTokenSource;

        public TimeSpan CheckInterval { get; set; }

        Timer Timer
        {
            get { return timer; }
            set
            {
                if (timer != null)
                    timer.Dispose();
                timer = value;
            }
        }

        public event EventHandler Updated;

        public AutoUpdater(IUpdateManager updateManager)
        {
            this.updateManager = updateManager;
            CheckInterval = TimeSpan.FromHours(1);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Timer != null 
                 ? TaskHelpers.Completed() 
                 : TaskHelpers.Iterate(StartImpl(cancellationToken), cancellationToken);
        }

        IEnumerable<Task> StartImpl(CancellationToken cancellationToken)
        {
            yield return Task.Factory.StartNew(() => CheckForUpdates(cancellationToken), cancellationToken);
            var stopTokenSource = new CancellationTokenSource();
            var newTimer = new Timer(CheckInterval.TotalMilliseconds);
            newTimer.Elapsed += delegate { CheckForUpdates(stopTokenSource.Token); };
            newTimer.Enabled = true;
            this.stopTokenSource = stopTokenSource;
            Timer = newTimer;
        }

        void CheckForUpdates(CancellationToken cancellationToken)
        {
            try
            {   // ReSharper disable once MethodSupportsCancellation
                CheckForUpdatesAsync(cancellationToken).Wait();
            }
            catch (Exception e)
            {
                log.Error(e.Message);
                if (e is StackOverflowException || e is ThreadAbortException)
                    throw;
            }
        }

        public void Stop()
        {
            var timer = this.timer;
            this.timer = null;
            if (timer != null)
            {
                var stopTokenSource = this.stopTokenSource;
                this.stopTokenSource = null;
                stopTokenSource.Cancel();
                stopTokenSource.Dispose();
                timer.Dispose();
            }
            log.Debug("Stopping the AutoUpdater.");
        }

        Task CheckForUpdatesAsync(CancellationToken token)
        {
            return TaskHelpers.Iterate(CheckForUpdatesImpl(token), token);
        }

        IEnumerable<Task> CheckForUpdatesImpl(CancellationToken token)
        {
            log.Debug("Checking for updates.");
            var checkTask = updateManager.CheckForUpdateAsync(token);
            yield return checkTask;
            var updateInfo = checkTask.Result;
            if (!updateInfo.HasUpdate)
            {
                log.Debug("No updates found.");
                yield break;
            }
            log.Debug("Updates found. Installing new files.");
            yield return updateManager.DoUpdateAsync(updateInfo, token);
            log.Debug("Update is ready.");
            RaiseUpdated();
        }

        void RaiseUpdated()
        {
            var updated = Updated;
            if (updated != null)
                updated(this, EventArgs.Empty);
        }
    }
}
