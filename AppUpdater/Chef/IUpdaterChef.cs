namespace AppUpdater.Chef
{
    using System.Threading;
    using System.Threading.Tasks;
    using Recipe;

    public interface IUpdaterChef
    {
        Task CookAsync(UpdateRecipe recipe, CancellationToken cancellationToken);
    }
}
