namespace AppUpdater
{
    using System;

    static class Disposable
    {
        public static TResult Using<T, TResult>(this T resource, Func<T, TResult> selector)
            where T : IDisposable
        {
            if (selector == null) throw new ArgumentNullException("selector");
            using (resource)
                return selector(resource);
        }
    }
}