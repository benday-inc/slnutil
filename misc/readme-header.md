# slnutil
A collection of utilities for working with .NET Solutions and Projects (.sln and .slnx).

[![NuGet](https://img.shields.io/nuget/v/slnutil.svg)](https://www.nuget.org/packages/slnutil/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/slnutil.svg)](https://www.nuget.org/packages/slnutil/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Written by Benjamin Day  
Pluralsight Author | Microsoft MVP
https://www.benday.com  
https://www.honestcheetah.com  
info@benday.com  
YouTube: https://www.youtube.com/@_benday  

## Key features

* Create solutions and projects with unit tests & integration tests for... 
    * [ASP.NET Web API](https://dotnet.microsoft.com/en-us/apps/aspnet/apis) Projects with [xUnit](https://xunit.net)
    * [ASP.NET MVC](https://dotnet.microsoft.com/en-us/apps/aspnet/mvc) Projects with xUnit
    * [.NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/what-is-maui?view=net-maui-8.0) Projects with xUnit & the [.NET MAUI Community Toolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/maui/)
    * .NET MAUI Projects with sample application, viewmodels, and xunit tests. This uses the [Benday.Presentation.Controls](https://www.nuget.org/packages/Benday.Presentation.Controls) & [Benday.Presentation](https://www.nuget.org/packages/Benday.Presentation) libraries.
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
* Set the connection string in appsettings.json from the command line
* Generate C# classes from JSON
* Run SQL commands or script files against SQL Server
* Check and update versions in Bicep files
* Format XML files (single file or recursive)
* View assembly information
* Run as an [MCP (Model Context Protocol)](https://modelcontextprotocol.io) server so AI assistants can inspect your solutions, projects, and files
* And lots more...

## MCP Server (AI assistant integration)

slnutil can run as an [MCP (Model Context Protocol)](https://modelcontextprotocol.io)
server so that AI assistants — Claude Code, Claude Desktop, VS Code, Visual Studio,
Cursor — can call its read-only inspection tools directly.

### What's exposed

All MCP tools are **read-only** and require explicit **absolute paths** (there is no
current-directory default and no stored configuration). Tools that write to files or
a database are intentionally *not* exposed; use the command line for those.

| MCP tool | What it does |
| --- | --- |
| `list_solution_projects` | List the projects in a solution with their target frameworks |
| `find_solutions` | Find `.sln`/`.slnx` files under a directory (optionally list projects) |
| `list_packages_config` | Find legacy `packages.config` NuGet references under a directory |
| `get_assembly_info` | Show assembly metadata for a compiled `.dll` |
| `get_connection_string` | Read a named connection string from a config file |
| `validate_connection_string` | Test that a connection string can connect to SQL Server |
| `base64_encode` | Encode a string as base64 |
| `format_json` | Pretty-print a JSON file (preview only) |
| `format_xml` | Pretty-print an XML file (preview only) |
| `print_file` | Print a file character-by-character for encoding diagnosis |
| `classes_from_json` | Generate C# classes from a JSON document |
| `discover_cli_commands` | List every slnutil command line command (the fallback for anything not exposed as a tool, including write/destructive commands) |
| `get_learning_resources` | Look up video tutorials and articles for a topic |

### Setup

Print ready-to-paste configuration for your client:

```
slnutil mcp-config
```

Or target a single client (`claudecode`, `claudedesktop`, `vscode`, `visualstudio`, `cursor`):

```
slnutil mcp-config /client:claudecode
```

For Claude Code and VS Code you can register the server at user scope automatically:

```
slnutil mcp-config /client:claudecode /install
```

Under the hood, clients launch `slnutil mcp-server`, which speaks JSON-RPC over stdio.
You normally don't run `mcp-server` yourself — the MCP client starts it for you.

## Suggestions, Problems, or Bugs?

*Got ideas for utilities you'd like to see? Found a bug? Let us know by submitting an issue https://github.com/benday-inc/slnutil/issues*. *Want to contribute? Submit a pull request.*

## Installing
The slnutil is distributed as a .NET Core Tool via NuGet. To install it go to the command prompt and type  
`dotnet tool install slnutil -g`

