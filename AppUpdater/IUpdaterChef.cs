namespace AppUpdater
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IUpdaterChef
    {
        Task CookAsync(UpdateRecipe recipe, CancellationToken cancellationToken);
    }
}
