namespace AppUpdater
{
    using System;
    using System.Collections.Generic;

    public class UpdateRecipe
    {
        public Version NewVersion { get; private set; }
        public Version CurrentVersion { get; private set; }
        public IEnumerable<UpdateRecipeFile> Files { get; private set; }

        public UpdateRecipe(Version newVersion, Version currentVersion, IEnumerable<UpdateRecipeFile> files)
        {
            NewVersion = newVersion;
            CurrentVersion = currentVersion;
            Files = files;
        }
    }
}
