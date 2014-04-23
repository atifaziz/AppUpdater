namespace AppUpdater
{
    using System;
    using System.Security.Cryptography;
    using System.IO;

    public static class Checksum
    {
        public static string Calculate(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            var sha = new SHA256Managed();
            var hash = sha.ComputeHash(stream);
            return BitConverter.ToString(hash)
                               .Replace("-", string.Empty)
                               .ToLowerInvariant();
        }
    }
}
