namespace AppUpdater.Logging
{
    public sealed class EmptyLog : ILog
    {
        public static EmptyLog Instance { get; private set; }

        public void Info (string message, params object[] values) {}
        public void Warn (string message, params object[] values) {}
        public void Error(string message, params object[] values) {}
        public void Debug(string message, params object[] values) {}

        static EmptyLog() { Instance = new EmptyLog(); }
    }
}
