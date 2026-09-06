using Transpiler.Core;
using Transpiler.Core.Model;
using Transpiler.CodeGen.CSharp.Emission;

namespace Transpiler.CodeGen.CSharp.NodeEmitters;

/// <summary>
/// One small class per source node type, mirroring <c>Transpiler.Adapters.N8n</c>'s handler
/// shape on the other side of the pipeline. Dispatched by <see cref="NodeEmitterRegistry"/> off
/// <see cref="WorkflowNode.SourceTypeIdentifier"/>. Depends only on <c>Transpiler.Core</c> —
/// never on the n8n adapter project.
/// </summary>
public interface INodeEmitter
{
    IReadOnlyList<string> SupportedSourceTypes { get; }

    EmittedNode Emit(WorkflowNode node, WorkflowGraph graph, CodeGenContext ctx);
}
