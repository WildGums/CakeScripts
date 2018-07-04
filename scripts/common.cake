// --------------------------------------------------
// Common reusable methods for repository operations
// --------------------------------------------------

public void Clean(string repositoryFolder)
{
    CleanDirectory(repositoryFolder + "/output");
    CleanDirectories(repositoryFolder +  "/**/bin");
    CleanDirectories(repositoryFolder + "/**/obj");    
    CleanDirectories(repositoryFolder + "/**/packages");    
    CleanDirectories(repositoryFolder + "/lib");        
}

public void GitPullTask(string repositoryFolder, string gitUserName, string gitPassword)
{
    // Note: The repo should not have uncommitted changes for this operation to work:
    // Note: Object [develop] must be known in the local git config, so the original clone must clone that branch (too)
    // It turned out that the following lines will silently overwrite local changes, so checking before:

    if (GitHasUncommitedChanges(repositoryFolder))
    {
        throw new Exception($"Repository '{repositoryFolder}' has uncommitted changes. Please commit before pulling");
    }

    GitCheckout(repositoryFolder, "develop", new FilePath[0]);
    GitPull(repositoryFolder, "cake.merger", "cake.merger@wildgums.com", gitUserName, gitPassword, "origin");
}

public void RestoreNuGetTask(string repositoryFolder)
{
    // Get all solutions (usually the One)
    var solutions = GetFiles(repositoryFolder + "/**/*.sln");

    // Use custom restore settings:
    var restoreSettings = new NuGetRestoreSettings {
        // This is the place to add custom settings:
    };
    
    // Take any special settings in effect * if any *, but not in a hardcoded way (for example the usual WildGums repositoryPath)
    var configFile = GetFiles(repositoryFolder + "/**/nuget.config").FirstOrDefault();
    if (configFile != null)
    {
        //restoreSettings.ConfigFile = configFile;
        restoreSettings.PackagesDirectory = "./lib";
    }

    // Restore the packages
    NuGetRestore(solutions, restoreSettings);
}

public void UpdateNuGetTask(string repositoryFolder)
{
    // Get all solutions (usually the One)
    var solutions = GetFiles(repositoryFolder + "/**/*.sln");

    // Update the packages
    // Use custom settings:
    var updateSettings = new NuGetUpdateSettings {
        // This is the place to add custom settings:
        Prerelease = true // According Catel praxis
    };
    NuGetUpdate(solutions, updateSettings);                
}

public void BuildTask(string repositoryFolder, string configuration)
{
     var solutions = GetFiles(repositoryFolder + "/**/*.sln");
     foreach(var solution in solutions)
     {
        MSBuild(solution, settings => settings.SetConfiguration(configuration));
     }
}

public void RunUnitTestsTask(string outputFolder, string configuration)
{
    var folders = new []{
        outputFolder + configuration + "/*.Tests.dll",
        outputFolder + configuration + "/*.Test.dll",
        // For non WildGums conform projects:        
        "./**/bin/" + configuration + "/*.Tests.dll",
        "./**/bin/" + configuration + "/*.Test.dll"
    };

    foreach(var folder in folders)
    {
    NUnit3(folder, new NUnit3Settings {
        NoResults = true
        });

    }
}

public void GitCommitTask(string repositoryFolder)
{
    GitAddAll(repositoryFolder);

    // If there are no chanches, commit will cause exception, so prevent it:
    if (GitHasUncommitedChanges(repositoryFolder))
    {
        GitCommit(repositoryFolder, "cake.merger", "cake.merger@wildgums.com", "Commit done by an automated Cake script");
    }
    else
    {
        Information("There were no uncommitted changes to commit");
    }
}

public void GitPushTask(string repositoryFolder, string gitUserName, string gitPassword)
{
    GitPush(repositoryFolder, gitUserName, gitPassword);
}
