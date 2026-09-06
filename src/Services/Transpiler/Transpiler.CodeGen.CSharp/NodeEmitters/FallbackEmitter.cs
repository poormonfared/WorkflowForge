using Transpiler.Core;
using Transpiler.Core.Model;
using Transpiler.CodeGen.CSharp.Emission;

namespace Transpiler.CodeGen.CSharp.NodeEmitters;

/// <summary>
/// Used for any <see cref="NodeKind.Unsupported"/> node. Forwards the previous item unchanged
/// with a TODO comment — never throws — so the generated program always runs end-to-end even
/// with stubs present. Mirrors the reference Python transpiler's fallback handler exactly.
/// </summary>
public sealed class FallbackEmitter : INodeEmitter
{
    public IReadOnlyList<string> SupportedSourceTypes { get; } = [];

    public static readonly FallbackEmitter Instance = new();

    public EmittedNode Emit(WorkflowNode node, WorkflowGraph graph, CodeGenContext ctx)
    {
        var varName = ctx.Names.Register(node.Id, node.Name);
        var previous = ctx.Names.CurrentVariable;
        ctx.Names.AdvanceCurrent(varName);

        ctx.Warn($"Unsupported node type '{node.SourceTypeIdentifier}' — generated as pass-through stub.", node.Id);

        return new EmittedNode
        {
            NodeId = node.Id,
            Kind = EmittedNodeKind.Statement,
            Comment = $"TODO(WorkflowForge): implement '{node.Name}' (type: {node.SourceTypeIdentifier})",
            Lines = [$"var {varName} = {previous}; // pass-through stub — no-op until implemented"],
        };
    }
}
