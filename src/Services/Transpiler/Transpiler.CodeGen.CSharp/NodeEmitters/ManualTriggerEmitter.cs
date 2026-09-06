using Transpiler.Core;
using Transpiler.Core.Model;
using Transpiler.CodeGen.CSharp.Emission;

namespace Transpiler.CodeGen.CSharp.NodeEmitters;

public sealed class ManualTriggerEmitter : INodeEmitter
{
    public IReadOnlyList<string> SupportedSourceTypes { get; } = ["n8n-nodes-base.manualtrigger", "n8n-nodes-base.start"];

    public EmittedNode Emit(WorkflowNode node, WorkflowGraph graph, CodeGenContext ctx)
    {
        var varName = ctx.Names.Register(node.Id, node.Name);
        ctx.Names.AdvanceCurrent(varName);

        return new EmittedNode
        {
            NodeId = node.Id,
            Kind = EmittedNodeKind.Statement,
            Comment = "Manual trigger — workflow starts here",
            Lines = [$"var {varName} = new JsonObject();"],
        };
    }
}
