using System.Text;
using Transpiler.Core;
using Transpiler.Core.Model;
using Transpiler.CodeGen.CSharp.Emission;

namespace Transpiler.CodeGen.CSharp.ProjectShapes;

/// <summary>Manual/no-trigger workflows: a plain console app that runs once and exits.</summary>
internal static class ConsoleProjectShape
{
    public static IReadOnlyList<GeneratedFile> Build(WorkflowGraph graph, IReadOnlyList<EmittedNode> topLevel, CodeGenContext ctx)
    {
        var name = ProjectShapeHelpers.ProjectName(graph);

        var program = new StringBuilder()
            .Append(ProjectShapeHelpers.UsingsBlock(ctx))
            .AppendLine()
            .AppendLine($"// Auto-generated from n8n workflow: {graph.Name}")
            .AppendLine("// WorkflowForge — no n8n runtime dependency required to run this.")
            .AppendLine()
            .Append(CSharpEmitter.EmitBlock(topLevel, indentLevel: 0))
            .AppendLine($"Console.WriteLine({ctx.Names.CurrentVariable}.ToJsonString());")
            .ToString();

        var csproj = $"""
            <Project Sdk="Microsoft.NET.Sdk">

              <PropertyGroup>
                <OutputType>Exe</OutputType>
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
            new GeneratedFile("README.md", ProjectShapeHelpers.Readme(graph, ctx, "Runs once from `Main` and exits (manual/no trigger).")),
        ];
    }
}
