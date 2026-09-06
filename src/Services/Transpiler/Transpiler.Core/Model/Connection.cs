namespace Transpiler.Core.Model;

public enum ConnectionType
{
    /// <summary>Standard execution-flow edge.</summary>
    Main,

    /// <summary>
    /// A non-execution edge (e.g. n8n's ai_tool/ai_memory/ai_languageModel connections).
    /// Collapsed into one kind here; <see cref="Connection.AuxiliaryTag"/> keeps the original label.
    /// </summary>
    Auxiliary,
}

/// <param name="SourceOutputIndex">Branch index on the source node's output — e.g. If-true=0/false=1, Switch case N.</param>
public sealed record Connection(
    string SourceNodeId,
    string TargetNodeId,
    ConnectionType Type,
    string? AuxiliaryTag,
    int SourceOutputIndex);
