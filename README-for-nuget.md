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
| cleanreferences | Simplifies package references in a C# project file. Mostly this fixes stuff in the EF Core references that breaks Azure DevOps & GitHub builds like PrivateAssets and IncludeAssets directives. |
| findsolutions | Find solution files in a folder tree. |
| listsolutionprojects | Gets list of projects in a solution. |
| base64 | Encodes a string value as a base 64 string. |
## cleanreferences
**Simplifies package references in a C# project file. Mostly this fixes stuff in the EF Core references that breaks Azure DevOps & GitHub builds like PrivateAssets and IncludeAssets directives.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| solutionpath | Optional | String | Solution file to use |
| preview | Optional | Boolean | Preview changes only |
## findsolutions
**Find solution files in a folder tree.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| rootdir | Required | String | Path to start search from |
| listprojects | Optional | Boolean | List projects in solutions |
| csv | Optional | Boolean | Output results as comma-separated values |
| skipreferences | Optional | Boolean | Output results as comma-separated values |
## listsolutionprojects
**Gets list of projects in a solution.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
## base64
**Encodes a string value as a base 64 string.**
### Arguments
| Argument | Is Optional | Data Type | Description |
| --- | --- | --- | --- |
| value | Required | String | Value to encode as base64 |
