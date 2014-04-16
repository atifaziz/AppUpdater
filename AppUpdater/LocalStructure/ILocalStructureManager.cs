﻿using AppUpdater.Manifest;
using System;
using System.Collections.Generic;

namespace AppUpdater.LocalStructure
{
    public interface ILocalStructureManager
    {
        void CreateVersionDir(Version version);
        void DeleteVersionDir(Version version);
        IEnumerable<Version> GetInstalledVersions();
        VersionManifest LoadManifest(Version version);
        Version GetCurrentVersion();
        void SetCurrentVersion(Version version);
        Version GetLastValidVersion();
        void SetLastValidVersion(Version version);
        Version GetExecutingVersion();
        bool HasVersionFolder(Version version);
        void CopyFile(Version originVersion, Version destinationVersion, string filename);
        void SaveFile(Version version, string filename, byte[] data);
        void ApplyDelta(Version originalVersion, Version newVersion, string filename, byte[] deltaData);
        Uri GetUpdateServerUri();
    }
}
