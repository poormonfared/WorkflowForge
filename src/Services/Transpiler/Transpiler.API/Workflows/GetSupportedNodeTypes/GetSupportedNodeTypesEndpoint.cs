using Carter;
using MediatR;

namespace Transpiler.API.Workflows.GetSupportedNodeTypes;

public class GetSupportedNodeTypesEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/workflows/supported-node-types", async (ISender sender) =>
            {
                var types = await sender.Send(new GetSupportedNodeTypesQuery());
                return Results.Ok(types);
            })
            .WithName("GetSupportedNodeTypes")
            .Produces<IReadOnlyList<string>>(StatusCodes.Status200OK)
            .WithSummary("List the n8n node types WorkflowForge can translate")
            .WithDescription("Anything not on this list still parses — it lands as a TODO pass-through stub instead of failing.");
    }
}
