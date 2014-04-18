namespace AppUpdater.Tests
{
    #region Imports

    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Rhino.Mocks;

    #endregion

    [TestFixture]
    public class AutoUpdaterTests
    {
        private AutoUpdater autoUpdater;
        private IUpdateManager updateManager;

        [SetUp]
        public void Setup()
        {
            updateManager = MockRepository.GenerateStub<IUpdateManager>();
            autoUpdater = new AutoUpdater(updateManager);
        }

        [Test]
        public void Ctor_SetsTheDefaultCheckIntervalTo1hour()
        {
            Assert.That(autoUpdater.CheckInterval, Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void Start_CheckForUpdatesOnStart()
        {
            autoUpdater.CheckInterval = TimeSpan.FromSeconds(10000);

            autoUpdater.StartAsync(CancellationToken.None).Wait();
            Thread.Sleep(1000);

            updateManager.AssertWasCalled(x => x.CheckForUpdateAsync(Arg<CancellationToken>.Is.Anything));
        }

        [Test]
        public void Start_DoNotCheckBeforeTime()
        {
            autoUpdater.CheckInterval = TimeSpan.FromSeconds(2);

            autoUpdater.StartAsync(CancellationToken.None).Wait();
            Thread.Sleep(1000);

            updateManager.AssertWasCalled(x => x.CheckForUpdateAsync(Arg<CancellationToken>.Is.Anything), s => s.Repeat.Once());
        }

        [Test]
        public void Start_StoppedUpdater_CheckAfterWaitTime()
        {
            autoUpdater.CheckInterval = TimeSpan.FromSeconds(1);

            autoUpdater.StartAsync(CancellationToken.None).Wait();
            Thread.Sleep(1500);

            updateManager.AssertWasCalled(x => x.CheckForUpdateAsync(Arg<CancellationToken>.Is.Anything), s => s.Repeat.Twice());
        }

        [Test]
        public void Start_StartedUpdater_DoNotStartAgain()
        {
            autoUpdater.CheckInterval = TimeSpan.FromSeconds(1);

            autoUpdater.StartAsync(CancellationToken.None).Wait();
            Thread.Sleep(100);
            autoUpdater.StartAsync(CancellationToken.None).Wait();
            Thread.Sleep(100);

            updateManager.AssertWasCalled(x => x.CheckForUpdateAsync(CancellationToken.None), s => s.Repeat.Once());
        }

        [Test]
        public void Stop_StartedUpdater_StopsTheChecks()
        {
            autoUpdater.CheckInterval = TimeSpan.FromSeconds(1);

            autoUpdater.StartAsync(CancellationToken.None).Wait();
            Thread.Sleep(300);
            autoUpdater.Stop();
            Thread.Sleep(1500);

            updateManager.AssertWasCalled(x => x.CheckForUpdateAsync(CancellationToken.None), s => s.Repeat.Once());
        }

        [Test]
        public void Stop_StoppedUpdater_DoNothing()
        {
            autoUpdater.CheckInterval = TimeSpan.FromSeconds(1);

            autoUpdater.Stop();
        }

        [Test]
        public void Updated_IsCalledAfterUpdate()
        {
            var called = false;
            var info = new UpdateInfo(true, new Version("2.0.0"));
            updateManager.Stub(x => x.CheckForUpdateAsync(CancellationToken.None)).Return(TaskHelpers.FromResult(info));
            updateManager.Stub(x => x.DoUpdateAsync(info, CancellationToken.None)).Return(TaskHelpers.Completed());
            autoUpdater.Updated += (sender, e) => called = true;

            autoUpdater.StartAsync(CancellationToken.None).Wait();
            Thread.Sleep(100);

            Assert.That(called, Is.True);
        }
    }
}
