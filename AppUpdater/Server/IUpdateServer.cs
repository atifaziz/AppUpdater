namespace AppUpdater.Server
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Manifest;

    public interface IUpdateServer
    {
        Task<Version> GetCurrentVersionAsync(CancellationToken cancellationToken);
        Task<VersionManifest> GetManifestAsync(Version version, CancellationToken cancellationToken);
        Task<byte[]> DownloadFileAsync(Version version, string filename, CancellationToken cancellationToken);
    }
}
