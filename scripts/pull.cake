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

Task("git-pull")
    .Does(() =>
{
    // Note: The repo should not have uncommitted changes for this operation to work:
    // Note: Object [develop] must be known in the local git config, so the original clone must clone that branch (too)
    // It turned out that the following lines will silently overwrite local changes, so checnking before:

    GitPullTask(repositoryFolder, gitUserName, gitPassword);
});

Task("default")
    .IsDependentOn("git-pull");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
