# slnutil
A collection of utilities for working with .NET Core Solutions and Projects.

Written by Benjamin Day  
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer  
https://www.benday.com  
https://www.slidespeaker.ai  
info@benday.com  
YouTube: https://www.youtube.com/@_benday  

## Key features

* Create solutions and projects with unit tests &amp; integration tests for... 
    * [ASP.NET Web API](https://dotnet.microsoft.com/en-us/apps/aspnet/apis) Projects with [xUnit](https://xunit.net)
    * [ASP.NET MVC](https://dotnet.microsoft.com/en-us/apps/aspnet/mvc) Projects with xUnit
    * [.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui?view=net-maui-8.0) Projects with xUnit &amp; the [.NET MAUI Community Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/)
    * .NET MAUI Projects with sample application, viewmodels, and xunit tests. This uses the [Benday.Presentation.Controls](https://www.nuget.org/packages/Benday.Presentation.Controls) &amp; [Benday.Presentation](https://www.nuget.org/packages/Benday.Presentation) libraries.
    * .NET Core Console application
    * Commands Utility application using [Benday.CommandsFramework](https://www.nuget.org/packages/Benday.CommandsFramework). This helps you to quickly write CLI utilities that run as a [dotnet tool](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install). 
* Create class diagrams for all or part of a project using [Mermaid](https://mermaid.js.org)
* Update the .NET Framework version for all projects in a solution
* Set or increment the assembly version for a project
* Set a project property value in a csproj file
* Deploy [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) [Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli) from a DLL (aka. deploy migrations without the source code)
* Validate a connection string against SQL Server to make sure it connects
* Bulk rename files and folders
* Edit json from the command line
* Set the connection string in appsettings.json form the command line
* And lots more...

## Suggestions, Problems, or Bugs?

*Got ideas for utilities you'd like to see? Found a bug? Let us know by submitting an issue https://github.com/benday-inc/slnutil/issues*. *Want to contribute? Submit a pull request.*

## Installing
The slnutil is distributed as a .NET Core Tool via NuGet. To install it go to the command prompt and type  
`dotnet tool install slnutil -g`

### Prerequisites
- You'll need to install .NET Core 8 from https://dotnet.microsoft.com/

## Commands
| Command Name | Description |
| --- | --- |
| assemblyinfo | View assembly info for a DLL. |
| rename | Bulk rename for files and folders. |
| classdiagram | Generate a class diagram for an assembly. |
| cleanreferences | Simplifies package references in a C# project file. Mostly this fixes stuff in the EF Core references that breaks Azure DevOps & GitHub builds like PrivateAssets and IncludeAssets directives. |
| createsolution | Create a solution and projects |
| deployefmigrations | Deploy EF Core Migrations from DLL binaries. |
| devtreeclean | Clean development folder tree. Removes node_modules, .git, bin, obj, and TestResults folders. |
| findsolutions | Find solution files in a folder tree. |
| formatxml | Formats XML files |
| getconnectionstring | Get database connection string in appsettings.json. |
| classesfromjson | Create C# classes from JSON with serialization attributes for System.Text.Json. |
| listpackages-oldstyle | Lists packages referenced in legacy style packages.config files. |
| listsolutionprojects | Gets list of projects in a solution. |
| replacetoken | Replace token in file. |
| setconnectionstring | Set database connection string in appsettings.json. |
| setframework | Set the target framework version on all projects. |
| setjsonvalue | Set a string value in a json file. |
| setpackageversion | Changes NuGet package references in a C# project file to a new value. |
| setprojectproperty | Set a project property value on all projects. |
| setprojectversion | Set the assembly and nuget package version property value on a project. |
| sortpropertygroup | Sorts the elements in a property group for a csproj file. |
| sortreferences | Sorts the references in an item group for a csproj file. |
| base64 | Encodes a string value as a base 64 string. |
| touch | Modifies a file's date to current date time or creates a new empty file if it doesn't exist. |
| updatebicepversions | Reads bicep file or files and updates the api versions to latest. |
| validateconnectionstring | Validate that specified connection string can connect to SQL Server. |
| wildcardreference | Changes package references in a C# project file to use wildcard version rather than fixed version number. |
## assemblyinfo
**View assembly info for a DLL.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Required | String | Assembly to view |
## rename
**Bulk rename for files and folders.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Optional | String | Starting directory for the rename operation |
| from | Required | String | String to search for and replace |
| to | Required | String | Replacement value |
| prefix | Optional | String | Prefix all matching values with this value |
| preview | Optional | Boolean | Preview changes |
| recursive | Optional | Boolean | Recurse the directory tree |
## classdiagram
**Generate a class diagram for an assembly.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| namespace | Optional | String | Filter by namespace |
| filename | Required | String | Path to assembly that you want a class diagram for. |
| hideinheritance | Optional | Boolean | Hide inheritance relationships. |
| typenamesmatchexact | Optional | Boolean | Exact match for type names.  Default is contains. |
| localonly | Optional | Boolean | Only show methods and properties that are declared locally. Don't show inherited members. |
| typenames | Optional | String | Show types that exist in this comma separated list. Default search is contains match that matches by substring. |
## cleanreferences
**Simplifies package references in a C# project file. Mostly this fixes stuff in the EF Core references that breaks Azure DevOps & GitHub builds like PrivateAssets and IncludeAssets directives.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution file to use |
| preview | Optional | Boolean | Preview changes only |
## createsolution
**Create a solution and projects**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Optional | String | Starting directory. If not supplied, the tool uses the current directory. |
| type | Required | String | Type of solution to create. Valid values are: webapi, mvc, console, commands, maui, maui-demo |
| namespace | Required | String | This is the root namespace for the solution.  For example: Benday.SampleApp |
| verbose | Optional | Boolean | verbose |
## deployefmigrations
**Deploy EF Core Migrations from DLL binaries.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| binariesdir | Optional | String | Path to EF Core migration binaries. Defaults to current directory. |
| startupdll | Optional | String | Path to EF Core startup DLL. |
| migrationsdll | Optional | String | Path to EF Core migrations DLL. |
| dbcontextname | Optional | String | Name of the EF Core DbContext class. |
| namespace | Optional | String | Root namespace of the EF Core migrations DLL. |
| verbose | Optional | Boolean | Output results as comma-separated values |
## devtreeclean
**Clean development folder tree. Removes node_modules, .git, bin, obj, and TestResults folders.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Optional | String | Starting directory. If not supplied, the tool uses the current directory. |
| keepgit | Optional | Boolean | If true, skips delete of .git folders and preserves any git repositories. Default value is true. Set this value to false to delete .git folders. |
| keepnodemodules | Optional | Boolean | If true, skips delete of node_modules folders. Default value is false. |
## findsolutions
**Find solution files in a folder tree.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Required | String | Path to start search from |
| listprojects | Optional | Boolean | List projects in solutions |
| csv | Optional | Boolean | Output results as comma-separated values |
| skipreferences | Optional | Boolean | Output results as comma-separated values |
## formatxml
**Formats XML files**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Optional | String | Path to file or wildcard to files |
| recursive | Optional | Boolean | Apply to matching files recursively |
| write | Optional | Boolean | Write changes back to the file |
## getconnectionstring
**Get database connection string in appsettings.json.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Optional | String | Path to json config file |
| name | Required | String | Name of the connection string to get |
## classesfromjson
**Create C# classes from JSON with serialization attributes for System.Text.Json.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
## listpackages-oldstyle
**Lists packages referenced in legacy style packages.config files.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Optional | String | Path to start search from |
## listsolutionprojects
**Gets list of projects in a solution.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution to examine. If this value is not supplied, the tool searches for a sln file automatically. |
| pathonly | Optional | Boolean | Only show the project paths. Don't show the framework versions. |
## replacetoken
**Replace token in file.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Required | String | Path to file |
| token | Required | String | Token to replace |
| value | Required | String | String value to set |
## setconnectionstring
**Set database connection string in appsettings.json.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Optional | String | Path to json config file |
| name | Required | String | Name of the connection string to set |
| value | Required | String | Connection string value |
## setframework
**Set the target framework version on all projects.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution to examine. If this value is not supplied, the tool searches for a sln file automatically. |
| version | Required | String | Framework version to set projects to. |
## setjsonvalue
**Set a string value in a json file.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Optional | String | Path to json config file |
| level1 | Required | String | First level json property name to set |
| level2 | Optional | String | Second level json property name to set |
| level3 | Optional | String | Third level json property name to set |
| level4 | Optional | String | Fourth level json property name to set |
| value | Required | String | String value to set |
| increment-int | Optional | Boolean | Increment the existing value or use the '/value' as the value if it does not exist or isn't an integer. |
| increment-minor-version | Optional | Boolean | Increment the minor version of the existing value or use the '/value' as the value if it does not exist or isn't an integer. |
| bool | Optional | Boolean | Set value into the json as boolean |
## setpackageversion
**Changes NuGet package references in a C# project file to a new value.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution file to use |
| preview | Optional | Boolean | Preview changes only |
| filter | Optional | String | Filter package by name. If package name starts with this value, it gets updated. |
| version | Required | String | Package version to reference |
## setprojectproperty
**Set a project property value on all projects.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution to examine. If this value is not supplied, the tool searches for a sln file automatically. |
| propertyname | Required | String | Name of the property to set. |
| propertyvalue | Required | String | Value for the property. |
## setprojectversion
**Set the assembly and nuget package version property value on a project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution to examine. If this value is not supplied, the tool searches for a sln file automatically. |
| project | Required | String | Project name to update. |
| increment | Optional | Boolean | Increment the minor value. |
| value | Optional | String | Value for the property. |
## sortpropertygroup
**Sorts the elements in a property group for a csproj file.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| project | Optional | String | Project file to edit. If this value is not supplied, the tool looks for a csproj file in the current directory. |
## sortreferences
**Sorts the references in an item group for a csproj file.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| project | Optional | String | Project file to edit. If this value is not supplied, the tool looks for a csproj file in the current directory. |
## base64
**Encodes a string value as a base 64 string.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| value | Required | String | Value to encode as base64 |
## touch
**Modifies a file's date to current date time or creates a new empty file if it doesn't exist.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Required | String | Path to file |
## updatebicepversions
**Reads bicep file or files and updates the api versions to latest.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Optional | String | Name of the bicep file, if you want to update just one file. |
| previewversions | Optional | Boolean | Allow preview versions for resources. |
| preview | Optional | Boolean | Do not save changes only preview the changes. |
## validateconnectionstring
**Validate that specified connection string can connect to SQL Server.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Optional | String | Path to json config file |
| name | Required | String | Name of the connection string to validate |
## wildcardreference
**Changes package references in a C# project file to use wildcard version rather than fixed version number.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution file to use |
| preview | Optional | Boolean | Preview changes only |
| filter | Required | String | Filter package by name. If package name starts with this value, it gets updated. |
