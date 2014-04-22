namespace AppUpdater
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Timer = System.Threading.Timer;

    #endregion

    public sealed class AutoUpdater
    {
        static readonly ILog log = Logger.For<AutoUpdater>();
        static readonly object busyLock = new object();
        static bool busy;
        static readonly object currentLock = new object();
        static AutoUpdater current;

        readonly CancellationTokenSource stopper;
        readonly Timer timer;
        readonly IUpdateManager updateManager;
        readonly TimeSpan checkInterval;
        readonly TaskScheduler scheduler;
        readonly EventHandler updated;

        AutoUpdater(IUpdateManager updateManager, TimeSpan checkInterval, Timer timer, EventHandler updated, TaskScheduler scheduler)
        {
            stopper = new CancellationTokenSource();
            this.timer = timer;
            this.updateManager = updateManager;
            this.checkInterval = checkInterval;
            this.scheduler = scheduler;
            this.updated = updated;
        }

        public sealed class Setup
        {
            TimeSpan checkInterval;

            public IUpdateManager UpdateManager { get; set; }

            public TimeSpan CheckInterval
            {
                get { return checkInterval; }
                set
                {
                    if ((int) value.TotalMilliseconds <= 0) throw new ArgumentOutOfRangeException("value", value, "Interval cannot be zero or negative.");
                    checkInterval = value;
                }
            }

            public TaskScheduler Scheduler { get; set; }
            public event EventHandler Updated;

            public Setup(IUpdateManager updateManager) : 
                this(updateManager, TimeSpan.FromHours(1)) {}

            public Setup(IUpdateManager updateManager, TimeSpan checkInterval)
            {
                if (updateManager == null) throw new ArgumentNullException("updateManager");
                UpdateManager = updateManager;
                CheckInterval = checkInterval;
            }

            internal EventHandler UpdatedInternal { get { return Updated; } }
        }

        public static void Start(Setup setup)
        {
            if (setup == null) throw new ArgumentNullException("setup");

            AutoUpdater updater;

            lock (currentLock)
            {
                if (current != null)
                    return;

                var timerUpdater = new AutoUpdater[1];
                updater = timerUpdater[0] = new AutoUpdater(
                    setup.UpdateManager,
                    setup.CheckInterval,
                    new Timer(_ => timerUpdater[0].CheckForUpdatesThenReschedule()),
                    setup.UpdatedInternal ?? delegate { },
                    setup.Scheduler ?? (SynchronizationContext.Current != null
                                        ? TaskScheduler.FromCurrentSynchronizationContext()
                                        : TaskScheduler.Default));

                ReplaceCurrent(updater);
            }

            updater.timer.Change(TimeSpan.Zero, TimeSpan.Zero); // immediate!
        }

        static void ReplaceCurrent(AutoUpdater value)
        {
            lock (currentLock)
            {
                var old = current;
                current = value;
                if (old == null)
                    return;
                old.stopper.Cancel();
                old.timer.Dispose();
            }
        }

        public static void Stop()
        {
            lock (currentLock)
            {
                if (current == null)
                    return;
                ReplaceCurrent(null);
            }
            log.Debug("Stopping the AutoUpdater.");
        }

        void CheckForUpdatesThenReschedule()
        {            
            var reschedule = false;
            try
            {   // ReSharper disable once MethodSupportsCancellation
                CheckForUpdatesAsync().Wait();
                reschedule = true;
            }
            catch (AggregateException ae)
            {
                var be = ae.GetBaseException();
                log.Error(be.Message);
                if (ae.Flatten().InnerExceptions.Any(ie => ie is StackOverflowException || ie is ThreadAbortException))
                    throw;
                reschedule = true;
            }
            finally
            {
                if (reschedule && !stopper.IsCancellationRequested)
                    timer.Change(checkInterval, TimeSpan.Zero);
            }
        }


        Task CheckForUpdatesAsync()
        {
            return TaskHelpers.Iterate(CheckForUpdatesImpl(), stopper.Token);
        }

        IEnumerable<Task> CheckForUpdatesImpl()
        {
            lock (busyLock)
            {
                if (busy)
                    yield break;
                busy = true;
            }

            try
            {
                log.Debug("Checking for updates.");
                var cancellationToken = stopper.Token;
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
                Task.Factory.StartNew(() => updated(this, EventArgs.Empty), cancellationToken, TaskCreationOptions.None, scheduler);
            }
            finally
            {
                lock (busyLock) { busy = false; }
            }
        }
    }
}
