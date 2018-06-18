// Note #1: Cake.Git: The loaddependencies=true will take effect 
// in case using Cake built in NuGet support and not the external nuget.exe

// Note #2: Cake.Git: We must use older version of Cake.Git because of internal incompatibility with LibGit2Sharp  
// error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<LibGit2Sharp.Tag>' to 'System.Collections.Generic.List<LibGit2Sharp.Tag>'
#addin nuget:?package=Cake.Git&version=0.16.1&loaddependencies=true
#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0

// Common definitions
#load common.cake

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "default");
var configuration = Argument("configuration", "Debug");
var gitUserName = Argument("git-username", "<username>");
var gitPassword = Argument("git-password", "******");
var doPull = Argument("do-pull", false);

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define directories.
var repositoryFolder = ".";
    
// TODO: Do not hardcode outputfolder(s), instead infer from the projects (*.csproj files) This currently in category overkill.
var outputFolder = repositoryFolder + "/output";

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("clean")
    .Does(() =>
{
    CleanTask(outputFolder, repositoryFolder, configuration);
});

Task("git-pull")
    .Does(() =>
{
    if (!doPull)
    {
        Information("Skipping pull because --do-pull option was not presented.");
        return;
    }
    // Note: The repo should not have uncommitted changes for this operation to work:
    // Note: Object [develop] must be known in the local git config, so the original clone must clone that branch (too)
    // It turned out that the following lines will silently overwrite local changes, so checnking before:

    GitPullTask(repositoryFolder, gitUserName, gitPassword);
});

Task("clean-all")
    .IsDependentOn("git-pull")
    .Does(() =>
{
    CleanAllTask(outputFolder, repositoryFolder);
});

Task("restore-nuget")
    .IsDependentOn("clean-all")
    .Does(() =>
{
    RestoreNuGetTask(repositoryFolder);
});

Task("update-nuget")
    .IsDependentOn("restore-nuget")
    .Does(() =>
{
    UpdateNuGetTask(repositoryFolder);
});

Task("build")
    .IsDependentOn("update-nuget")
    .Does(() =>
{
    BuildTask(repositoryFolder, configuration);

});

Task("run-unit-tests")
    .IsDependentOn("build")
    .Does(() =>
{
    RunUnitTestsTask(outputFolder, configuration);

});


Task("git-commit")
    .IsDependentOn("run-unit-tests")
    .Does(() =>
{
    GitCommitTask(repositoryFolder) ;
});

Task("git-push")
    .IsDependentOn("git-commit")
    .Does(() =>
{
    GitPushTask(repositoryFolder, gitUserName, gitPassword);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("default")
    .IsDependentOn("git-push");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
