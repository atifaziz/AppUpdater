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

            Assert.That(checksum, Is.EqualTo("B94F6F125C79E3A5FFAA826F584C10D52ADA669E6762051B826B55776D05AED2"));
        }
    }
}
