// Note #1: Cake.CsvHelper: The loaddependencies=true will only take effect 
// in case using Cake built in NuGet support and not the external nuget.exe
//
// Note #2: Cake.CsvHelper: We must use an older version because the newer 
// version is incompatible with the referenced C# CsvHelper .NET package
// (it references to class CsvClassMap which is renamed to ClassMap)
#addin nuget:?package=Cake.CsvHelper&version=0.2.3&loaddependencies=true

// Note #1: Cake.Git: The loaddependencies=true will only take effect 
// in case using Cake built in NuGet support and not the external nuget.exe

// Note #2: Cake.Git: We must use older version of Cake.Git because of internal incompatibility with LibGit2Sharp  
// error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<LibGit2Sharp.Tag>' to 'System.Collections.Generic.List<LibGit2Sharp.Tag>'
#addin nuget:?package=Cake.Git&version=0.16.1&loaddependencies=true
#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0

// Common definitions
#load common.cake


//-------------------------------------------------------------------------------------
// ARGUMENTS
//-------------------------------------------------------------------------------------
var target = Argument("target", "default");
var configuration = Argument("configuration", "Debug");
var workFolder = Argument("work-folder", "C:/TempRepos/"); 
var gitUserName = Argument("git-username", "<username>");
var gitPassword = Argument("git-password", "******");
var controllFileName = Argument("controll", "");
var version = "1.0.0";
Information($"build.cake v{version}");

public string GetRepositoryFolder(string cloneUrl)
{
    var split = cloneUrl.Replace(".git", "").Split('/', '\\');
    return split.LastOrDefault()?? "created-"+ DateTime.Now.ToString("{yyyy-MM-dd-HH-mm-ss-fff}");
}

class ControllInfo {
    public string PathFragment {get; set; }
}

//-------------------------------------------------------------------------------------
// Initialize 
//-------------------------------------------------------------------------------------

workFolder = $"{workFolder.Replace("/", "\\").Trim('\\')}\\";
Func<DirectoryPath, bool> predicate = dummy => true;

if (string.IsNullOrEmpty(controllFileName))
{
    Information($"Processing all folders in '{workFolder}'. No controll file was specified.");
}
else
{
    Information($"Processing folders in '{workFolder}' using controll file '{controllFileName}'.");
    var controllInfos = ReadCsv<ControllInfo>(controllFileName, new CsvHelperSettings { HasHeaderRecord = true });
    predicate = path => controllInfos.Any(ci => path.FullPath.ToLower().Contains(ci.PathFragment.ToLower()));
}

// GetDirectories filterable overload does not work as expected, so uing LINQ instead:
var directories = GetDirectories($"{workFolder}*").Where(predicate).Select(directoryPath => directoryPath.FullPath).ToList();
Information($"{directories.Count()} repository folder(s) will be processed.");

//-------------------------------------------------------------------------------------
// TASKS
//-------------------------------------------------------------------------------------
Task("Initialize")
.Does(() =>
{
    workFolder = $"{workFolder.Replace("/", "\\").Trim('\\')}\\";
    Func<DirectoryPath, bool> predicate = dummy => true;
    
    if (string.IsNullOrEmpty(controllFileName))
    {
        Information($"Processing all folders in '{workFolder}'. No controll file was specified.");
    }
    else
    {
        Information($"Processing folders in '{workFolder}' using controll file '{controllFileName}'.");
        var controllInfos = ReadCsv<ControllInfo>(controllFileName, new CsvHelperSettings { HasHeaderRecord = true });
        predicate = path => controllInfos.Any(ci => path.FullPath.ToLower().Contains(ci.PathFragment.ToLower()));
    }

    // GetDirectories filterable overload does not work as expected, so uing LINQ instead:
    directories = GetDirectories($"{workFolder}*").Where(predicate).Select(directoryPath => directoryPath.FullPath).ToList();
    Information($"{directories.Count()} repository folder(s) will be processed.");
});

foreach(var directory in directories)
{
    // Individual task, can run with --target=clean
    // Will be executed for all specified folders
    Task($"clean {directory}")
    .Does(() =>
    {
        CleanTask(directory);
    });

    // Individual task, can run with --target=git-pull
    // Will be executed for all specified folders
    Task($"git-pull {directory}")
    .Does(() =>
    {
        GitPullTask(directory, gitUserName, gitPassword);
    });

    // Individual task, can run with --target=restore-nuget
    // Will be executed for all specified folders
    Task($"restore-nuget {directory}")
    .Does(() =>
    {
        RestoreNuGetTask(directory);
    });

    // Individual task, can run with --target=update-nuget
    // Will be executed for all specified folders
    Task($"update-nuget {directory}")
    .IsDependentOn($"restore-nuget {directory}")
    .Does(() =>
    {
        UpdateNuGetTask(directory);
    });

    // Individual task, can run with --target=build
    // Will be executed for all specified folders
    Task($"build {directory}")
    .Does(() =>
    {
        BuildTask(directory, configuration);
    });

    // Individual task, can run with --target=run-unit-tests
    // Will be executed for all specified folders
    Task($"run-unit-tests {directory}")
    .Does(() =>
    {
        RunUnitTestsTask(directory, configuration);
    });

    // Individual task, can run with --target=git-commit
    // Will be executed for all specified folders
    Task($"git-commit {directory}")
    .Does(() =>
    {
        GitCommitTask(directory) ;
    });

    // Individual task, can run with --target=git-push
    // Will be executed for all specified folders
    Task($"git-push {directory}")
    .Does(() =>
    {
        GitPushTask(directory, gitUserName, gitPassword);
    });

    // ------------------------------
    // Configure default dependency chain:
    // ------------------------------

    // Part of complex task 'default'. Do not run as target
    // To do a full build use the default target, that will execute all dependency steps for all folders
    Task($"clean-before-pull {directory}")
    .Does(() =>
    {
        CleanTask(directory);
    });

    // Part of complex task 'default'. Do not run as target
    // To do a full build use the default target, that will execute all dependency steps for all folders
    Task($"git-pull-internal {directory}")
    .IsDependentOn($"clean-before-pull {directory}")
    .Does(() =>
    {
        GitPullTask(directory, gitUserName, gitPassword);
    });    

    // Part of complex task 'default'. Do not run as target
    // To do a full build use the default target, that will execute all dependency steps for all folders
    Task($"clean-after-pull {directory}")
    .IsDependentOn($"git-pull-internal {directory}")    
    .Does(() =>
    {
        CleanTask(directory);
    });

    // Part of complex task 'default'. Do not run as target
    // To do a full build use the default target, that will execute all dependency steps for all folders
    Task($"restore-nuget-internal {directory}")
    .IsDependentOn($"clean-after-pull {directory}")    
    .Does(() =>
    {
        RestoreNuGetTask(directory);
    });

    // Part of complex task 'default'. Do not run as target
    // To do a full build use the default target, that will execute all dependency steps for all folders
    Task($"update-nuget-internal {directory}")
    .IsDependentOn($"restore-nuget-internal {directory}")
    .Does(() =>
    {
        UpdateNuGetTask(directory);
    });

    // Part of complex task 'default'. Do not run as target
    // To do a full build use the default target, that will execute all dependency steps for all folders
    Task($"build-internal {directory}")
    .IsDependentOn($"update-nuget-internal {directory}")    
    .Does(() =>
    {
        BuildTask(directory, configuration);
    });

    // Part of complex task 'default'. Do not run as target
    // To do a full build use the default target, that will execute all dependency steps for all folders
    Task($"run-unit-tests-internal {directory}")
    .IsDependentOn($"build-internal {directory}")    
    .Does(() =>
    {
        RunUnitTestsTask(directory, configuration);
    });

    // Part of complex task 'default'. Do not run as target
    // To do a full build use the default target, that will execute all dependency steps for all folders
    Task($"git-commit-internal {directory}")
    .IsDependentOn($"run-unit-tests-internal {directory}")        
    .Does(() =>
    {
        GitCommitTask(directory) ;
    });

    // Part of complex task 'default'. Do not run as target
    // To do a full build use the default target, that will execute all dependency steps for all folders
    Task($"git-push-internal {directory}")
    .IsDependentOn($"git-commit-internal {directory}")        
    .Does(() =>
    {
        GitPushTask(directory, gitUserName, gitPassword);
    });

    // Complex 'default' task. Does all dependency steps for all folders
    Task($"default {directory}")
    .IsDependentOn($"git-push-internal {directory}")
    .Does(() =>
    {
    });        



    //-------------------------------------------------------------------------------------
    // EXECUTION
    //-------------------------------------------------------------------------------------

    // Catch and display Exception for a particular repository, 
    // then continue, allowing other repositories to be processed
    try
    {
        RunTarget($"{target} {directory}");     
    }
    catch (Exception e)
    {
        Error($"Error while processing repository folder '{directory}'. {e.Message}");
    }
}