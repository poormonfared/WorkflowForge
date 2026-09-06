using Transpiler.Core;
using Transpiler.Core.Model;
using Transpiler.CodeGen.CSharp.Emission;

namespace Transpiler.CodeGen.CSharp.NodeEmitters;

/// <summary>
/// n8n's Set / Edit Fields node. Supports the flat <c>values: { field: value }</c> shape and the
/// older nested <c>values: { string: [{name, value}], ... }</c> shape. The newer v2
/// "assignments.assignments[]" schema is not yet handled in v1 — falls through as a no-op with a
/// warning rather than guessing at its structure.
/// </summary>
public sealed class SetEmitter : INodeEmitter
{
    public IReadOnlyList<string> SupportedSourceTypes { get; } = ["n8n-nodes-base.set"];

    public EmittedNode Emit(WorkflowNode node, WorkflowGraph graph, CodeGenContext ctx)
    {
        var varName = ctx.Names.Register(node.Id, node.Name);
        var previous = ctx.Names.CurrentVariable;
        ctx.Names.AdvanceCurrent(varName);
        ctx.UsingDirectives.Add("System.Text.Json.Nodes");

        var lines = new List<string> { $"var {varName} = ({previous} as JsonNode)?.DeepClone() as JsonObject ?? new JsonObject();" };

        if (node.Parameters.TryGetValue("values", out var valuesRaw) && valuesRaw is MapParameter valuesMap)
        {
            foreach (var (fieldName, fieldValue) in valuesMap.Entries)
            {
                foreach (var (name, value) in FlattenFieldAssignments(fieldName, fieldValue))
                {
                    lines.Add($"{varName}[\"{name}\"] = {ExpressionTranslator.Translate(value, ctx, node.Id)};");
                }
            }
        }

        return new EmittedNode
        {
            NodeId = node.Id,
            Kind = EmittedNodeKind.Statement,
            Comment = $"Set: {node.Name}",
            Lines = lines,
        };
    }

    private static IEnumerable<(string Name, ParameterValue Value)> FlattenFieldAssignments(string key, ParameterValue value)
    {
        switch (value)
        {
            // Nested shape: values.string/number/boolean = [{ name, value }, ...]
            case CollectionParameter collection:
                foreach (var item in collection.Items)
                {
                    if (item is MapParameter { Entries: var entries } &&
                        entries.TryGetValue("name", out var nameValue) &&
                        nameValue.AsLiteralString() is { } name &&
                        entries.TryGetValue("value", out var assignedValue))
                    {
                        yield return (name, assignedValue);
                    }
                }

                break;

            // Flat shape: values.<fieldName> = <literal or expression>
            case LiteralParameter or ExpressionParameter:
                yield return (key, value);
                break;
        }
    }
}
