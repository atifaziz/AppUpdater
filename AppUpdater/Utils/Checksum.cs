namespace AppUpdater.Utils
{
    using System;
    using System.Security.Cryptography;
    using System.IO;

    public static class Checksum
    {
        public static string Calculate(string path)
        {
            using (var stream = File.OpenRead(path))
                return Calculate(stream);
        }

        public static string Calculate(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            var sha = new SHA256Managed();
            var checksum = sha.ComputeHash(stream);
            return BitConverter.ToString(checksum).Replace("-", string.Empty);
        }
    }
}
