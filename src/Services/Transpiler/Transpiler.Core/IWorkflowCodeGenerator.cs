using Transpiler.Core.Model;

namespace Transpiler.Core;

public sealed record CodeGenOptions;

public sealed record GeneratedFile(string RelativePath, string Contents);

public sealed record GeneratedProject(IReadOnlyList<GeneratedFile> Files, IReadOnlyList<string> Warnings);

/// <summary>
/// Adapter-pattern boundary: turns a source-agnostic <see cref="WorkflowGraph"/> into a runnable
/// generated project. Never references any <c>IWorkflowSourceParser</c> implementation.
/// </summary>
public interface IWorkflowCodeGenerator
{
    GeneratedProject Generate(WorkflowGraph graph, CodeGenOptions options);
}
