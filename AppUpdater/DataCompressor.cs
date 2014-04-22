namespace AppUpdater
{
    using System;
    using System.IO.Compression;
    using System.IO;

    public class DataCompressor
    {
        public static void Compress(Stream input, Stream output)
        {
            using (var zip = new GZipStream(output, CompressionMode.Compress))
                input.CopyTo(zip);
        }

        public static byte[] Compress(byte[] data)
        {
            return GZip(data, Compress);
        }

        public static void Decompress(Stream input, Stream output)
        {
            using (var zip = new GZipStream(input, CompressionMode.Decompress))
                zip.CopyTo(output);
        }

        public static byte[] Decompress(byte[] data)
        {
            return GZip(data, Decompress);
        }

        static byte[] GZip(byte[] data, Action<Stream, Stream> mode)
        {
            if (data == null) // TODO review
                return null;

            using (var input = new MemoryStream(data))
            using (var output = new MemoryStream())
            {
                mode(input, output);
                return output.ToArray();
            }
        }
    }
}
