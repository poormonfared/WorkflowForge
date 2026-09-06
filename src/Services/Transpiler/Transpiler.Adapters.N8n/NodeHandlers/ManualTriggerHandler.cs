using Transpiler.Adapters.N8n.Json;
using Transpiler.Core.Model;

namespace Transpiler.Adapters.N8n.NodeHandlers;

public sealed class ManualTriggerHandler : IN8nNodeHandler
{
    public IReadOnlyList<string> SupportedTypes { get; } = ["n8n-nodes-base.manualTrigger", "n8n-nodes-base.start"];

    public WorkflowNode Map(N8nNodeDto dto) => new(
        Id: dto.Name.Trim(),
        Name: dto.Name,
        Kind: NodeKind.Trigger,
        SourceTypeIdentifier: dto.Type,
        Parameters: ParameterValueReader.ReadObject(dto.Parameters),
        Disabled: dto.Disabled ?? false);
}
