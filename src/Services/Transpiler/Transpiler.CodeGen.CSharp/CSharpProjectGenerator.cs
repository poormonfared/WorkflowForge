using Transpiler.Core;
using Transpiler.Core.Analysis;
using Transpiler.Core.Model;
using Transpiler.CodeGen.CSharp.Emission;
using Transpiler.CodeGen.CSharp.NodeEmitters;
using Transpiler.CodeGen.CSharp.ProjectShapes;

namespace Transpiler.CodeGen.CSharp;

/// <summary>
/// <see cref="Transpiler.Core.Model.WorkflowGraph"/> -> generated C# project. Implements the
/// <see cref="IWorkflowCodeGenerator"/> boundary; never references <c>Transpiler.Adapters.N8n</c>.
/// </summary>
public sealed class CSharpProjectGenerator : IWorkflowCodeGenerator
{
    public GeneratedProject Generate(WorkflowGraph graph, CodeGenOptions options)
    {
        var order = WorkflowGraphAnalysis.TopologicalOrder(graph);
        var ctx = new CodeGenContext { Graph = graph, TopologicalOrder = order };
        var registry = new NodeEmitterRegistry();
        var nodesById = graph.Nodes.ToDictionary(n => n.Id);

        var topLevel = new List<EmittedNode>();
        foreach (var nodeId in order)
        {
            if (!ctx.ProcessedNodeIds.Add(nodeId))
            {
                continue; // already emitted as part of an If branch
            }

            topLevel.Add(registry.Emit(nodesById[nodeId], graph, ctx));
        }

        var files = graph.Metadata.PrimaryTrigger switch
        {
            TriggerKind.Webhook => MinimalApiProjectShape.Build(graph, topLevel, ctx),
            TriggerKind.Schedule => WorkerProjectShape.Build(graph, topLevel, ctx),
            _ => ConsoleProjectShape.Build(graph, topLevel, ctx),
        };

        return new GeneratedProject(files, ctx.Warnings);
    }
}
