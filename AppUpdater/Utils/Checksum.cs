using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace AppUpdater.Utils
{
    public static class Checksum
    {
        public static string Calculate(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var sha = new SHA256Managed();
            var checksum = sha.ComputeHash(stream);
            var sb = new StringBuilder();
            foreach (var b in checksum)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }
    }
}
