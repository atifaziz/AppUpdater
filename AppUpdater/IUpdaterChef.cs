namespace AppUpdater
{
    using System.Threading;
    using System.Threading.Tasks;
    using AppUpdater;

    public interface IUpdaterChef
    {
        Task CookAsync(UpdateRecipe recipe, CancellationToken cancellationToken);
    }
}
