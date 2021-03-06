﻿namespace AppUpdater
{
    using System;

    public class UpdateInfo
    {
        public Version Version { get; private set; }
        public bool HasUpdate { get; private set; }

        public UpdateInfo(bool hasUpdate, Version version)
        {
            HasUpdate = hasUpdate;
            Version = version;
        }
    }
}
