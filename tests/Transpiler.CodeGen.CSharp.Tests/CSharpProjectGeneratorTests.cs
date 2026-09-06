using Transpiler.Adapters.N8n;
using Transpiler.CodeGen.CSharp;
using Transpiler.Core;

namespace Transpiler.CodeGen.CSharp.Tests;

public class CSharpProjectGeneratorTests
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

    private const string ManualWorkflowJson = """
        {
          "name": "Manual Flow",
          "nodes": [
            { "id": "1", "name": "Start", "type": "n8n-nodes-base.manualTrigger", "typeVersion": 1, "parameters": {} },
            { "id": "2", "name": "Set Name", "type": "n8n-nodes-base.set", "typeVersion": 3,
              "parameters": { "values": { "name": "Alice" } } }
          ],
          "connections": {
            "Start": { "main": [[{ "node": "Set Name", "type": "main", "index": 0 }]] }
          }
        }
        """;

    private static (Transpiler.Core.Model.WorkflowGraph Graph, GeneratedProject Project) GenerateFrom(string json)
    {
        var parser = new N8nWorkflowParser();
        var graph = parser.Parse(json, new ParseOptions());
        var project = new CSharpProjectGenerator().Generate(graph, new CodeGenOptions());
        return (graph, project);
    }

    [Fact]
    public void Generate_WebhookTrigger_ProducesAMinimalApiProject()
    {
        var (_, project) = GenerateFrom(WebhookWorkflowJson);

        var csproj = project.Files.Single(f => f.RelativePath.EndsWith(".csproj"));
        Assert.Contains("Microsoft.NET.Sdk.Web", csproj.Contents);

        var program = project.Files.Single(f => f.RelativePath == "Program.cs");
        Assert.Contains("app.MapPost(\"/greet\"", program.Contents);
    }

    [Fact]
    public void Generate_ManualTrigger_ProducesAConsoleProject()
    {
        var (_, project) = GenerateFrom(ManualWorkflowJson);

        var csproj = project.Files.Single(f => f.RelativePath.EndsWith(".csproj"));
        Assert.Contains("<OutputType>Exe</OutputType>", csproj.Contents);
        Assert.DoesNotContain("Microsoft.NET.Sdk.Web", csproj.Contents);
    }

    [Fact]
    public void Generate_UnsupportedNode_EmitsPassThroughStubAndWarningInsteadOfThrowing()
    {
        var (_, project) = GenerateFrom(WebhookWorkflowJson);

        var program = project.Files.Single(f => f.RelativePath == "Program.cs");
        Assert.Contains("TODO(WorkflowForge): implement 'Notify'", program.Contents);
        Assert.Contains("pass-through stub", program.Contents);
        Assert.Contains(project.Warnings, w => w.Contains("n8n-nodes-base.slack"));
    }

    [Fact]
    public void Generate_IfNode_EmitsRealIfElseBlockWiredToBothBranches()
    {
        var (_, project) = GenerateFrom(WebhookWorkflowJson);

        var program = project.Files.Single(f => f.RelativePath == "Program.cs");
        Assert.Contains("if (Equals(", program.Contents);
        Assert.Contains("else", program.Contents);
    }

    [Fact]
    public void Generate_AlwaysIncludesAReadmeListingWarnings()
    {
        var (_, project) = GenerateFrom(WebhookWorkflowJson);

        var readme = project.Files.Single(f => f.RelativePath == "README.md");
        Assert.Contains("Follow-up needed", readme.Contents);
        Assert.Contains("slack", readme.Contents);
    }
}
