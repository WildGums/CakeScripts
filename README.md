# Introduction

## Goal

This project's main goal is to implement automated Cake scripts for the following tasks:

1. Create Cake script to query all repositories which belong to a given GitHub organization
2. Given a list of repositories and their URLs. Clone all the repositories to a local directory
3. Given a folder of the cloned repositories and an optional control file. Create Cake script to clean, pull latest changes, restore and update packages, build, run unit test, commit and push those repositories

## Tips

1. Use VS Code with the Cake and Powershell extensions
2. Before the very first run of any Cake script, initialize.ps1 script must be run once
3. Because of version incompatibility of some used packages with the latest Cake.Core, to prevent error messages you must use --settings_skipverification=true 

## Task #1: Query

### Function

Queries all repositories which belong to a given GitHub origanization

### Source Files

``` Text
/scripts/tools/packages.config  // Standard Cake bootstrap file, common for all scripts
/scripts/initialize.ps1  // Customized WildGums init file to download Cake, common for all scripts
/scripts/query.cake  // Implementation
```

### Command line arguments

```Text
--owner-login=... (defaults to: WildGums), GitHub organization login name
--repositories=...  (defaults to: repositories.csv), output file name
```

### Run samples

``` PowerShell
query.cake --settings_skipverification=true
query.cake --repositories=custom.csv --settings_skipverification=true
```

## Task #2: Clone

### Function

Clones all specified repositories to a local directory

### Source Files

``` Text
/scripts/tools/packages.config  // Standard Cake bootstrap file, common for all scripts
/scripts/initialize.ps1  // Customized WildGums init file to download Cake, common for all scripts
/scripts/clone.cake  // Implementation

```

### Command line arguments

```Text
--work-folder=... (defaults to: C:\Source\) 
--repositories=...  (defaults to: repositories.csv), List of repositories (urls) to clone
```
### Run samples

``` PowerShell
clone.cake --settings_skipverification=true
clone.cake --work-folder=c:/temp --repositories=custom-repositories.csv
```

### Behavior comments

- If the main folder (for example: C:\Source) does not exist the script creates it
- The repository folder names are inferred from the repository url (last part before the .git)
- The repository folders automatically created
- The "develop" branch is cloned. If there is no develop branch, then falls back to master
- If the repository folder already exist, then the clone for that repository is skipped and a yellow warning message will be displayed to the user. This is because the clone operation overwrites the file (no merge) so potentially existing work would be destroyed.

### Implementation details

The implementation uses two Cake build addins:

- #addin nuget:?package=Cake.CsvHelper&version=0.2.3&loaddependencies=true
- #addin nuget:?package=Cake.Git&version=0.16.1&loaddependencies=true

Note the version is pinned, i.e. we are not using the latest versions. The first attempts to use the latest versions resulted in internal incompatibility errors with the latest internal dependencies that can not be pinned. See details below:

### Issues (solved)

**Cake.CsvHelper:** We must use an older version, because the newer Cake.CsvHelper version is incompatible with the referenced CsvHelper .NET package references CsvClassMap which is renamed to ClassMap in newer CsvHelper versions

"error CS0234: The type or namespace name 'CsvClassMap' does not exist in the namespace 'CsvHelper.Configuration'"

(we do not use that class explicitly, Cake.CsvHelper trying to use, but it is not exist in the downloaded CsvHelper .NET dependency)

**Cake.Git:** We must use older version of Cake.Git because of internal incompatibility with LibGit2Sharp  
"error CS0029: Cannot implicitly convert type 'System.Collections.Generic.List<LibGit2Sharp.Tag>' to 'System.Collections.Generic.List<LibGit2Sharp.Tag>"

**Addin referencing older Cake.Core (Cake.CsvHelper)**

Error: The assembly 'Cake.CsvHelper is referencing an older version of Cake.Core (0.23.0). This assembly must reference at least Cake.Core version 0.26.0.
An option is to downgrade Cake to an earlier version. It's not recommended, but we can explicitly opt out of assembly verification by configuring the Skip Verification setting to true (i.e. command line parameter "--settings_skipverification=true" see sample commands

## Task 3: Process

### Function

Given a folder of the cloned repositories and an optional control file. This script cleans, pulls latest changes, restores and updates packages, builds, runs unit test, commits and pushes those repositories

### Source

```Text
/scripts/tools/packages.config  // Standard Cake bootstrap file, common for all scripts
/scripts/initialize.ps1  // Customized WildGums init file to download Cake, common for all scripts
/scripts/common.cake  // Reusable Tasks
/scripts/process.cake // Implementation
```

### Command line arguments

```Text
--target=... (defaults to: do all) Can be: clean, git-pull, restore-nuget, update-nuget, build, run-unit-test, git-commit, git-push
--configuration =... (defaults to: Debug) MSBuild configuration to use  
--work-folder=... (defaults to: C:\Source\) 
--git-username=...
--git-password=...
--control=... (defaults to: no controll file) // List of repositories to process. If there is no control file specified, all repositories will be processed in the work folder

```

### Run samples
```PowerShell
process.cake --git-username=myusername --git-password=mypassword --settings_skipverification=true
process.cake --target=clean 
process.cake --target=git-pull 
process.cake --control=OrcLibraries.csv --git-username=myusername --git-password=mypassword
```
### Control File
The control file is a .csv file with one column: PathFragment. The column header in the very firs line is mandatory.


### Behaviour comments

- For to pull task the repository should not have uncommitted changes, because of the potential branch switch to the develop branch. To prevent loss, the script is checks for uncommitted changes, and in case there are throws exception $"Repository '{repositoryFolder}' has uncommitted changes. Please commit before pulling".
- Object [develop] must be known in the local git config, so the original clone must clone that branch (too). In case of Object [develop] not found the opration falls back to the master branch
- The update task NuGetUpdate() will not work in case there are no existing restored packages, so as a prerequisite dependency a restore task NuGetRestore() also implemented.
- The GitCommit task causes errors and breaks the dependency chain if the there are no committable changes. That’s why it cannot be executed unconditionally, so conditional logic checks for uncommitted changes before and if there are no changes the GitCommit is skipped. The user will be informed with a custom message: "There were no uncommitted changes to commit"

### Implementation details

The implementation uses Cake build addins:

- #addin nuget:?package=Cake.Git&version=0.16.1&loaddependencies=true
- Because in common.cake there are  method aliases referring to CsvHelper #addin nuget:?package=Cake.CsvHelper&version=0.2.3&loaddependencies=true also must be added.

### Issues (solved)

The very same as in Task 2: Clone.

### Issues (partially solved)

Both NuGet tasks (restore and update) works OK with packages.config files. Even the WildGums custom /lib packages folder works OK by manually discovering the nuget.config file and configuring the restore task to use that config file.

This can be demonstrated with the repo: https://github.com/pluraltouch/example.git which is a fork of the Cake Example repo.

#### NuGetRestore

The issues begin with the newer .csproj files with     <PackageReference...> Unfortunately the NuGetRestore task can not handle nuget.config  this case. Commenting out the discovery and usage of nuget.config the task works, but restores the packages to a common location. This not fully conforms with the repo based operations what are we implementing.

Still can be solved with  workaround by hardcoding the package folder as /lib to the NuGetRestoreSettings parameter instead of the discovery of the nuget.config.

#### NuGetUpdate

The NuGetUpdate task seems to be hopeless: Hardcoded looking for the packages what are defined in packages.config, and in the NuGetUpdateSettings parameter there are no traces to configure look anywhere else.

