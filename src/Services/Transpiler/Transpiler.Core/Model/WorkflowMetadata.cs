namespace Transpiler.Core.Model;

public enum TriggerKind
{
    None,
    Manual,
    Webhook,
    Schedule,
}

/// <param name="SourceProperties">Adapter-specific passthrough data (original workflow id, etc.) — never read by codegen.</param>
public sealed record WorkflowMetadata(
    TriggerKind PrimaryTrigger,
    IReadOnlyDictionary<string, string> SourceProperties);
