using System.Text;
using System.Text.RegularExpressions;
using Transpiler.Core;
using Transpiler.Core.Model;
using Transpiler.CodeGen.CSharp.Emission;

namespace Transpiler.CodeGen.CSharp.ProjectShapes;

/// <summary>Webhook-triggered workflows: an ASP.NET Core Minimal API with one route.</summary>
internal static partial class MinimalApiProjectShape
{
    [GeneratedRegex("[^a-zA-Z0-9/_-]+")]
    private static partial Regex UnsafePathChars();

    public static IReadOnlyList<GeneratedFile> Build(WorkflowGraph graph, IReadOnlyList<EmittedNode> topLevel, CodeGenContext ctx)
    {
        var name = ProjectShapeHelpers.ProjectName(graph);
        var trigger = graph.Nodes.FirstOrDefault(n =>
            n.Kind == NodeKind.Trigger && n.SourceTypeIdentifier.Equals("n8n-nodes-base.webhook", StringComparison.OrdinalIgnoreCase));

        var httpMethod = trigger?.Parameters.GetValueOrDefault("httpMethod")?.AsLiteralString()?.ToUpperInvariant() ?? "POST";
        var rawPath = trigger?.Parameters.GetValueOrDefault("path")?.AsLiteralString();
        var path = SanitizePath(string.IsNullOrWhiteSpace(rawPath) ? trigger?.Name ?? "webhook" : rawPath);

        var mapMethod = httpMethod switch
        {
            "GET" => "MapGet",
            "PUT" => "MapPut",
            "DELETE" => "MapDelete",
            "PATCH" => "MapPatch",
            _ => "MapPost",
        };

        var program = new StringBuilder()
            .Append(ProjectShapeHelpers.UsingsBlock(ctx))
            .AppendLine("using Microsoft.AspNetCore.Http;")
            .AppendLine()
            .AppendLine($"// Auto-generated from n8n workflow: {graph.Name}")
            .AppendLine("// WorkflowForge — no n8n runtime dependency required to run this.")
            .AppendLine()
            .AppendLine("var builder = WebApplication.CreateBuilder(args);")
            .AppendLine("builder.Services.AddHttpClient();")
            .AppendLine("var app = builder.Build();")
            .AppendLine()
            .AppendLine($"app.{mapMethod}(\"/{path}\", async (HttpRequest request, IHttpClientFactory httpClientFactory) =>")
            .AppendLine("{")
            .AppendLine("    var httpClient = httpClientFactory.CreateClient();")
            .Append(Indent(CSharpEmitter.EmitBlock(topLevel, indentLevel: 0), "    "))
            .AppendLine($"    return Results.Ok({ctx.Names.CurrentVariable});")
            .AppendLine("});")
            .AppendLine()
            .AppendLine("app.Run();")
            .ToString();

        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RootNamespace>{name}</RootNamespace>
              </PropertyGroup>

            </Project>

            """;

        return
        [
            new GeneratedFile("Program.cs", program),
            new GeneratedFile($"{name}.csproj", csproj),
            new GeneratedFile("appsettings.json", "{\n  \"Logging\": { \"LogLevel\": { \"Default\": \"Information\" } }\n}\n"),
            new GeneratedFile("README.md", ProjectShapeHelpers.Readme(graph, ctx, $"Minimal API — `{httpMethod} /{path}`.")),
        ];
    }

    private static string SanitizePath(string raw)
    {
        var cleaned = UnsafePathChars().Replace(raw.Trim(), "_").Trim('/');
        return string.IsNullOrEmpty(cleaned) ? "webhook" : cleaned;
    }

    private static string Indent(string block, string prefix)
    {
        var sb = new StringBuilder();
        foreach (var line in block.Split('\n'))
        {
            var trimmed = line.TrimEnd('\r');
            sb.AppendLine(trimmed.Length == 0 ? trimmed : $"{prefix}{trimmed}");
        }

        return sb.ToString();
    }
}
