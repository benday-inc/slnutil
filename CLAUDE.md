# CLAUDE.md

Guidance for working in this repository.

## What slnutil is

`slnutil` is a .NET global tool (`dotnet tool`) of local developer-productivity
commands for working with .NET solutions and projects: inspect/edit solutions and
`.csproj` files, format JSON/XML, manage connection strings, run SQL, generate class
diagrams, and more. It is built on [`Benday.CommandsFramework`](https://www.nuget.org/packages/Benday.CommandsFramework)
and multi-targets `net8.0;net9.0;net10.0`.

## Projects

- `Benday.SolutionUtil.Api` — all commands and supporting logic (the real code).
- `Benday.SolutionUtil.ConsoleUi` — thin `Program.cs` entry point; produces the
  `slnutil` executable via `DefaultProgram`.
- `Benday.SolutionUtil.UnitTests` — xUnit v3 tests (run as an executable, see below).

## Command pattern

Each command is a class in `Benday.SolutionUtil.Api` marked with
`[Command(Name = ..., Description = ...)]` and deriving from `SynchronousCommand`
(override `OnExecute`) or `AsynchronousCommand` (`IsAsync = true`, override
`Task OnExecute()`). Commands declare arguments in `GetArguments()` and write output
via `WriteLine(...)` (an injected `ITextOutputProvider`). Command names and argument
names live in `Constants.cs`. Surface user-facing errors as
`Benday.CommandsFramework.KnownException`.

Commands are discovered by reflection at runtime, so adding a `[Command]` class is all
that's needed to register a new CLI command.

## MCP server

slnutil can run as a Model Context Protocol server (`slnutil mcp-server`) exposing
read-only tools to AI clients. Code lives in `Benday.SolutionUtil.Api/Mcp`:

- `McpServerCommand.cs` — the `mcp-server` command. Builds a generic host, adds the
  MCP server with **stdio** transport, sets `ServerInstructions`, and registers the
  tool classes. **stdout is the JSON-RPC transport — nothing may be written to it
  outside the protocol.** Logging is routed to stderr.
- `CommandToolRunner.cs` — runs an existing `CommandBase` in-process with its output
  captured to a string (via `StringBuilderTextOutputProvider`), so a tool can reuse a
  command without duplicating logic. Translates `KnownException` to `McpException`.
- `Mcp/Tools/` — the `[McpServerToolType]` classes. Tool methods are `static`, marked
  `[McpServerTool(Name = ..., ReadOnly = true)]` with a `[Description]` written in
  outcome language (the description is what the model reads to decide when to call it).
  - `SolutionInspectionTools` — solution/project/package/assembly/connection-string reads.
  - `TransformTools` — base64, JSON/XML format (preview), print file, JSON→C# classes.
  - `CliDiscoveryTools` — `discover_cli_commands`, the fallback that lists every CLI
    command (including write/destructive ones that are intentionally not tools).
  - `LearningResourceTools` — `get_learning_resources`, a static topic→links map.
- `McpConfigCommand.cs` / `McpClientSetup.cs` — the `mcp-config` command and the pure,
  unit-tested builders for per-client config JSON and install/uninstall CLI args.

### MCP design rules

- **Read-only only (for now).** Do not expose commands that write files or a database
  as MCP tools. Destructive commands (`devtreeclean`, `runsql`, `deployefmigrations`)
  are excluded entirely; they remain available via the CLI and `discover_cli_commands`.
- **Require explicit absolute paths.** A long-lived server has no meaningful current
  directory. Tool parameters take full paths; don't rely on cwd defaults.
- **No headless-hostile modes.** Skip clipboard and browser/editor-launching behavior
  (e.g. `classdiagram` opens a browser; `classesfromjson` opens an editor). Where a
  command's logic is reusable, call the underlying helper directly instead of the
  command (see `classes_from_json` using `JsonToClassGenerator`).
- Keep `CliCommandCatalog.CommandToMcpTool` in sync as tools are added so
  `discover_cli_commands` can report which commands already have a tool.

## Build & test

- Build a single target: `dotnet build -f net10.0`. In sandboxes without the net8/9
  reference packs, only `net10.0` builds locally; CI covers the others. Use no
  net10-only APIs.
- Tests use **xUnit v3 / Microsoft Testing Platform** and run as an executable, not via
  the VSTest `dotnet test` adapter:
  `dotnet Benday.SolutionUtil.UnitTests/bin/Debug/net10.0/Benday.SolutionUtil.UnitTests.dll`
  Filter one test with `-filter "/*/*/FixtureName/MethodName"`.
- Smoke-test the MCP server over stdio (no live services needed):
  ```bash
  printf '%s\n' \
   '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"t","version":"1.0"}}}' \
   '{"jsonrpc":"2.0","method":"notifications/initialized"}' \
   '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}' \
   | (cat; sleep 2) | dotnet slnutil.dll mcp-server 2>/dev/null
  ```
  stdout must contain only JSON-RPC responses.

## READMEs are generated

`README.md` and `README-for-nuget.md` are generated: hand-written intro from
`misc/readme-header.md` + an auto-generated commands table. To change them, edit
`misc/readme-header.md`, run the `MarkdownUsageFormatterFixture.GenerateReadmeFiles`
test, then copy `generated-readme-files/*` to the repo root (see
`update-readme-files-from-generated.sh`). Do not hand-edit the generated files.
