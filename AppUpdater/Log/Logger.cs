namespace AppUpdater.Log
{
    using System;

    public static class Logger
    {
        public static Func<Type, ILog> LoggerProvider { get; set; }

        static Logger()
        {
            LoggerProvider = _ => EmptyLog.Instance;
        }

        public static ILog For(Type type)
        {
            try
            {
                return LoggerProvider(type);
            }
            catch
            {
                return EmptyLog.Instance;
            }
        }

        public static ILog For<T>()
        {
            return For(typeof(T));
        }
    }
}
