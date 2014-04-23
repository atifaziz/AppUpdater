namespace AppUpdater
{
    #region Imports

    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Logging;

    #endregion

    public class UpdateServer : IUpdateServer
    {
        private readonly Uri updateServerUrl;
        private readonly ILog log = Logger.For<UpdateServer>();

        public UpdateServer(Uri updateServerUrl)
        {
            this.updateServerUrl = updateServerUrl;
        }

        public virtual Task<Version> GetCurrentVersionAsync(CancellationToken cancellationToken)
        {
            return DownloadStringAsync(new Uri("version.xml", UriKind.Relative), cancellationToken)
                  .ContinueWith(t => new Version((string) XDocument.Parse(t.Result)
                                                                   .Elements("version")
                                                                   .Take(1)
                                                                   .Attributes("current")
                                                                   .FirstOrDefault()), 
                                cancellationToken);
        }

        public virtual Task<VersionManifest> GetManifestAsync(Version version, CancellationToken cancellationToken)
        {
            return DownloadStringAsync(GetVersionUrl(version, "manifest.xml"), cancellationToken)
                  .ContinueWith(t => VersionManifest.LoadVersionData(version, t.Result), cancellationToken);
        }

        public virtual Task<byte[]> DownloadFileAsync(Version version, string fileName, CancellationToken cancellationToken)
        {
            return DownloadBinaryAsync(GetVersionUrl(version, fileName), cancellationToken);
        }

        Uri GetVersionUrl(Version version, string fileName)
        {
            return new Uri(updateServerUrl, version + "/" + fileName);
        }

        Task<string> DownloadStringAsync(Uri url, CancellationToken cancellationToken)
        {
            return DownloadAsync(url,
                                 WebClientTaskifiers.DownloadString,
                                 (wc, ur1) => wc.DownloadStringAsync(ur1),
                                 cancellationToken);
        }

        Task<byte[]> DownloadBinaryAsync(Uri url, CancellationToken cancellationToken)
        {
            return DownloadAsync(url,
                                 WebClientTaskifiers.DownloadData,
                                 (wc, ur1) => wc.DownloadDataAsync(ur1),
                                 cancellationToken);
        }

        Task<T> DownloadAsync<T>(Uri url,
            Func<WebClient, CancellationToken, Task<T>> tasker,
            Action<WebClient, Uri> starter,
            CancellationToken cancellationToken)
        {
            var versionUrl = new Uri(updateServerUrl, url);
            var client = new WebClient();
            var task = tasker(client, cancellationToken);
            starter(client, versionUrl);
            log.Debug("Downloading from URL: {0}", versionUrl);
            return task;
        }

        static class WebClientTaskifiers
        {
            public static readonly Func<WebClient, CancellationToken, Task<string>> DownloadString =
                EapTaskifier<WebClient, DownloadStringCompletedEventArgs, string>(
                    (wc, h) => wc.DownloadStringCompleted += (_, args) => h(args),
                    args => args.Result,
                    wc => wc.CancelAsync);

            public static readonly Func<WebClient, CancellationToken, Task<byte[]>> DownloadData =
                EapTaskifier<WebClient, DownloadDataCompletedEventArgs, byte[]>(
                    (wc, h) => wc.DownloadDataCompleted += (_, args) => h(args),
                    args => args.Result,
                    wc => wc.CancelAsync);

            // Event-based Asynchronous Pattern (EAP) -> Task

            static Func<T, CancellationToken, Task<TResult>> EapTaskifier<T, TArgs, TResult>(
                Action<T, Action<TArgs>> connector,
                Func<TArgs, TResult> selector,
                Func<T, Action> aborter)
                where TArgs : AsyncCompletedEventArgs
            {
                return (client, cancellationToken) =>
                {
                    var tcs = new TaskCompletionSource<TResult>();
                    connector(client, args =>
                    {
                        if (args.Cancelled)
                            tcs.SetCanceled();
                        else if (args.Error != null)
                            tcs.SetException(args.Error);
                        else
                            tcs.SetResult(selector(args));
                    });
                    cancellationToken.Register(aborter(client));
                    return tcs.Task;
                };
            }
        }
    }
}
