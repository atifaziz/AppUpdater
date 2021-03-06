﻿namespace AppUpdater.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class VersionManifestFileTests
    {
        private VersionManifestFile file;

        [SetUp]
        public void SetUp()
        {
            file = new VersionManifestFile("", "", 1, new[]
            {
                new VersionManifestDeltaFile("aa.bb", "AAA", 1),
                new VersionManifestDeltaFile("bb.bb", "BBB", 1),
            });
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void GetDeltaFrom_WithValidChecksum_ReturnsTheItem()
        {
            var delta = file.GetDeltaFrom("AAA");

            Assert.That(delta, Is.Not.Null);
            Assert.That(delta.FileName, Is.EqualTo("aa.bb"));
        }

        [Test] // ReSharper disable once InconsistentNaming
        public void GetDeltaFrom_WithInvalidChecksum_ReturnsNull()
        {
            var delta = file.GetDeltaFrom("MMMMM");

            Assert.That(delta, Is.Null);
        }
    }
}
