using System.Text;
using Transpiler.Core;
using Transpiler.Core.Model;
using Transpiler.CodeGen.CSharp.Emission;

namespace Transpiler.CodeGen.CSharp.ProjectShapes;

/// <summary>Schedule/cron-triggered workflows: a .NET Worker Service (<see cref="Microsoft.Extensions.Hosting.BackgroundService"/>).</summary>
internal static class WorkerProjectShape
{
    public static IReadOnlyList<GeneratedFile> Build(WorkflowGraph graph, IReadOnlyList<EmittedNode> topLevel, CodeGenContext ctx)
    {
        var name = ProjectShapeHelpers.ProjectName(graph);
        var trigger = graph.Nodes.FirstOrDefault(n =>
            n.Kind == NodeKind.Trigger && n.SourceTypeIdentifier.Equals("n8n-nodes-base.scheduleTrigger", StringComparison.OrdinalIgnoreCase));

        var interval = ResolveInterval(trigger, ctx);

        var program = $"""
            var builder = Host.CreateApplicationBuilder(args);
            builder.Services.AddHttpClient();
            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();

            """;

        var worker = new StringBuilder()
            .Append(ProjectShapeHelpers.UsingsBlock(ctx))
            .AppendLine()
            .AppendLine($"// Auto-generated from n8n workflow: {graph.Name}")
            .AppendLine("// WorkflowForge — no n8n runtime dependency required to run this.")
            .AppendLine()
            .AppendLine("public sealed class Worker(IHttpClientFactory httpClientFactory, ILogger<Worker> logger) : BackgroundService")
            .AppendLine("{")
            .AppendLine($"    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes({interval.TotalMinutes}));")
            .AppendLine()
            .AppendLine("    protected override async Task ExecuteAsync(CancellationToken stoppingToken)")
            .AppendLine("    {")
            .AppendLine("        var httpClient = httpClientFactory.CreateClient();")
            .AppendLine("        while (await _timer.WaitForNextTickAsync(stoppingToken))")
            .AppendLine("        {")
            .AppendLine("            try")
            .AppendLine("            {")
            .Append(Indent(CSharpEmitter.EmitBlock(topLevel, indentLevel: 0), "                "))
            .AppendLine($"                logger.LogInformation(\"Workflow tick completed: {{Result}}\", {ctx.Names.CurrentVariable}.ToJsonString());")
            .AppendLine("            }")
            .AppendLine("            catch (Exception ex)")
            .AppendLine("            {")
            .AppendLine("                logger.LogError(ex, \"Workflow tick failed\");")
            .AppendLine("            }")
            .AppendLine("        }")
            .AppendLine("    }")
            .AppendLine("}")
            .ToString();

        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk.Worker">

              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RootNamespace>{name}</RootNamespace>
              </PropertyGroup>

              <ItemGroup>
                <PackageReference Include="Microsoft.Extensions.Hosting" Version="10.0.0" />
              </ItemGroup>

            </Project>

            """;

        return
        [
            new GeneratedFile("Program.cs", program),
            new GeneratedFile("Worker.cs", worker),
            new GeneratedFile($"{name}.csproj", csproj),
            new GeneratedFile("README.md", ProjectShapeHelpers.Readme(graph, ctx, $"Worker Service — ticks every {interval}.")),
        ];
    }

    private static TimeSpan ResolveInterval(WorkflowNode? trigger, CodeGenContext ctx)
    {
        if (trigger?.Parameters.GetValueOrDefault("rule") is MapParameter rule &&
            rule.Entries.GetValueOrDefault("interval") is CollectionParameter { Items: [MapParameter firstInterval, ..] })
        {
            var field = firstInterval.Entries.GetValueOrDefault("field")?.AsLiteralString();
            var minutesInterval = firstInterval.Entries.GetValueOrDefault("minutesInterval")?.AsNumber();
            var hoursInterval = firstInterval.Entries.GetValueOrDefault("hoursInterval")?.AsNumber();

            switch (field)
            {
                case "minutes" when minutesInterval is > 0:
                    return TimeSpan.FromMinutes(minutesInterval.Value);
                case "hours" or null when hoursInterval is > 0:
                    return TimeSpan.FromHours(hoursInterval.Value);
            }
        }

        if (trigger is not null)
        {
            ctx.Warn("Schedule interval not recognized in v1 — defaulting to hourly.", trigger.Id);
        }

        return TimeSpan.FromHours(1);
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
