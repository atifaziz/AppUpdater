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
        AutoUpdater.Setup setup;
        IUpdateManager updateManager;

        [SetUp]
        public void Setup()
        {
            updateManager = MockRepository.GenerateStub<IUpdateManager>();
            setup = new AutoUpdater.Setup(updateManager);
        }

        [TearDown]
        public void TearDown()
        {
            AutoUpdater.Stop();
        }

        [Test]
        public void Ctor_SetsTheDefaultCheckIntervalTo1hour()
        {
            Assert.That(setup.CheckInterval, Is.EqualTo(TimeSpan.FromHours(1)));
        }

        [Test]
        public void Start_CheckForUpdatesOnStart()
        {
            setup.CheckInterval = TimeSpan.FromSeconds(10000);

            AutoUpdater.Start(setup);
            Thread.Sleep(1000);

            updateManager.AssertWasCalled(x => x.CheckForUpdateAsync(Arg<CancellationToken>.Is.Anything));
        }

        [Test]
        public void Start_DoNotCheckBeforeTime()
        {
            setup.CheckInterval = TimeSpan.FromSeconds(2);

            AutoUpdater.Start(setup);
            Thread.Sleep(1000);

            updateManager.AssertWasCalled(x => x.CheckForUpdateAsync(Arg<CancellationToken>.Is.Anything), s => s.Repeat.Once());
        }

        [Test]
        public void Start_StoppedUpdater_CheckAfterWaitTime()
        {
            setup.CheckInterval = TimeSpan.FromSeconds(1);

            AutoUpdater.Start(setup);
            Thread.Sleep(1500);

            updateManager.AssertWasCalled(x => x.CheckForUpdateAsync(Arg<CancellationToken>.Is.Anything), s => s.Repeat.Twice());
        }

        [Test]
        public void Start_StartedUpdater_DoNotStartAgain()
        {
            setup.CheckInterval = TimeSpan.FromSeconds(1);

            AutoUpdater.Start(setup);
            Thread.Sleep(100);
            AutoUpdater.Start(setup);
            Thread.Sleep(100);

            updateManager.AssertWasCalled(x => x.CheckForUpdateAsync(Arg<CancellationToken>.Is.Anything), s => s.Repeat.Once());
        }

        [Test]
        public void Stop_StartedUpdater_StopsTheChecks()
        {
            setup.CheckInterval = TimeSpan.FromSeconds(1);

            AutoUpdater.Start(setup);
            Thread.Sleep(300);
            AutoUpdater.Stop();
            Thread.Sleep(1500);

            updateManager.AssertWasCalled(x => x.CheckForUpdateAsync(Arg<CancellationToken>.Is.Anything), s => s.Repeat.Once());
        }

        [Test]
        public void Stop_StoppedUpdater_DoNothing()
        {
            setup.CheckInterval = TimeSpan.FromSeconds(1);

            AutoUpdater.Start(setup);
            AutoUpdater.Stop();
            AutoUpdater.Stop();
        }

        [Test]
        public void Updated_IsCalledAfterUpdate()
        {
            var called = false;
            var info = new UpdateInfo(true, new Version("2.0.0"));
            updateManager.Stub(x => x.CheckForUpdateAsync(Arg<CancellationToken>.Is.Anything)).Return(TaskHelpers.FromResult(info));
            updateManager.Stub(x => x.DoUpdateAsync(Arg<UpdateInfo>.Is.Equal(info), Arg<CancellationToken>.Is.Anything)).Return(TaskHelpers.Completed());
            setup.Updated += (sender, e) => called = true;

            AutoUpdater.Start(setup);
            Thread.Sleep(100);

            Assert.That(called, Is.True);
        }
    }
}
