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

#addin nuget:?package=Cake.Http
#addin nuget:?package=Cake.Json
#addin nuget:?package=Newtonsoft.Json&version=9.0.1

 
//-------------------------------------------------------------------------------------
// ARGUMENTS
//-------------------------------------------------------------------------------------
var target = Argument("target", "default");
var owner = Argument("owner-login", "WildGums"); // Currently implemented for organistations
var workFolder = Argument("work-folder", "C:/Source/"); 
var repositoryCsvFileName = Argument("repository-csv", "repositories.csv");

public string GetRepositoryFolder(string cloneUrl)
{
    var split = cloneUrl.Replace(".git", "").Split('/', '\\');
    return split.LastOrDefault()?? "created-"+ DateTime.Now.ToString("{yyyy-MM-dd-HH-mm-ss-fff}");
}

class RepositoryInfo {
    public string CloneUrl {get; set; }
    public string GitUrl {get; set; }
    public override string ToString()
    {
        return $"{nameof(CloneUrl)}: {CloneUrl}, {nameof(GitUrl)}: {GitUrl}";
    }

}

//--------------------------------------------------------------------
// TASKS
//--------------------------------------------------------------------

Task("Default")
    .Does(() =>
{
    owner = owner.Trim();
    Information($"Querying GitHub repositories for owner organistaion '{owner}'");


    // Url creation is currently implemented only for organistations:
    string json = HttpGet($"https://api.github.com/orgs/{owner}/repos?per_page=200 ");

    // There is no support (alias) for JArray in Cake.Json, so using NewtonSoft original class name: JArray.Parse
    var resultJArray = JArray.Parse(json);

    var repositoryInfos = new List<RepositoryInfo>();
    foreach(var item in resultJArray)
    {
        repositoryInfos.Add(new RepositoryInfo 
        {
            CloneUrl = (string) item["clone_url"],
            GitUrl = (string) item["git_url"]
        });
    }
    WriteCsv<RepositoryInfo>(repositoryCsvFileName, repositoryInfos, new CsvHelperSettings { HasHeaderRecord = true });
    Information($"{repositoryInfos.Count} repositories successfully queried and written to CSV file '{repositoryCsvFileName}'");
});

//--------------------------------------------------------------------
// EXECUTION
//--------------------------------------------------------------------

RunTarget(target);