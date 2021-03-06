﻿namespace AppUpdater.Logging
{
    public interface ILog
    {
        void Info(string message, params object[] values);
        void Warn(string message, params object[] values);
        void Error(string message, params object[] values);
        void Debug(string message, params object[] values);
    }
}
