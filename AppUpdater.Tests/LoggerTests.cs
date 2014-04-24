namespace AppUpdater.Tests
{
    using System;
    using NUnit.Framework;
    using Logging;

    [TestFixture]
    public class LoggerTests
    {
        [SetUp]
        public void Setup()
        {
            Logger.LoggerProvider = (type) => new TestLog(type);
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void For_ReturnsTheLoggerForTheType()
        {
            var log = Logger.For(typeof(LoggerTests));

            Assert.That(log, Is.InstanceOf<TestLog>());
            Assert.That((log as TestLog).Type, Is.EqualTo(typeof(LoggerTests)));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void ForT_ReturnsTheLoggerForTheType()
        {
            var log = Logger.For<LoggerTests>();

            Assert.That(log, Is.InstanceOf<TestLog>());
            Assert.That((log as TestLog).Type, Is.EqualTo(typeof(LoggerTests)));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void For_WithAProviderThatReturnsAnError_ReturnsTheEmptyLog()
        {
            Logger.LoggerProvider = (type) => { throw new Exception("Error"); };

            var log = Logger.For(typeof(LoggerTests));

            Assert.That(log, Is.InstanceOf<EmptyLog>());
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void ForT_WithAProviderThatReturnsAnError_ReturnsTheEmptyLog()
        {
            Logger.LoggerProvider = (type) => { throw new Exception("Error"); };

            var log = Logger.For<LoggerTests>();

            Assert.That(log, Is.InstanceOf<EmptyLog>());
        }

        public class TestLog : ILog
        {
            public Type Type { get; private set; }

            public TestLog(Type type) { Type = type; }

            public void Info (string message, params object[] values) { throw new NotImplementedException(); }
            public void Warn (string message, params object[] values) { throw new NotImplementedException(); }
            public void Error(string message, params object[] values) { throw new NotImplementedException(); }
            public void Debug(string message, params object[] values) { throw new NotImplementedException(); }
        }
    }
}
