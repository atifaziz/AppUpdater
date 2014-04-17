using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using AppUpdater.Log;

namespace AppUpdater.Tests
{
    [TestFixture]
    public class LoggerTests
    {
        [SetUp]
        public void Setup()
        {
            Logger.LoggerProvider = (type) => new TestLog(type);
        }

        [Test]
        public void For_ReturnsTheLoggerForTheType()
        {
            var log = Logger.For(typeof(LoggerTests));

            Assert.That(log, Is.InstanceOf<TestLog>());
            Assert.That((log as TestLog).Type, Is.EqualTo(typeof(LoggerTests)));
        }

        [Test]
        public void ForT_ReturnsTheLoggerForTheType()
        {
            var log = Logger.For<LoggerTests>();

            Assert.That(log, Is.InstanceOf<TestLog>());
            Assert.That((log as TestLog).Type, Is.EqualTo(typeof(LoggerTests)));
        }

        [Test]
        public void For_WithAProviderThatReturnsAnError_ReturnsTheEmptyLog()
        {
            Logger.LoggerProvider = (type) => { throw new Exception("Error"); };

            var log = Logger.For(typeof(LoggerTests));

            Assert.That(log, Is.InstanceOf<EmptyLog>());
        }

        [Test]
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
