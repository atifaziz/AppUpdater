namespace AppUpdater.Tests
{
    using System.IO;
    using System.Text;
    using NUnit.Framework;

    [TestFixture]
    public class ChecksumTests
    {
        [Test]
        public void Calculate_CreatesTheChecksum()
        {
            var data = Encoding.UTF8.GetBytes("some text");
            var stream = new MemoryStream();
            stream.Write(data, 0, data.Length);
            stream.Position = 0;

            var checksum = Checksum.Calculate(stream);

            Assert.That(checksum, Is.EqualTo("b94f6f125c79e3a5ffaa826f584c10d52ada669e6762051b826b55776d05aed2"));
        }
    }
}
