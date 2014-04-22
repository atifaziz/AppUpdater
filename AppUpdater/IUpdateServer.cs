namespace AppUpdater
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IUpdateServer
    {
        Task<Version> GetCurrentVersionAsync(CancellationToken cancellationToken);
        Task<VersionManifest> GetManifestAsync(Version version, CancellationToken cancellationToken);
        Task<byte[]> DownloadFileAsync(Version version, string filename, CancellationToken cancellationToken);
    }
}
