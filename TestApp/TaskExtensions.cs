namespace TestApp
{
    using System;
    using System.Threading.Tasks;
    using System.Windows.Forms;

    static class TaskExtensions
    {
        public static Task ContinueEventHandlerWith<T>(this Task<T> task, IWin32Window owner, Action<T> action)
        {
            if (task == null) throw new ArgumentNullException("task");
            if (action == null) throw new ArgumentNullException("action");

            return task.ContinueWith(t =>
            {
                try
                {
                    action(task.Result);
                }
                catch (Exception e)
                {
                    if (new ThreadExceptionDialog(e).ShowDialog(owner) == DialogResult.Abort)
                        Application.ExitThread();
                }

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public static Task ContinueEventHandlerWith(this Task task, IWin32Window owner, Action action)
        {
            if (task == null) throw new ArgumentNullException("task");
            if (action == null) throw new ArgumentNullException("action");

            return task.ContinueWith(t =>
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    if (new ThreadExceptionDialog(e).ShowDialog(owner) == DialogResult.Abort)
                        Application.ExitThread();
                }

            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}