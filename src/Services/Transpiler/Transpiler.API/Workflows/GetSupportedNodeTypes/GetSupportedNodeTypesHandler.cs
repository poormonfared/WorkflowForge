using BuildingBlocks.CQRS;
using Transpiler.Adapters.N8n.NodeHandlers;

namespace Transpiler.API.Workflows.GetSupportedNodeTypes;

public sealed record GetSupportedNodeTypesQuery : IQuery<IReadOnlyList<string>>;

internal sealed class GetSupportedNodeTypesQueryHandler : IQueryHandler<GetSupportedNodeTypesQuery, IReadOnlyList<string>>
{
    public Task<IReadOnlyList<string>> Handle(GetSupportedNodeTypesQuery query, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(NodeHandlerRegistry.SupportedTypes.OrderBy(t => t, StringComparer.Ordinal).ToList());
}
