using System;
using System.Collections.Generic;

namespace AppUpdater.Recipe
{
    public class UpdateRecipe
    {
        public Version NewVersion { get; private set; }
        public Version CurrentVersion { get; private set; }
        public IEnumerable<UpdateRecipeFile> Files { get; private set; }

        public UpdateRecipe(Version newVersion, Version currentVersion, IEnumerable<UpdateRecipeFile> files)
        {
            this.NewVersion = newVersion;
            this.CurrentVersion = currentVersion;
            this.Files = files;
        }
    }
}
