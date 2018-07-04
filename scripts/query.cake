// Note #1: Cake.CsvHelper: The loaddependencies=true will only take effect 
// in case using Cake built in NuGet support and not the external nuget.exe
//
// Note #2: Cake.CsvHelper: We must use an older version because the newer 
// version is incompatible with the referenced C# CsvHelper .NET package
// (it references to class CsvClassMap which is renamed to ClassMap)
#addin nuget:?package=Cake.CsvHelper&version=0.2.3&loaddependencies=true

#addin nuget:?package=Cake.Http
#addin nuget:?package=Cake.Json
#addin nuget:?package=Newtonsoft.Json&version=9.0.1

 
//-------------------------------------------------------------------------------------
// ARGUMENTS
//-------------------------------------------------------------------------------------
var target = Argument("target", "default");
var owner = Argument("owner-login", "WildGums"); 
var repositoryCsvFileName = Argument("repositories", "repositories.csv");
var version = "1.0.0";

class RepositoryInfo {
    public string CloneUrl {get; set; }
    public string GitUrl {get; set; }
    public override string ToString()
    {
        return $"{nameof(CloneUrl)}: {CloneUrl}, {nameof(GitUrl)}: {GitUrl}";
    }
}

Information($"query.cake v{version}");

//--------------------------------------------------------------------
// TASKS
//--------------------------------------------------------------------

Task("Default")
    .Does(() =>
{
    Information($"Querying GitHub repositories for owner organization '{owner}'");

    // Url creation is currently implemented only for organizations:
    string json = HttpGet($"https://api.github.com/orgs/{owner}/repos?per_page=200 ");

    // There is no support (alias) for JArray in Cake.Json, using NewtonSoft original class name: JArray.Parse
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