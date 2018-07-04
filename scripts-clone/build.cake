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
        foreach(var ci in controllInfos)
        {
            Information(ci.PathFragment);
        }
        predicate = path => controllInfos.Any(ci => path.FullPath.ToLower().Contains(ci.PathFragment.ToLower()));
    }

    // GetDirectories build in filter overload does not work as expected, so uing LINQ instead:
    var directories = GetDirectories($"{workFolder}*").Where(predicate);
    Information($"{directories.Count()} repository folder(s) will be processed.");
});

Task("Clean")
    .IsDependentOn("Initialize")
    .Does(() =>
{
});

Task("Default")
    .IsDependentOn("Clean")
    .Does(() =>
{
});

//-------------------------------------------------------------------------------------
// EXECUTION
//-------------------------------------------------------------------------------------

RunTarget(target);