using System.Text.Json;
using Transpiler.Adapters.N8n.Json;
using Transpiler.Adapters.N8n.NodeHandlers;
using Transpiler.Core;
using Transpiler.Core.Model;

namespace Transpiler.Adapters.N8n;

/// <summary>
/// n8n JSON export -> <see cref="WorkflowGraph"/>. The only project that knows n8n's JSON shape;
/// implements the <see cref="IWorkflowSourceParser"/> boundary so <c>Transpiler.CodeGen.CSharp</c>
/// never needs to.
/// </summary>
public sealed class N8nWorkflowParser : IWorkflowSourceParser
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly string[] WebhookTypes = ["n8n-nodes-base.webhook"];
    private static readonly string[] ScheduleTypes = ["n8n-nodes-base.scheduletrigger"];

    private readonly List<ParseDiagnostic> _diagnostics = new();

    public string SourceFormatId => "n8n";

    public IReadOnlyList<ParseDiagnostic> Diagnostics => _diagnostics;

    public WorkflowGraph Parse(string rawSource, ParseOptions options)
    {
        _diagnostics.Clear();

        N8nWorkflowDto dto;
        try
        {
            dto = JsonSerializer.Deserialize<N8nWorkflowDto>(rawSource, JsonOptions)
                  ?? throw new FormatException("Workflow JSON deserialized to null.");
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid n8n workflow JSON: {ex.Message}", ex);
        }

        var nodes = dto.Nodes.Select(MapNode).ToList();
        var nodeNames = nodes.Select(n => n.Id).ToHashSet();
        var connections = BuildConnections(dto, nodeNames);
        var metadata = new WorkflowMetadata(DetectPrimaryTrigger(nodes), new Dictionary<string, string>());

        return new WorkflowGraph(dto.Name, nodes, connections, metadata);
    }

    private WorkflowNode MapNode(N8nNodeDto dto)
    {
        var handler = NodeHandlerRegistry.Find(dto.Type);
        if (handler is not null)
        {
            return handler.Map(dto);
        }

        _diagnostics.Add(new ParseDiagnostic(
            DiagnosticSeverity.Warning,
            $"Unsupported n8n node type '{dto.Type}' — generated as a TODO pass-through stub.",
            dto.Name));

        return new WorkflowNode(
            Id: dto.Name.Trim(),
            Name: dto.Name,
            Kind: NodeKind.Unsupported,
            SourceTypeIdentifier: dto.Type,
            Parameters: ParameterValueReader.ReadObject(dto.Parameters),
            Disabled: dto.Disabled ?? false);
    }

    private List<Connection> BuildConnections(N8nWorkflowDto dto, HashSet<string> nodeNames)
    {
        var connections = new List<Connection>();

        foreach (var (sourceName, connectionsByType) in dto.Connections)
        {
            if (!nodeNames.Contains(sourceName))
            {
                _diagnostics.Add(new ParseDiagnostic(DiagnosticSeverity.Warning, $"Connections reference unknown source node '{sourceName}' — skipped."));
                continue;
            }

            foreach (var (connectionTypeKey, outputs) in connectionsByType)
            {
                var isMain = connectionTypeKey.Equals("main", StringComparison.OrdinalIgnoreCase);

                for (var branchIndex = 0; branchIndex < outputs.Count; branchIndex++)
                {
                    foreach (var target in outputs[branchIndex])
                    {
                        if (!nodeNames.Contains(target.Node))
                        {
                            _diagnostics.Add(new ParseDiagnostic(DiagnosticSeverity.Warning, $"Connection from '{sourceName}' targets unknown node '{target.Node}' — skipped."));
                            continue;
                        }

                        connections.Add(new Connection(
                            SourceNodeId: sourceName,
                            TargetNodeId: target.Node,
                            Type: isMain ? ConnectionType.Main : ConnectionType.Auxiliary,
                            AuxiliaryTag: isMain ? null : connectionTypeKey,
                            SourceOutputIndex: branchIndex));
                    }
                }
            }
        }

        return connections;
    }

    private static TriggerKind DetectPrimaryTrigger(IReadOnlyList<WorkflowNode> nodes)
    {
        var triggers = nodes.Where(n => n.Kind == NodeKind.Trigger).ToList();
        if (triggers.Count == 0)
        {
            return TriggerKind.None;
        }

        if (triggers.Any(n => WebhookTypes.Contains(n.SourceTypeIdentifier.ToLowerInvariant())))
        {
            return TriggerKind.Webhook;
        }

        if (triggers.Any(n => ScheduleTypes.Contains(n.SourceTypeIdentifier.ToLowerInvariant())))
        {
            return TriggerKind.Schedule;
        }

        return TriggerKind.Manual;
    }
}
