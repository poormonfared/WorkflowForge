# Contributing to WorkflowForge

Thanks for taking a look. This project is young — most useful contributions right now are new node-type support and bug reports against real n8n exports.

## Development setup

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- `dotnet restore WorkflowForge.slnx`

## Build, test, run

```bash
dotnet build WorkflowForge.slnx
dotnet test WorkflowForge.slnx
```

Run a single test project while iterating:

```bash
dotnet test tests/Transpiler.CodeGen.CSharp.Tests
```

See the [README](README.md#run-the-full-stack) for running the API/gateway/web app or the CLI.

## Project layout and boundaries

- `Transpiler.Core` has no dependencies and defines the seam: `IWorkflowSourceParser` and `IWorkflowCodeGenerator`.
- `Transpiler.Adapters.N8n` and `Transpiler.CodeGen.CSharp` each depend **only** on `Transpiler.Core` — never on each other. If you find yourself wanting to reference one from the other, that's a sign the change belongs in `Transpiler.Core` instead.
- `Transpiler.API` is the only project allowed to know about concrete adapters and generators (it's the composition root), alongside `WorkflowForge.Cli`.

Please keep that boundary intact — it's the whole point of the adapter pattern here, and it's what will let a future source (Activepieces, Node-RED, ...) be added as a new `Transpiler.Adapters.*` project without touching code generation.

## Adding support for a new n8n node type

Node support is split into two independent halves — you usually only need one of them if the node already round-trips as an `Unsupported` pass-through stub and you're just improving its translation.

**Parsing side** (`Transpiler.Adapters.N8n/NodeHandlers/`):
1. Add a class implementing `IN8nNodeHandler` — see `HttpRequestHandler.cs` for a template.
2. List every n8n type string (and version aliases) it handles in `SupportedTypes`.
3. Register it in `NodeHandlerRegistry.BuildRegistry()`.

**Codegen side** (`Transpiler.CodeGen.CSharp/NodeEmitters/`):
1. Add a class implementing `INodeEmitter` — see `HttpRequestEmitter.cs` for a template.
2. Register it in `NodeEmitterRegistry`'s constructor.
3. If the node can branch (like `If`), look at `IfEmitter` first — C#'s block scoping means a branch's `var` declarations can't escape an `if`/`else`, so branching emitters need to assign into a merge variable declared *before* the block. This trips people up; it's called out in `IfEmitter`'s doc comment.

Either side falls back gracefully if you only implement one: an unrecognized parse-side type becomes `NodeKind.Unsupported` (still valid, still wired into the graph); an unrecognized codegen-side type becomes a `// TODO(WorkflowForge): ...` pass-through stub, never a thrown exception. Generated code should always compile and run, even with stubs in it.

Add a test in `Transpiler.Adapters.N8n.Tests` and/or `Transpiler.CodeGen.CSharp.Tests` alongside your handler/emitter.

## Expression translation

n8n's `={{ $json.field }}` expression syntax is only partially translated by design (see `Transpiler.CodeGen.CSharp/Emission/ExpressionTranslator.cs`) — `$json.field` variable references resolve to real C#, everything else (ternaries, arrow-function array methods, template literals) becomes a `TODO` placeholder rather than a guess. Expanding this is welcome, but keep the same rule: never emit code that might be *wrong* silently — an honest stub beats a plausible-looking mistranslation.

## Pull requests

- Add tests for behavior changes; `dotnet test WorkflowForge.slnx` must pass.
- Keep PRs focused — a new node handler, a bug fix, a doc update. Avoid bundling unrelated changes.
- Describe what n8n export (or a trimmed-down version of it) you tested against, if relevant.

## Code of conduct

This project follows the [Contributor Covenant](CODE_OF_CONDUCT.md).
