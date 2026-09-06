using Carter;
using MediatR;

namespace Transpiler.API.Workflows.GenerateProject;

public class GenerateProjectEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/workflows/{workflowId}/generate", async (string workflowId, ISender sender) =>
            {
                var result = await sender.Send(new GenerateProjectCommand(workflowId));
                return Results.Ok(result);
            })
            .WithName("GenerateProject")
            .Produces<GenerateProjectResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Generate the C# project for a previously uploaded workflow")
            .WithDescription("Runs the code generator against the parsed graph and stages the result for download.");
    }
}
