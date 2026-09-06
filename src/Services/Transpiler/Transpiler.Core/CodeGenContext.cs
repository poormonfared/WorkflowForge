using Transpiler.Core.Model;

namespace Transpiler.Core;

/// <summary>
/// Mutable state threaded through every node emitter during a single generation run.
/// One instance per <see cref="IWorkflowCodeGenerator.Generate"/> call.
/// </summary>
public sealed class CodeGenContext
{
    public required WorkflowGraph Graph { get; init; }

    /// <summary>Node ids in dependency-first order, main-flow connections only.</summary>
    public required IReadOnlyList<string> TopologicalOrder { get; init; }

    public NameRegistry Names { get; } = new();

    public HashSet<string> UsingDirectives { get; } = new();

    public HashSet<string> NuGetPackages { get; } = new();

    public List<string> Warnings { get; } = new();

    /// <summary>
    /// Node ids already emitted by a branch/loop emitter's own recursion, so the top-level
    /// dispatch loop skips them instead of emitting them again.
    /// </summary>
    public HashSet<string> ProcessedNodeIds { get; } = new();

    public void Warn(string message, string? nodeId = null) =>
        Warnings.Add(nodeId is null ? message : $"[{nodeId}] {message}");
}
