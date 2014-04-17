﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using System.IO;

namespace AppUpdater.Utils
{
    public class DataCompressor
    {
        public static void Compress(Stream inputStream, Stream outputStream)
        {
            using (var zip = new GZipStream(outputStream, CompressionMode.Compress))
            {
                inputStream.CopyTo(zip);
            }
        }

        public static byte[] Compress(byte[] data)
        {
            if (data == null) // TODO review
            {
                return null;
            }

            using (var msInput = new MemoryStream())
            {
                msInput.Write(data, 0, data.Length);
                msInput.Position = 0;

                using (var msOutput = new MemoryStream())
                {
                    Compress(msInput, msOutput);

                    return msOutput.ToArray();
                }
            }
        }

        public static void Decompress(Stream inputStream, Stream outputStream)
        {
            using (var zip = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                zip.CopyTo(outputStream);
            }
        }

        public static byte[] Decompress(byte[] data)
        {
            if (data == null) // TODO review
            {
                return null;
            }

            using (var msInput = new MemoryStream())
            {
                msInput.Write(data, 0, data.Length);
                msInput.Position = 0;

                using (var msOutput = new MemoryStream())
                {
                    Decompress(msInput, msOutput);

                    return msOutput.ToArray();
                }
            }
        }
    }
}
