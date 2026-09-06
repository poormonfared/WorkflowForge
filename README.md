# WorkflowForge

[![CI](https://github.com/poormonfared/WorkflowForge/actions/workflows/ci.yml/badge.svg)](https://github.com/poormonfared/WorkflowForge/actions/workflows/ci.yml)
[![CodeQL](https://github.com/poormonfared/WorkflowForge/actions/workflows/codeql.yml/badge.svg)](https://github.com/poormonfared/WorkflowForge/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)

WorkflowForge transpiles n8n workflow JSON exports into an **owned, runnable C# project** — no n8n runtime dependency at execution time. Upload a workflow, get back a real `dotnet build`-able Minimal API / Worker Service / Console project you check into your own repo.

## Why

Point-and-click workflow tools are great for prototyping, but running them in production means depending on their runtime forever. WorkflowForge converts the workflow *once* into idiomatic, human-readable C# — after that, it's just your code.

## Architecture

The n8n-parsing layer is decoupled from C# code generation behind an adapter-pattern boundary (`Transpiler.Core`'s `IWorkflowSourceParser` / `IWorkflowCodeGenerator`), so another workflow source (Activepieces, Node-RED, ...) can be added later without touching codegen. The solution itself follows a microservices/vertical-slice layout — one bounded-context service, a YARP gateway in front of it, and a Blazor web app behind the gateway:

```
WorkflowForge.slnx
src/
  BuildingBlocks/BuildingBlocks/        # CQRS abstractions, MediatR pipeline behaviors, exception handling
  Services/Transpiler/
    Transpiler.API/                     # Minimal API host — Carter modules + MediatR vertical slices
    Transpiler.Core/                    # platform-agnostic graph model + adapter/codegen abstractions
    Transpiler.Adapters.N8n/            # n8n JSON -> WorkflowGraph
    Transpiler.CodeGen.CSharp/          # WorkflowGraph -> generated C# project
  ApiGateways/WorkflowForge.Gateway/    # YARP reverse proxy
  WebApps/WorkflowForge.Web/            # Blazor Server upload/generate/download UI
tools/WorkflowForge.Cli/                # in-process CLI over the same libraries (no network hop)
tests/                                  # one test project per library + API integration tests
```

Neither `Transpiler.Adapters.N8n` nor `Transpiler.CodeGen.CSharp` reference each other — only `Transpiler.API` (and separately, `WorkflowForge.Cli`) wire the two together as a composition root.

## Getting started

**Prerequisites:** [.NET 10 SDK](https://dotnet.microsoft.com/download)

```bash
dotnet restore WorkflowForge.slnx
dotnet build WorkflowForge.slnx
dotnet test WorkflowForge.slnx
```

### Run the full stack

In three terminals:

```bash
dotnet run --project src/Services/Transpiler/Transpiler.API      # http://localhost:5200
dotnet run --project src/ApiGateways/WorkflowForge.Gateway        # http://localhost:5204
dotnet run --project src/WebApps/WorkflowForge.Web                # http://localhost:5157
```

Open `http://localhost:5157`, upload an n8n workflow export (see `samples/n8n/`), click **Generate**, then **Download**.

### Or use the CLI

```bash
dotnet run --project tools/WorkflowForge.Cli -- samples/n8n/webhook-example.json ./out
cd out && dotnet run
```

## v1 node coverage

Manual Trigger, Webhook, Schedule Trigger, Set, If, HTTP Request. Anything else is parsed without failing and generated as an explicit `// TODO(WorkflowForge): ...` pass-through stub rather than a hard error — the rest of the workflow still generates and runs.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md), including how to add support for a new node type.

## License

[MIT](LICENSE)
