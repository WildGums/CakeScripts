// --------------------------------------------------
// Common reusable methods for repository operations
// --------------------------------------------------

public void CleanTask(string repositoryFolder)
{
    CleanDirectory($"{repositoryFolder}/output");
    CleanDirectories($"{repositoryFolder}/**/bin");
    CleanDirectories($"{repositoryFolder}/**/obj");    
    CleanDirectories($"{repositoryFolder}/**/packages");    
    // CleanDirectories($"{repositoryFolder}/lib");        
}

public void GitPullTask(string repositoryFolder, string gitUserName, string gitPassword)
{
    // #1: The repo should not have uncommitted changes for this operation to work:
    // #2: Object [develop] must be known in the local git config, so the original clone must clone that branch (too)
    // In case [develop] object not found give a shot for [master]
    // CheckOut will silently overwrite local changes, so checking before:

    if (GitHasUncommitedChanges(repositoryFolder))
    {
        throw new Exception($"Repository '{repositoryFolder}' has uncommitted changes. Please commit before pulling");
    }
    try
    {
        GitCheckout(repositoryFolder, "develop", new FilePath[0]);        
    }
    catch(LibGit2Sharp.NotFoundException e)            
    {
        if (e.Message.Contains("develop"))
        {
            Warning($"Branch 'develop' not found in '{repositoryFolder}'. Trying to pull 'master'");    
            GitCheckout(repositoryFolder, "master", new FilePath[0]);        
            Information("Successfully pulled 'master'");
        }
        else{
            throw;
        }
    }    

    GitPull(repositoryFolder, "cake.merger", "cake.merger@wildgums.com", gitUserName, gitPassword, "origin");
}

public void RestoreNuGetTask(string repositoryFolder)
{
    // Get all solutions (usually the One)
    var solutions = GetSolutionFiles(repositoryFolder);

    // Use custom restore settings:
    var restoreSettings = new NuGetRestoreSettings {
        // This is the place to add custom settings:
    };
    
    // Take any special settings in effect * if any *, but not in a hardcoded way (for example the usual WildGums repositoryPath)
    var configFile = GetFiles(repositoryFolder + "/**/nuget.config").FirstOrDefault();
    if (configFile != null)
    {
        //restoreSettings.ConfigFile = configFile;
        restoreSettings.PackagesDirectory = $"{repositoryFolder}/lib";
    }

    // Restore the packages
    NuGetRestore(solutions, restoreSettings);
}

public void UpdateNuGetTask(string repositoryFolder)
{
    // Get all solutions (usually the One)
    var solutions = GetSolutionFiles(repositoryFolder);

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
    var solutions = GetSolutionFiles(repositoryFolder);
     foreach(var solution in solutions)
     {
        MSBuild(solution, settings => settings.SetConfiguration(configuration));
     }
}

public void RunUnitTestsTask(string repositoryFolder, string configuration)
{
    var folders = new []{
        $"{repositoryFolder}/output/{configuration}/**/*.Tests.dll",
        $"{repositoryFolder}/output/{configuration}/**/*.Test.dll" ,      
        // For non WildGums conform projects:        
        $"{repositoryFolder}/**/bin/{configuration}/*.Tests.dll",
        $"{repositoryFolder}/**/bin/{configuration}/*.Test.dll",
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

private IEnumerable<FilePath> GetSolutionFiles(string repositoryFolder)
{
    return GetFiles(repositoryFolder + "/**/*.sln").Where(s => !s.FullPath.Contains("[")); // special templates
}
