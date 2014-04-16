using System;
using System.IO;
using System.Net;
using System.Xml;
using AppUpdater.Manifest;
using AppUpdater.Log;

namespace AppUpdater.Server
{
    using System.Linq;
    using System.Xml.Linq;

    public class DefaultUpdateServer : IUpdateServer
    {
        private readonly Uri updateServerUrl;
        private readonly ILog log = Logger.For<DefaultUpdateServer>();

        public DefaultUpdateServer(Uri updateServerUrl)
        {
            this.updateServerUrl = updateServerUrl;
        }

        public Version GetCurrentVersion()
        {
            var xmlData = DownloadString("version.xml");
            var doc = XDocument.Parse(xmlData);
            return new Version((string) doc.Elements("config").Single().Element("version"));
        }

        public VersionManifest GetManifest(Version version)
        {
            var xmlData = DownloadString(GetVersionFilename(version, "manifest.xml"));
            return VersionManifest.LoadVersionData(version, xmlData);
        }

        public byte[] DownloadFile(Version version, string filename)
        {
            return DownloadBinary(GetVersionFilename(version, filename));
        }

        private string DownloadString(string filename)
        {
            var versionUrl = new Uri(updateServerUrl, filename);
            var client = new WebClient();
            log.Debug("Downloading from url: {0}", versionUrl);
            return client.DownloadString(versionUrl);
        }

        private byte[] DownloadBinary(string filename)
        {
            var versionUrl = new Uri(updateServerUrl, filename);
            var client = new WebClient();
            log.Debug("Downloading from url: {0}", versionUrl);
            return client.DownloadData(versionUrl);
        }

        private string GetVersionFilename(Version version, string filename)
        {
            return new Uri(updateServerUrl, /*TODO*/ Path.Combine(version.ToString(), filename)).ToString();
        }
    }
}
