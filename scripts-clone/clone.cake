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

//-------------------------------------------------------------------------------------
// ARGUMENTS
//-------------------------------------------------------------------------------------
var target = Argument("target", "default");
var workFolder = Argument("work-folder", "C:/TempRepos/"); 
var gitUserName = Argument("git-username", "<username>");
var gitPassword = Argument("git-password", "******");
var repositoryCsvFileName = Argument("repository-csv", "repositories.csv");
var version = "1.0.0";

public string GetRepositoryFolder(string cloneUrl)
{
    var split = cloneUrl.Replace(".git", "").Split('/', '\\');
    return split.LastOrDefault()?? "created-"+ DateTime.Now.ToString("{yyyy-MM-dd-HH-mm-ss-fff}");
}

class RepositoryInfo {
    public string CloneUrl {get; set; }
    public string GitUrl {get; set; }
}

//-------------------------------------------------------------------------------------
// TASKS
//-------------------------------------------------------------------------------------

Task("Default")
    .Does(() =>
{
    Information($"clone.cake v{version}");
    var repositoryInfos = ReadCsv<RepositoryInfo>(repositoryCsvFileName, new CsvHelperSettings { HasHeaderRecord = true });
    
    Information($"Cloning {repositoryInfos.Count()} repositories to '{workFolder}'. Urls read from file '{repositoryCsvFileName}'");
    foreach(var info in repositoryInfos)
    {
        var repositoryFolder = workFolder + GetRepositoryFolder(info.CloneUrl);
        if (!DirectoryExists(workFolder))
        {
            CreateDirectory(workFolder);
        }
        if (DirectoryExists(repositoryFolder))
        {
            Warning($"Folder '{repositoryFolder}' already exists. Skipping repository '{info.CloneUrl}'");
            continue;
        }
        var branchNames = new [] {"develop", "master"};
        foreach(var branchName in branchNames)
        {
            try
            {
                var settings = new GitCloneSettings {
                    BranchName = branchName, 
                    Checkout = true,
                    IsBare = false,
                    RecurseSubmodules = true
                };

                // Unfortunately this overload does not work, giving an unrelated error message:
                // Error while cloning 'https://github.com/pluraltouch/example.git': Failed to find workDirectoryPath: <correct path>
                // GitClone(info.CloneUrl, repositoryFolder, gitUserName, gitPassword, settings);
                GitClone(info.CloneUrl, repositoryFolder, settings);
                Information($"Repository '{info.CloneUrl}' successfully cloned to '{repositoryFolder}'.");
                break;
            }
            catch(LibGit2Sharp.NotFoundException e)            
            {
                if (e.Message.Contains("develop"))
                {
                    Warning($"Branch 'develop' not found in '{info.CloneUrl}'. Trying to clone 'master'");    
                    // This will not destroy _any_ existing source, only the freshly created (empty) folder by this run session
                    // See the  if (DirectoryExists(repositoryFolder)) statement
                    DeleteDirectory(repositoryFolder, new DeleteDirectorySettings {
                        Recursive = true,
                        Force = true
                    });
                }
                else{
                    Error($"Error while cloning '{info.CloneUrl}': {e.Message}");
                }
            }
            catch(Exception e)
            {
                Error($"Error while cloning '{info.CloneUrl}': {e.Message}");
            }
        }
    }
});

//-------------------------------------------------------------------------------------
// EXECUTION
//-------------------------------------------------------------------------------------

RunTarget(target);