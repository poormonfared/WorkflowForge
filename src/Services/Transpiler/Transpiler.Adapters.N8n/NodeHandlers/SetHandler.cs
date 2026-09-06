using Transpiler.Adapters.N8n.Json;
using Transpiler.Core.Model;

namespace Transpiler.Adapters.N8n.NodeHandlers;

/// <summary>n8n's "Edit Fields" / Set node — assigns literal or expression values onto the item.</summary>
public sealed class SetHandler : IN8nNodeHandler
{
    public IReadOnlyList<string> SupportedTypes { get; } = ["n8n-nodes-base.set"];

    public WorkflowNode Map(N8nNodeDto dto) => new(
        Id: dto.Name.Trim(),
        Name: dto.Name,
        Kind: NodeKind.Action,
        SourceTypeIdentifier: dto.Type,
        Parameters: ParameterValueReader.ReadObject(dto.Parameters),
        Disabled: dto.Disabled ?? false);
}
