namespace AppUpdater
{
    public class VersionManifestDeltaFile
    {
        public string FileName { get; private set; }
        public string Checksum { get; private set; }
        public long Size { get; private set; }

        public VersionManifestDeltaFile(string fileName, string checksum, long size)
        {
            FileName = fileName;
            Checksum = checksum;
            Size = size;
        }
    }
}
