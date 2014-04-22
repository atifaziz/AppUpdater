namespace AppUpdater
{
    #region Imports

    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;

    #endregion

    public class UpdaterChef : IUpdaterChef
    {
        private readonly ILog log = Logger.For<UpdaterChef>();
        private readonly ILocalStructureManager localStructureManager;
        private readonly IUpdateServer updateServer;

        public UpdaterChef(ILocalStructureManager localStructureManager, IUpdateServer updateServer)
        {
            this.localStructureManager = localStructureManager;
            this.updateServer = updateServer;
        }

        public virtual Task CookAsync(UpdateRecipe recipe, CancellationToken cancellationToken)
        {
            var output = new TaskCompletionSource<object>();

            Task.Factory.StartNew(() =>
                {
                    try
                    {
                        Cook(recipe, cancellationToken, output);
                    }
                    catch (Exception e)
                    {
                        output.TrySetException(e);
                    }
                }, 
                cancellationToken, 
                TaskCreationOptions.None, 
                TaskScheduler.Default);     // Run on thread pool

            return output.Task;
        }

        void Cook(UpdateRecipe recipe, CancellationToken cancellationToken, TaskCompletionSource<object> output)
        {
            if (localStructureManager.HasVersionFolder(recipe.NewVersion))
                localStructureManager.DeleteVersionDir(recipe.NewVersion);

            localStructureManager.CreateVersionDir(recipe.NewVersion);

            var copies = 
                from file in recipe.Files
                where file.Action == FileUpdateAction.Copy
                select file;
            
            foreach (var file in copies)
                OnCopy(recipe, file);

            var downloadz =
                from file in recipe.Files
                where file.Action == FileUpdateAction.DownloadDelta 
                   || file.Action == FileUpdateAction.Download
                select new
                {
                    IsDelta = file.Action == FileUpdateAction.DownloadDelta,
                    File    = file,
                };

            var downloads = downloadz.ToArray();

            if (downloads.Length == 0)
            {
                output.SetResult(null);
            }
            else
            {
                var outstandingCount = new[] { downloads.Length };

                foreach (var download in downloads)
                {
                    OnDownloadAsync(recipe, download.File, download.IsDelta, cancellationToken)
                        // ReSharper disable once MethodSupportsCancellation
                        .ContinueWith(t =>
                        {
                            if (t.IsCanceled || t.IsFaulted)
                                output.TrySetFromTask(t);
                            else if (0 == Interlocked.Decrement(ref outstandingCount[0]))
                                output.SetResult(null);
                        });
                }
            }
        }

        void OnCopy(UpdateRecipe recipe, UpdateRecipeFile file)
        {
            log.Debug("Copying file \"{0}\" from version \"{1}\".", file.Name, recipe.CurrentVersion);
            localStructureManager.CopyFile(recipe.CurrentVersion, recipe.NewVersion, file.Name);
        }

        Task OnDownloadAsync(UpdateRecipe recipe, UpdateRecipeFile file, bool delta, CancellationToken cancellationToken)
        {
            log.Debug("Downloading {0}file \"{1}\".",
                      delta ? "patch" : null,
                      file.FileToDownload);

            return updateServer.DownloadFileAsync(recipe.NewVersion, file.FileToDownload, cancellationToken).ContinueWith(t =>
            {
                var data = t.Result;
                if (delta)
                {
                    log.Debug("Applying patch file.");
                    localStructureManager.ApplyDelta(recipe.CurrentVersion, recipe.NewVersion, file.Name, data);
                }
                else
                {
                    log.Debug("Decompressing the file.");
                    data = DataCompressor.Decompress(data);
                    log.Debug("Saving the file \"{0}\".", file.Name);
                    localStructureManager.SaveFile(recipe.NewVersion, file.Name, data);
                }
            }, cancellationToken);
        }
    }
}
