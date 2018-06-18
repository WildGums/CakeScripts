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

Task("restore-nuget")
    .IsDependentOn("clean")
    .Does(() =>
{
    RestoreNuGetTask(repositoryFolder);
});

Task("build")
    .IsDependentOn("restore-nuget")
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
