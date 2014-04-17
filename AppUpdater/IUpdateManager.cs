namespace AppUpdater
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IUpdateManager
    {
        void Initialize();
        Task<UpdateInfo> CheckForUpdateAsync(CancellationToken cancellationToken);
        Task DoUpdateAsync(UpdateInfo updateInfo, CancellationToken cancellationToken);
    }
}
