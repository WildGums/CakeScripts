# Introduction

## Goal

The project's main goal is to implement automated Cake scripts for the following tasks:

1. Given a list of repositories and their URLs, clone all the repositories to a local directory. (i.e. clone all repos into C:\Source\ by default)
2. Build a script to pull the latest changes from a repository and checkout the develop branch.
3. Build a script to update packages to the latest versions given a control csv file.
4. Build a script to build, run unit tests and if successful commit and push the changes back to the cloud repository
5. Build a script to delete all bin, obj, and package folders (i.e. all artifacts.)
6. A bonus script also implemented what is using the make dependency logic and chains the pull->clean->restore-nuget->update-nuget->build->run-unit-test->commit->push logic in one dependency chain.

## Tips

Use VS Code with the Cake and Powershell extensions

## Task 1: Clone

### Function

Given a list of repositories and their URLs, clone all the repositories to a local directory. (i.e. clone all repos into C:\Source\)

### Source Files

``` Text
/tools/packages.config  // Standard Cake bootstrap file
/initialize.ps1  // Customized WildGums init file to download Cake
/clone.cake  // Implementation
/repositories.csv // Identical with the originally specified WildGumsRepositories.csv
```

### Command line arguments

```Text
--work-folder=... (defaults to: C:\Source\)
--git-username=...
--git-password=...
--repository-csv=...  (defaults to: repositories.csv)
```

### Deployment

Unpack cake-scripts-clone.zip in any folder, and run initialize.ps1 in it. The working folder where the repositories will be cloned is configurable via a command line argument

### Run samples

Note: Before the very first run the initialize.ps1 script must be run once

``` PowerShell
tools/cake/cake ./clone.cake --git-username=myusername --git-password=mypassword --settings_skipverification=true

tools/cake/cake ./clone.cake --git-username=myusername --git-password=mypassword --work-folder=c:/temp –repository-csv=priority-repsitories.csv
```

### Behavior comments

- If the main folder (for example: C:\Source) does not exist the script creates it
- The repository folder names are inferred from the repository url (last part before the .git)
- The repository folders automatically created
- The "develop" branch is cloned, this is currently hardcoded
- If the repository folder already exist, then the clone for that repository is skipped and a yellow warning message will be displayed to the user. This is because the clone overwrites the file (no merge) so potentially existing work would be destroyed.

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

### Referencing older Cake Core

Error: The assembly 'Cake.CsvHelper is referencing an older version of Cake.Core (0.23.0). This assembly must reference at least Cake.Core version 0.26.0.
An option is to downgrade Cake to an earlier version. It's not recommended, but we can explicitly opt out of assembly verification by configuring the Skip Verification setting to true (i.e. command line parameter "--settings_skipverification=true" see sample commands

## Task 2: Pull latest changes from develop branch

### Function

Build a script to pull the latest changes from a repository and checkout the develop branch.

### Source

```Text
/tools/packages.config  // Standard Cake bootstrap file
/initialize.ps1  // Customized WildGums init file to download Cake
/common.cake  // Reusable Tasks
/pull.cake // Implementation
```

### Command line arguments

```Text
--git-username=...
--git-password=...
```

### Deployment

Unpack cake-scripts.zip to an existing repository’s root folder and run initialize.ps1 in it.
Run samples

Note: Before the very first run the initialize.ps1 script must be run once

```PowerShell
tools/cake/cake ./pull.cake --git-username=myusername --git-password=mypassword --settings_skipverification=true
```

### Behaviour comments

- The repo should not have uncommitted changes for this operation to work, because of the potential branch switch to the develop branch. To prevent loss, the script is checks for uncommitted changes, and in case there are throws exception $"Repository '{repositoryFolder}' has uncommitted changes. Please commit before pulling".
- Object [develop] must be known in the local git config, so the original clone must clone that branch (too).

### Implementation details

The implementation uses Cake build addins:

- #addin nuget:?package=Cake.Git&version=0.16.1&loaddependencies=true
- Because in common.cake there are  method aliases referring to CsvHelper #addin nuget:?package=Cake.CsvHelper&version=0.2.3&loaddependencies=true also must be added.

### Issues (solved)

The very same as in Task 1: Clone.

## Task 3: Restore and update NuGet packages

### Function

Build a script to update packages to the latest versions given a control csv file.

### Source

```Text
/tools/packages.config  // Standard Cake bootstrap file
/initialize.ps1  // Customized WildGums init file to download Cake
/common.cake  // Reusable Tasks
/update-nuget.cake // Implementation
```

### Command line arguments

There are no command line arguments

### Deployment

Unpack cake-scripts.zip to an existing repository’s root folder and run initialize.ps1 in it.

### Run samples

Note: Before the very first run the initialize.ps1 script must be run once

```PowerShell
tools/cake/cake ./update-nuget.cake --settings_skipverification=true
```

### Behaviour comments

- The update task NuGetUpdate() will not work in case there are no existing restored packages, so as a prerequisite dependency a restore task NuGetRestore() also implemented.

### Implementation details

The implementation uses Cake build addins:

- Because in common.cake there are  method aliases referring to Git #addin nuget:?package=Cake.Git&version=0.16.1&loaddependencies=true
- Because in common.cake there are  method aliases referring to CsvHelper #addin nuget:?package=Cake.CsvHelper&version=0.2.3&loaddependencies=true also must be added.

### Issues (partially solved)

Both tasks (restore and update) works OK with packages.config files. Even the WildGums custom /lib packages folder works OK by manually discovering the nuget.config file and configuring the restore task to use that config file.

This can be demonstrated with the repo: https://github.com/pluraltouch/example.git which is a fork of the Cake Example repo.

#### NuGetRestore

The issues begin with the newer .csproj files with     <PackageReference...> Unfortunately the NuGetRestore task can not handle nuget.config  this case. Commenting out the discovery and usage of nuget.config the task works, but restores the packages to a common location. This not fully conforms with the repo based operations what are we implementing.

Still can be solved with  workaround by hardcoding the package folder as /lib to the NuGetRestoreSettings parameter instead of the discovery of the nuget.config.

#### NuGetUpdate

The NuGetUpdate task seems to be hopeless: Hardcoded looking for the packages what are defined in packages.config, and in the NuGetUpdateSettings parameter there are no traces to configure look anywhere else.

## Task 4: Build – Test – Commit - Push

### Function

Build a script to build, run unit tests and if successful commit and push the changes back to the cloud repository

### Source

```Text
/tools/packages.config  // Standard Cake bootstrap file
/initialize.ps1  // Customized WildGums init file to download Cake
/common.cake  // Reusable Tasks
/build-test-commit-push.cake // Implementation
```

### Command line arguments

```Text
--git-username=...
--git-password=...
--configuration =... (defaults to:Debug)
```

### Deployment

Unpack cake-scripts.zip to an existing repository’s root folder and run initialize.ps1 in it.

### Run samples

Note: Before the very first run the initialize.ps1 script must be run once

```PowerShell
tools/cake/cake ./clone.cake --git-username=myusername --git-password=mypassword --settings_skipverification=true

tools/cake/cake ./build-test-commit-push.cake --git-username=myusername --git-password=mypassword --configuration=Release --settings_skipverification=true
```

### Behaviour comments

- Unlike the clean-all, this script only deletes those files what are belonging to the given configuration argument
- The GitCommit task causes errors and breaks the dependency chain if the there are no committable changes. That’s why it cannot be executed unconditionally, so conditional logic checks for uncommitted changes before and if there are no changes the GitCommit is skipped. The user will be informed with a custom message: "There were no uncommitted changes to commit"

### Implementation details

The implementation uses Cake build addins:

- #tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
- #addin nuget:?package=Cake.Git&version=0.16.1&loaddependencies=true
- Because in common.cake there are  method aliases referring to CsvHelper #addin nuget:?package=Cake.CsvHelper&version=0.2.3&loaddependencies=true also must be added.

### Issues (solved)

Exactly the same as in Task 1: Clone

## Task 5: Clean All

### Function

Build a script to delete all bin, obj, and package folders (i.e. all artifacts.)

### Source

```Text
/tools/packages.config  // Standard Cake bootstrap file
/initialize.ps1  // Customized WildGums init file to download Cake
/common.cake  // Reusable Tasks
/clean-all.cake // Implementation
```

### Command line arguments

There are no command line arguments

### Deployment

Unpack cake-scripts.zip to an existing repository’s root folder and run initialize.ps1 in it.

### Run samples

Note: Before the very first run the initialize.ps1 script must be run once

```PowerShell
tools/cake/cake ./clean-all.cake --settings_skipverification=true
```

### Behaviour comments

- Cleans /output, /lib, all bin, all obj, all packages folders

### Implementation details

The implementation uses Cake build addins:

- Because in common.cake there are  method aliases referring to Git #addin nuget:?package=Cake.Git&version=0.16.1&loaddependencies=true
- Because in common.cake there are  method aliases referring to CsvHelper #addin nuget:?package=Cake.CsvHelper&version=0.2.3&loaddependencies=true also must be added.

### Issues

There were no issues

## Task 5+1: All in one

### Function

A bonus script also implemented what is using the make dependency logic and chains the pull->clean->restore-nuget->update-nuget->build->run-unit-test->commit->push logic in one dependency chain.

### Source

```Text
/tools/packages.config  // Standard Cake bootstrap file
/initialize.ps1  // Customized WildGums init file to download Cake
/common.cake  // Reusable Tasks
/main.cake  // Implementation
```

### Command line arguments

```Text
--target=... (defaults to Default, which means pull to push full pipeline)

Other possible values:
    --target=clean, git-pull, clean-all, restore-nuget, update-nuget, build, run-unit-tests, git-commit, git-push (practically same as Default)

--git-username=...
--git-password=...
--configuration =…... (defaults to:Debug)
--do-pull=... (defaults to:false, which means: pull will be skipped by default, user do-pull=true if you want to pull)
```

### Deployment

Unpack cake-scripts.zip to an existing repository’s root folder and run initialize.ps1 in it.

### Run samples

Note: Before the very first run the initialize.ps1 script must be run once

```PowerShell
tools/cake/cake ./main.cake --git-username=myusername --git-password=mypassword --settings_skipverification=true
```

### Behaviour comments

- The dependency chain is   git-pull, clean-all, restore-nuget, update-nuget, build, run-unit-tests, git-commit, git-push. For example using target=git-push executes the whole chain (except pull which would not work it there are uncommitted  changes, so git-pull is opted out bey default. Using target=restore-nuget will clean-all then restore-nuget.

### Implementation details

This all in one script and the individual task scripts are using the very same C# algorithms. To prevent copy and paste all individual logic is implemented in common.cake, and this script and the individual script a loading common.cake via #load common.cake.

The implementation uses two Cake build addins:

- #tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0
- #addin nuget:?package=Cake.CsvHelper&version=0.2.3&loaddependencies=true
- #addin nuget:?package=Cake.Git&version=0.16.1&loaddependencies=true

Note the pinned, not latest versions. The first attempts to use the latest versions resulted internal incompatibility error with the latest internal dependencies, what can not be pinned. See details below:

### Issues

The union of all issues listed in this document except Cake.CsvHelper related.
