﻿namespace AppUpdater
{
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Collections.Generic;

    public class VersionManifestFile
    {
        static readonly ICollection<VersionManifestDeltaFile> ZeroDeltas = new VersionManifestDeltaFile[0];

        public string Name { get; private set; }
        public string Checksum { get; private set; }
        public long Size { get; private set; }
        public ICollection<VersionManifestDeltaFile> Deltas { get; private set; }

        public string DeployedName
        {
            get
            {
                return Name == null ? null : Name + ".deploy";
            }
        }

        public VersionManifestFile(string name, string checksum, long size) : 
            this(name, checksum, size, null) {}

        public VersionManifestFile(string name, string checksum, long size, IEnumerable<VersionManifestDeltaFile> deltas)
        {
            Name = name;
            Checksum = checksum;
            Size = size;
            Deltas = deltas != null 
                   ? new ReadOnlyCollection<VersionManifestDeltaFile>(deltas.ToArray())
                   : ZeroDeltas;
        }

        public VersionManifestDeltaFile GetDeltaFrom(string checksum)
        {
            return Deltas.FirstOrDefault(x => x.Checksum == checksum);
        }
    }
}
