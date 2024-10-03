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
- You'll need to install .NET Core 8 from https://dotnet.microsoft.com/

## Commands
| Command Name | Description |
| --- | --- |
| [assemblyinfo](#assemblyinfo) | View assembly info for a DLL. |
| [rename](#rename) | Bulk rename for files and folders. |
| [cleanreferences](#cleanreferences) | Simplifies package references in a C# project file. Mostly this fixes stuff in the EF Core references that breaks Azure DevOps & GitHub builds like PrivateAssets and IncludeAssets directives. |
| [deployefmigrations](#deployefmigrations) | Deploy EF Core Migrations from DLL binaries. |
| [devtreeclean](#devtreeclean) | Clean development folder tree. Removes node_modules, .git, bin, obj, and TestResults folders. |
| [findsolutions](#findsolutions) | Find solution files in a folder tree. |
| [getconnectionstring](#getconnectionstring) | Get database connection string in appsettings.json. |
| [classesfromjson](#classesfromjson) | Create C# classes from JSON with serialization attributes for System.Text.Json. |
| [listpackages-oldstyle](#listpackages-oldstyle) | Lists packages referenced in legacy style packages.config files. |
| [listsolutionprojects](#listsolutionprojects) | Gets list of projects in a solution. |
| [replacetoken](#replacetoken) | Replace token in file. |
| [setconnectionstring](#setconnectionstring) | Set database connection string in appsettings.json. |
| [setframework](#setframework) | Set the target framework version on all projects. |
| [setjsonvalue](#setjsonvalue) | Set a string value in a json file. |
| [setprojectproperty](#setprojectproperty) | Set a project property value on all projects. |
| [setprojectversion](#setprojectversion) | Set the assembly and nuget package version property value on a project. |
| [base64](#base64) | Encodes a string value as a base 64 string. |
| [touch](#touch) | Modifies a file's date to current date time or creates a new empty file if it doesn't exist. |
| [validateconnectionstring](#validateconnectionstring) | Validate that specified connection string can connect to SQL Server. |
| [wildcardreference](#wildcardreference) | Changes package references in a C# project file to use wildcard version rather than fixed version number. |
## <a name="assemblyinfo"></a> assemblyinfo
**View assembly info for a DLL.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Required | String | Assembly to view |
## <a name="rename"></a> rename
**Bulk rename for files and folders.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Optional | String | Starting directory for the rename operation |
| from | Required | String | String to search for and replace |
| to | Required | String | Replacement value |
| preview | Optional | Boolean | Preview changes |
| recursive | Optional | Boolean | Recurse the directory tree |
## <a name="cleanreferences"></a> cleanreferences
**Simplifies package references in a C# project file. Mostly this fixes stuff in the EF Core references that breaks Azure DevOps & GitHub builds like PrivateAssets and IncludeAssets directives.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution file to use |
| preview | Optional | Boolean | Preview changes only |
## <a name="deployefmigrations"></a> deployefmigrations
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
## <a name="devtreeclean"></a> devtreeclean
**Clean development folder tree. Removes node_modules, .git, bin, obj, and TestResults folders.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Optional | String | Starting directory. If not supplied, the tool uses the current directory. |
| keepgit | Optional | Boolean | If true, skips delete of .git folders and preserves any git repositories. Default value is true. Set this value to false to delete .git folders. |
## <a name="findsolutions"></a> findsolutions
**Find solution files in a folder tree.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Required | String | Path to start search from |
| listprojects | Optional | Boolean | List projects in solutions |
| csv | Optional | Boolean | Output results as comma-separated values |
| skipreferences | Optional | Boolean | Output results as comma-separated values |
## <a name="getconnectionstring"></a> getconnectionstring
**Get database connection string in appsettings.json.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Optional | String | Path to json config file |
| name | Required | String | Name of the connection string to get |
## <a name="classesfromjson"></a> classesfromjson
**Create C# classes from JSON with serialization attributes for System.Text.Json.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
## <a name="listpackages-oldstyle"></a> listpackages-oldstyle
**Lists packages referenced in legacy style packages.config files.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Optional | String | Path to start search from |
## <a name="listsolutionprojects"></a> listsolutionprojects
**Gets list of projects in a solution.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution to examine. If this value is not supplied, the tool searches for a sln file automatically. |
| pathonly | Optional | Boolean | Only show the project paths. Don't show the framework versions. |
## <a name="replacetoken"></a> replacetoken
**Replace token in file.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Required | String | Path to file |
| token | Required | String | Token to replace |
| value | Required | String | String value to set |
## <a name="setconnectionstring"></a> setconnectionstring
**Set database connection string in appsettings.json.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Optional | String | Path to json config file |
| name | Required | String | Name of the connection string to set |
| value | Required | String | Connection string value |
## <a name="setframework"></a> setframework
**Set the target framework version on all projects.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution to examine. If this value is not supplied, the tool searches for a sln file automatically. |
| version | Required | String | Framework version to set projects to. |
## <a name="setjsonvalue"></a> setjsonvalue
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
## <a name="setprojectproperty"></a> setprojectproperty
**Set a project property value on all projects.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution to examine. If this value is not supplied, the tool searches for a sln file automatically. |
| propertyname | Required | String | Name of the property to set. |
| propertyvalue | Required | String | Value for the property. |
## <a name="setprojectversion"></a> setprojectversion
**Set the assembly and nuget package version property value on a project.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution to examine. If this value is not supplied, the tool searches for a sln file automatically. |
| project | Required | String | Project name to update. |
| increment | Optional | Boolean | Increment the minor value. |
| value | Optional | String | Value for the property. |
## <a name="base64"></a> base64
**Encodes a string value as a base 64 string.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| value | Required | String | Value to encode as base64 |
## <a name="touch"></a> touch
**Modifies a file's date to current date time or creates a new empty file if it doesn't exist.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Required | String | Path to file |
## <a name="validateconnectionstring"></a> validateconnectionstring
**Validate that specified connection string can connect to SQL Server.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| filename | Optional | String | Path to json config file |
| name | Required | String | Name of the connection string to validate |
## <a name="wildcardreference"></a> wildcardreference
**Changes package references in a C# project file to use wildcard version rather than fixed version number.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution file to use |
| preview | Optional | Boolean | Preview changes only |
| filter | Optional | String | Filter package by name. If package name starts with this value, it gets updated. |
