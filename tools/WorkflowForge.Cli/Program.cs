using Transpiler.Adapters.N8n;
using Transpiler.CodeGen.CSharp;
using Transpiler.Core;

// Thin, in-process composition root over the same Transpiler.Core/Adapters.N8n/CodeGen.CSharp
// libraries Transpiler.API wires up over HTTP — no network hop, useful for scripted/CI use.
// Usage: workflowforge <input-workflow.json> <output-directory>

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: workflowforge <input-workflow.json> <output-directory>");
    return 1;
}

var (inputPath, outputDirectory) = (args[0], args[1]);

if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"Input file not found: {inputPath}");
    return 1;
}

var rawJson = await File.ReadAllTextAsync(inputPath);

IWorkflowSourceParser parser = new N8nWorkflowParser();
IWorkflowCodeGenerator generator = new CSharpProjectGenerator();

var graph = parser.Parse(rawJson, new ParseOptions());
foreach (var diagnostic in parser.Diagnostics)
{
    Console.WriteLine($"[{diagnostic.Severity}] {diagnostic.Message}");
}

var project = generator.Generate(graph, new CodeGenOptions());

Directory.CreateDirectory(outputDirectory);
foreach (var file in project.Files)
{
    var path = Path.Combine(outputDirectory, file.RelativePath);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    await File.WriteAllTextAsync(path, file.Contents);
}

Console.WriteLine($"Generated {project.Files.Count} file(s) to {outputDirectory}");
if (project.Warnings.Count > 0)
{
    Console.WriteLine($"{project.Warnings.Count} node(s) generated as TODO stubs:");
    foreach (var warning in project.Warnings)
    {
        Console.WriteLine($"  - {warning}");
    }
}

return 0;
