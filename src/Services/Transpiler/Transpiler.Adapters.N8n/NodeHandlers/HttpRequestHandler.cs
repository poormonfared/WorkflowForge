using Transpiler.Adapters.N8n.Json;
using Transpiler.Core.Model;

namespace Transpiler.Adapters.N8n.NodeHandlers;

public sealed class HttpRequestHandler : IN8nNodeHandler
{
    public IReadOnlyList<string> SupportedTypes { get; } = ["n8n-nodes-base.httpRequest", "n8n-nodes-base.httpRequestV4"];

    public WorkflowNode Map(N8nNodeDto dto) => new(
        Id: dto.Name.Trim(),
        Name: dto.Name,
        Kind: NodeKind.Action,
        SourceTypeIdentifier: dto.Type,
        Parameters: ParameterValueReader.ReadObject(dto.Parameters),
        Disabled: dto.Disabled ?? false);
}
