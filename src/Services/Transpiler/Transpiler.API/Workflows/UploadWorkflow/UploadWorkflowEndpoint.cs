using Carter;
using MediatR;

namespace Transpiler.API.Workflows.UploadWorkflow;

public class UploadWorkflowEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/workflows", async (HttpRequest request, ISender sender) =>
            {
                using var reader = new StreamReader(request.Body);
                var rawJson = await reader.ReadToEndAsync();

                var result = await sender.Send(new UploadWorkflowCommand(rawJson));

                return Results.Created($"/workflows/{result.WorkflowId}", result);
            })
            .WithName("UploadWorkflow")
            .Produces<UploadWorkflowResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .WithSummary("Upload and parse an n8n workflow export")
            .WithDescription("Parses the uploaded n8n JSON into a WorkflowGraph and returns a workflow id for the Generate/Download calls that follow.");
    }
}
