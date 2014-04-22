namespace AppUpdater
{
    public class VersionManifestDeltaFile
    {
        public string Filename { get; private set; }
        public string Checksum { get; private set; }
        public long Size { get; private set; }

        public VersionManifestDeltaFile(string filename, string checksum, long size)
        {
            Filename = filename;
            Checksum = checksum;
            Size = size;
        }
    }
}
