using Transpiler.Adapters.N8n;
using Transpiler.Core;
using Transpiler.Core.Model;

namespace Transpiler.Adapters.N8n.Tests;

public class N8nWorkflowParserTests
{
    private const string WebhookWorkflowJson = """
        {
          "name": "Greet Webhook",
          "nodes": [
            { "id": "1", "name": "Webhook", "type": "n8n-nodes-base.webhook", "typeVersion": 1,
              "parameters": { "httpMethod": "POST", "path": "greet" } },
            { "id": "2", "name": "Set Name", "type": "n8n-nodes-base.set", "typeVersion": 3,
              "parameters": { "values": { "name": "Alice" } } },
            { "id": "3", "name": "Check Name", "type": "n8n-nodes-base.if", "typeVersion": 1,
              "parameters": { "condition": { "value1": "={{ $json.name }}", "operation": "equal", "value2": "Alice" } } },
            { "id": "4", "name": "Notify", "type": "n8n-nodes-base.slack", "typeVersion": 1,
              "parameters": { "message": "hi" } }
          ],
          "connections": {
            "Webhook": { "main": [[{ "node": "Set Name", "type": "main", "index": 0 }]] },
            "Set Name": { "main": [[{ "node": "Check Name", "type": "main", "index": 0 }]] },
            "Check Name": {
              "main": [
                [{ "node": "Notify", "type": "main", "index": 0 }],
                []
              ]
            }
          }
        }
        """;

    [Fact]
    public void Parse_MapsKnownNodeTypesToTheirCoreKind()
    {
        var parser = new N8nWorkflowParser();

        var graph = parser.Parse(WebhookWorkflowJson, new ParseOptions());

        Assert.Equal("Greet Webhook", graph.Name);
        Assert.Equal(NodeKind.Trigger, graph.Nodes.Single(n => n.Name == "Webhook").Kind);
        Assert.Equal(NodeKind.Action, graph.Nodes.Single(n => n.Name == "Set Name").Kind);
        Assert.Equal(NodeKind.Condition, graph.Nodes.Single(n => n.Name == "Check Name").Kind);
    }

    [Fact]
    public void Parse_MapsUnknownNodeTypesToUnsupportedWithoutThrowing()
    {
        var parser = new N8nWorkflowParser();

        var graph = parser.Parse(WebhookWorkflowJson, new ParseOptions());

        var notify = graph.Nodes.Single(n => n.Name == "Notify");
        Assert.Equal(NodeKind.Unsupported, notify.Kind);
        Assert.Equal("n8n-nodes-base.slack", notify.SourceTypeIdentifier);
        Assert.Contains(parser.Diagnostics, d => d.Message.Contains("slack"));
    }

    [Fact]
    public void Parse_DetectsWebhookAsThePrimaryTrigger()
    {
        var parser = new N8nWorkflowParser();

        var graph = parser.Parse(WebhookWorkflowJson, new ParseOptions());

        Assert.Equal(TriggerKind.Webhook, graph.Metadata.PrimaryTrigger);
    }

    [Fact]
    public void Parse_BuildsMainConnectionsWithBranchIndices()
    {
        var parser = new N8nWorkflowParser();

        var graph = parser.Parse(WebhookWorkflowJson, new ParseOptions());

        var fromIf = graph.Connections.Where(c => c.SourceNodeId == "Check Name").ToList();
        Assert.Single(fromIf);
        Assert.Equal("Notify", fromIf[0].TargetNodeId);
        Assert.Equal(0, fromIf[0].SourceOutputIndex);
        Assert.Equal(ConnectionType.Main, fromIf[0].Type);
    }
}
