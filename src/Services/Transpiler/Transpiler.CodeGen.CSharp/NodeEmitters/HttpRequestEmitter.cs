using Transpiler.Core;
using Transpiler.Core.Model;
using Transpiler.CodeGen.CSharp.Emission;

namespace Transpiler.CodeGen.CSharp.NodeEmitters;

/// <summary>
/// n8n's HTTP Request node. Generates a call against an injected <see cref="HttpClient"/> (via
/// <c>IHttpClientFactory</c>, registered in the generated <c>Program.cs</c>) — no custom HTTP
/// abstraction, per the plan.
/// </summary>
public sealed class HttpRequestEmitter : INodeEmitter
{
    public IReadOnlyList<string> SupportedSourceTypes { get; } = ["n8n-nodes-base.httprequest", "n8n-nodes-base.httprequestv4"];

    public EmittedNode Emit(WorkflowNode node, WorkflowGraph graph, CodeGenContext ctx)
    {
        var varName = ctx.Names.Register(node.Id, node.Name);
        ctx.Names.AdvanceCurrent(varName);
        ctx.UsingDirectives.Add("System.Text.Json.Nodes");
        ctx.UsingDirectives.Add("System.Net.Http.Json");

        var method = node.Parameters.GetValueOrDefault("method")?.AsLiteralString()?.ToUpperInvariant() ?? "GET";
        var urlValue = node.Parameters.TryGetValue("url", out var url) ? ExpressionTranslator.Translate(url, ctx, node.Id) : "\"\"";

        var lines = new List<string>
        {
            $"using var {varName}Request = new HttpRequestMessage(HttpMethod.{ToHttpMethodEnum(method)}, {urlValue}?.ToString());",
            $"using var {varName}Response = await httpClient.SendAsync({varName}Request);",
            $"{varName}Response.EnsureSuccessStatusCode();",
            $"var {varName} = await {varName}Response.Content.ReadFromJsonAsync<JsonNode>() ?? new JsonObject();",
        };

        return new EmittedNode
        {
            NodeId = node.Id,
            Kind = EmittedNodeKind.Statement,
            Comment = $"HTTP Request: {method} {node.Name}",
            Lines = lines,
        };
    }

    private static string ToHttpMethodEnum(string method) => method switch
    {
        "GET" => "Get",
        "POST" => "Post",
        "PUT" => "Put",
        "PATCH" => "Patch",
        "DELETE" => "Delete",
        "HEAD" => "Head",
        "OPTIONS" => "Options",
        _ => "Get",
    };
}
