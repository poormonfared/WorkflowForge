namespace Transpiler.CodeGen.CSharp.Emission;

public enum EmittedNodeKind
{
    /// <summary>Plain sequential C# statement(s).</summary>
    Statement,

    /// <summary>An if/else block. <see cref="EmittedNode.Lines"/> holds just the "if (...)" line.</summary>
    IfBranch,
}

/// <summary>
/// One unit of generated C# derived from one <c>WorkflowNode</c> — the .NET analogue of the
/// reference Python transpiler's IRNode. The <see cref="CSharpEmitter"/> is the only place that
/// turns these into source text.
/// </summary>
public sealed class EmittedNode
{
    public required string NodeId { get; init; }
    public required EmittedNodeKind Kind { get; init; }
    public IReadOnlyList<string> Lines { get; init; } = [];

    /// <summary>For <see cref="EmittedNodeKind.IfBranch"/>: "true" / "false" -> the branch body.</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<EmittedNode>> Branches { get; init; } =
        new Dictionary<string, IReadOnlyList<EmittedNode>>();

    public string? Comment { get; init; }
}
