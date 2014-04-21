namespace AppUpdater
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Timer = System.Threading.Timer;

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

        public void Start()
        {
            Start(null);
        }

        public void Start(TaskScheduler scheduler)
        {
            if (Timer != null)
                return;
            StartImpl(scheduler ?? (SynchronizationContext.Current != null 
                                    ? TaskScheduler.FromCurrentSynchronizationContext() 
                                    : TaskScheduler.Default));
        }

        void StartImpl(TaskScheduler scheduler)
        {
            var stopTokenSource = new CancellationTokenSource();
            Timer = new Timer(delegate { CheckForUpdates(stopTokenSource.Token, scheduler); }, 
                              null, TimeSpan.Zero, CheckInterval);
            this.stopTokenSource = stopTokenSource;
        }

        void CheckForUpdates(CancellationToken cancellationToken, TaskScheduler scheduler)
        {
            try
            {   // ReSharper disable once MethodSupportsCancellation
                CheckForUpdatesAsync(cancellationToken, scheduler).Wait();
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

        Task CheckForUpdatesAsync(CancellationToken cancellationToken, TaskScheduler scheduler)
        {
            return TaskHelpers.Iterate(CheckForUpdatesImpl(cancellationToken, scheduler), cancellationToken);
        }

        IEnumerable<Task> CheckForUpdatesImpl(CancellationToken cancellationToken, TaskScheduler scheduler)
        {
            log.Debug("Checking for updates.");
            var checkTask = updateManager.CheckForUpdateAsync(cancellationToken);
            yield return checkTask;
            var updateInfo = checkTask.Result;
            if (!updateInfo.HasUpdate)
            {
                log.Debug("No updates found.");
                yield break;
            }
            log.Debug("Updates found. Installing new files.");
            yield return updateManager.DoUpdateAsync(updateInfo, cancellationToken);
            log.Debug("Update is ready.");
            var updated = Updated;
            if (updated != null)
                Task.Factory.StartNew(() => updated(this, EventArgs.Empty), cancellationToken, TaskCreationOptions.None, scheduler);
        }
    }
}
