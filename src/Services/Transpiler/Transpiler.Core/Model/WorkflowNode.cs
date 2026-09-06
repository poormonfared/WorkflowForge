namespace Transpiler.Core.Model;

public enum NodeKind
{
    Trigger,
    Action,
    Condition,
    Switch,
    Merge,
    Loop,
    Unsupported,
}

/// <summary>
/// One node from the source workflow, translated into the platform-agnostic shape.
/// </summary>
/// <param name="SourceTypeIdentifier">
/// The original source-format type string (e.g. n8n's "n8n-nodes-base.httpRequest").
/// Kept for diagnostics and for naming TODO stubs when <see cref="Kind"/> is <see cref="NodeKind.Unsupported"/>.
/// </param>
public sealed record WorkflowNode(
    string Id,
    string Name,
    NodeKind Kind,
    string SourceTypeIdentifier,
    IReadOnlyDictionary<string, ParameterValue> Parameters,
    bool Disabled);
