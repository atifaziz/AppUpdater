namespace AppUpdater.Server
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
    using Log;
    using Manifest;

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
            return DownloadStringAsync("version.xml", cancellationToken)
                  .ContinueWith(t =>
                  {
                      var doc = XDocument.Parse(t.Result);
                      return new Version((string) doc.Elements("config").Single().Element("version"));

                  }, cancellationToken);
        }

        public virtual Task<VersionManifest> GetManifestAsync(Version version, CancellationToken cancellationToken)
        {
            return DownloadStringAsync(GetVersionFilename(version, "manifest.xml"), cancellationToken)
                  .ContinueWith(t => VersionManifest.LoadVersionData(version, t.Result), cancellationToken);
        }

        public virtual Task<byte[]> DownloadFileAsync(Version version, string filename, CancellationToken cancellationToken)
        {
            return DownloadBinaryAsync(GetVersionFilename(version, filename), cancellationToken);
        }

        string GetVersionFilename(Version version, string filename)
        {
            return new Uri(updateServerUrl, /*TODO*/ Path.Combine(version.ToString(), filename)).ToString();
        }

        Task<string> DownloadStringAsync(string filename, CancellationToken cancellationToken)
        {
            return DownloadAsync(filename,
                                 WebClientTaskifiers.DownloadString,
                                 (wc, url) => wc.DownloadStringAsync(url),
                                 cancellationToken);
        }

        Task<byte[]> DownloadBinaryAsync(string filename, CancellationToken cancellationToken)
        {
            return DownloadAsync(filename,
                                 WebClientTaskifiers.DownloadData,
                                 (wc, url) => wc.DownloadDataAsync(url),
                                 cancellationToken);
        }

        Task<T> DownloadAsync<T>(string filename,
            Func<WebClient, CancellationToken, Task<T>> tasker,
            Action<WebClient, Uri> starter,
            CancellationToken cancellationToken)
        {
            var versionUrl = new Uri(updateServerUrl, filename);
            var client = new WebClient();
            var task = tasker(client, cancellationToken);
            starter(client, versionUrl);
            log.Debug("Downloading from url: {0}", versionUrl);
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
