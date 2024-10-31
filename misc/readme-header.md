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
