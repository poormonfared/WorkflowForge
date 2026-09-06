using Transpiler.Adapters.N8n.Json;
using Transpiler.Core.Model;

namespace Transpiler.Adapters.N8n.NodeHandlers;

/// <summary>n8n's If node — two-branch conditional. Branch wiring itself lives in the connection graph.</summary>
public sealed class IfHandler : IN8nNodeHandler
{
    public IReadOnlyList<string> SupportedTypes { get; } = ["n8n-nodes-base.if"];

    public WorkflowNode Map(N8nNodeDto dto) => new(
        Id: dto.Name.Trim(),
        Name: dto.Name,
        Kind: NodeKind.Condition,
        SourceTypeIdentifier: dto.Type,
        Parameters: ParameterValueReader.ReadObject(dto.Parameters),
        Disabled: dto.Disabled ?? false);
}
