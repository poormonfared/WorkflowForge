using Transpiler.Core;
using Transpiler.Core.Analysis;
using Transpiler.Core.Model;
using Transpiler.CodeGen.CSharp.Emission;

namespace Transpiler.CodeGen.CSharp.NodeEmitters;

/// <summary>
/// n8n's If node. Condition translation is deliberately narrow for v1 (matches the plan's
/// expression-scope call): only the legacy <c>condition: {value1, operation, value2}</c> shape is
/// translated to a real C# comparison; anything else (including n8n's current
/// <c>conditions.conditions[]</c> schema) falls back to a TODO placeholder condition rather than
/// guessing — the branch wiring itself (which downstream nodes belong to which side) is always
/// correct regardless, since that comes from the connection graph, not the condition parameters.
/// </summary>
/// <remarks>
/// A branch's own <c>var x = ...</c> declarations are scoped to that C# block and can't be read
/// after the if/else — unlike the reference Python transpiler, which has no such scoping issue.
/// So each branch's final value is assigned into one merge variable declared *before* the if,
/// which becomes <see cref="NameRegistry.CurrentVariable"/> for whatever follows.
/// </remarks>
public sealed class IfEmitter(NodeEmitterRegistry registry) : INodeEmitter
{
    public IReadOnlyList<string> SupportedSourceTypes { get; } = ["n8n-nodes-base.if"];

    public EmittedNode Emit(WorkflowNode node, WorkflowGraph graph, CodeGenContext ctx)
    {
        var condition = TranslateCondition(node, ctx);
        var incomingVar = ctx.Names.CurrentVariable;
        ctx.UsingDirectives.Add("System.Text.Json.Nodes");

        var trueStart = graph.Connections
            .Where(c => c.SourceNodeId == node.Id && c.Type == ConnectionType.Main && c.SourceOutputIndex == 0)
            .Select(c => c.TargetNodeId).FirstOrDefault();
        var falseStart = graph.Connections
            .Where(c => c.SourceNodeId == node.Id && c.Type == ConnectionType.Main && c.SourceOutputIndex == 1)
            .Select(c => c.TargetNodeId).FirstOrDefault();

        var mergePoint = WorkflowGraphAnalysis.FindMergePoint(graph, node.Id);
        var mergedVar = ctx.Names.Register(node.Id, node.Name);

        ctx.Names.AdvanceCurrent(incomingVar);
        var trueBody = EmitBranch(trueStart, mergePoint, graph, ctx);
        trueBody.Add(AssignMerged(mergedVar, ctx.Names.CurrentVariable));

        ctx.Names.AdvanceCurrent(incomingVar);
        var falseBody = EmitBranch(falseStart, mergePoint, graph, ctx);
        falseBody.Add(AssignMerged(mergedVar, ctx.Names.CurrentVariable));

        ctx.Names.AdvanceCurrent(mergedVar);

        return new EmittedNode
        {
            NodeId = node.Id,
            Kind = EmittedNodeKind.IfBranch,
            Comment = $"If: {node.Name}",
            Lines = [$"JsonNode? {mergedVar} = null;", $"if ({condition})"],
            Branches = new Dictionary<string, IReadOnlyList<EmittedNode>>
            {
                ["true"] = trueBody,
                ["false"] = falseBody,
            },
        };
    }

    private static EmittedNode AssignMerged(string mergedVar, string sourceVar) => new()
    {
        NodeId = mergedVar,
        Kind = EmittedNodeKind.Statement,
        Lines = [$"{mergedVar} = {sourceVar};"],
    };

    private List<EmittedNode> EmitBranch(string? start, string? mergePoint, WorkflowGraph graph, CodeGenContext ctx)
    {
        if (start is null)
        {
            return [];
        }

        var branchNodeIds = WorkflowGraphAnalysis.BranchSubgraph(graph, start, mergePoint);
        var nodesById = graph.Nodes.ToDictionary(n => n.Id);
        var emitted = new List<EmittedNode>();

        foreach (var nodeId in branchNodeIds)
        {
            if (!ctx.ProcessedNodeIds.Add(nodeId))
            {
                continue;
            }

            emitted.Add(registry.Emit(nodesById[nodeId], graph, ctx));
        }

        return emitted;
    }

    private static string TranslateCondition(WorkflowNode node, CodeGenContext ctx)
    {
        if (node.Parameters.TryGetValue("condition", out var raw) &&
            raw is MapParameter condition &&
            condition.Entries.TryGetValue("value1", out var v1) &&
            condition.Entries.TryGetValue("value2", out var v2))
        {
            var operation = condition.Entries.GetValueOrDefault("operation")?.AsLiteralString();
            var left = ExpressionTranslator.Translate(v1, ctx, node.Id);
            var right = ExpressionTranslator.Translate(v2, ctx, node.Id);

            switch (operation)
            {
                case "equal":
                    return $"Equals({left}?.ToString(), {right}?.ToString())";
                case "notEqual":
                    return $"!Equals({left}?.ToString(), {right}?.ToString())";
                case "larger" or "greaterThan":
                    return $"string.CompareOrdinal({left}?.ToString(), {right}?.ToString()) > 0";
                case "smaller" or "lessThan":
                    return $"string.CompareOrdinal({left}?.ToString(), {right}?.ToString()) < 0";
            }
        }

        ctx.Warn("If condition not translated in v1 (falls back to a TODO placeholder).", node.Id);
        return "true /* TODO(WorkflowForge): translate If condition */";
    }
}
