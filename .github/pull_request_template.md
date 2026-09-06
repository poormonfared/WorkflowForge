## Summary

<!-- What does this change, and why? -->

## Related issue

<!-- Closes #... , or "N/A" -->

## Test plan

- [ ] `dotnet test WorkflowForge.slnx` passes
- [ ] Added/updated tests for this change
- [ ] If this adds/changes node support: tested against a real (or trimmed) n8n export
- [ ] If this changes generated output: confirmed the generated project still `dotnet build`s

## Checklist

- [ ] PR is focused on one change (see [CONTRIBUTING.md](../CONTRIBUTING.md))
- [ ] `Transpiler.Adapters.N8n` and `Transpiler.CodeGen.CSharp` still don't reference each other
