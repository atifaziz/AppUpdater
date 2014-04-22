using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using AppUpdater;
using System.IO;

namespace AppUpdater.Tests
{
    [TestFixture]
    public class DataCompressorTests
    {
        [Test]
        public void Compress_ValidData_ReturnsSmallerData()
        {
            var data = Encoding.UTF8.GetBytes(new string('a', 1000));

            var compressedData = DataCompressor.Compress(data);

            Assert.That(compressedData.Length, Is.LessThan(data.Length));
        }

        [Test]
        public void Decompress_ValidData_ReturnsTheOriginalData()
        {
            var originalData = Encoding.UTF8.GetBytes(new string('a', 1000));
            var compressedData = DataCompressor.Compress(originalData);

            var decompressedData = DataCompressor.Decompress(compressedData);

            Assert.That(decompressedData, Is.EqualTo(originalData));
        }

        [Test]
        public void Compress_NullData_ReturnsNullData()
        {
            byte[] data = null;

            var compressedData = DataCompressor.Compress(data);

            Assert.That(compressedData, Is.Null);
        }

        [Test]
        public void Decompress_NullData_ReturnsNullData()
        {
            byte[] compressedData = null;

            var decompressedData = DataCompressor.Decompress(compressedData);

            Assert.That(decompressedData, Is.Null);
        }
    }
}
