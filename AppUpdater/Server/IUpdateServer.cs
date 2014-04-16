using AppUpdater.Manifest;

namespace AppUpdater.Server
{
    using System;

    public interface IUpdateServer
    {
        Version GetCurrentVersion();
        VersionManifest GetManifest(Version version);
        byte[] DownloadFile(Version version, string filename);
    }
}
