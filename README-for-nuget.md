# slnutil
A collection of utilities for working with .NET Core Solutions and Projects.

Written by Benjamin Day  
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer  
https://www.benday.com  
info@benday.com 

*Got ideas for utilities you'd like to see? Found a bug? Let us know by submitting an issue https://github.com/benday-inc/slnutil/issues*. *Want to contribute? Submit a pull request.*

## Installing
The slnutil is distributed as a .NET Core Tool via NuGet. To install it go to the command prompt and type  
`dotnet tool install slnutil -g`

### Prerequisites
- You'll need to install .NET Core 7 from https://dotnet.microsoft.com/

## Commands
| Command Name | Description |
| --- | --- |
| rename | Bulk rename for files and folders. |
| cleanreferences | Simplifies package references in a C# project file. Mostly this fixes stuff in the EF Core references that breaks Azure DevOps & GitHub builds like PrivateAssets and IncludeAssets directives. |
| devtreeclean | Clean development folder tree. Removes node_modules, .git, bin, obj, and TestResults folders. |
| findsolutions | Find solution files in a folder tree. |
| getconnectionstring | Get database connection string in appsettings.json. |
| listsolutionprojects | Gets list of projects in a solution. |
| replacetoken | Replace token in file. |
| setconnectionstring | Set database connection string in appsettings.json. |
| setframework | Set the target framework version on all projects. |
| setjsonvalue | Set a string value in a json file. |
| base64 | Encodes a string value as a base 64 string. |
| validateconnectionstring | Validate that specified connection string can connect to SQL Server. |
## rename
**Bulk rename for files and folders.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Optional | String | Starting directory for the rename operation |
| from | Required | String | String to search for and replace |
| to | Required | String | Replacement value |
| preview | Optional | Boolean | Preview changes |
| recursive | Optional | Boolean | Recurse the directory tree |
## cleanreferences
**Simplifies package references in a C# project file. Mostly this fixes stuff in the EF Core references that breaks Azure DevOps & GitHub builds like PrivateAssets and IncludeAssets directives.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution file to use |
| preview | Optional | Boolean | Preview changes only |
## devtreeclean
**Clean development folder tree. Removes node_modules, .git, bin, obj, and TestResults folders.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Optional | String | Starting directory. If not supplied, the tool uses the current directory. |
## findsolutions
**Find solution files in a folder tree.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Required | String | Path to start search from |
| listprojects | Optional | Boolean | List projects in solutions |
| csv | Optional | Boolean | Output results as comma-separated values |
| skipreferences | Optional | Boolean | Output results as comma-separated values |
## getconnectionstring
**Get database connection string in appsettings.json.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Optional | String | Path to json config file |
| name | Required | String | Name of the connection string to get |
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
## base64
**Encodes a string value as a base 64 string.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| value | Required | String | Value to encode as base64 |
## validateconnectionstring
**Validate that specified connection string can connect to SQL Server.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Optional | String | Path to json config file |
| name | Required | String | Name of the connection string to validate |
