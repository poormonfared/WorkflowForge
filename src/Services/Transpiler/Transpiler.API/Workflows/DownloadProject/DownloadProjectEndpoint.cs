using Carter;
using MediatR;

namespace Transpiler.API.Workflows.DownloadProject;

public class DownloadProjectEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/workflows/{workflowId}/download", async (string workflowId, ISender sender) =>
            {
                var zipBytes = await sender.Send(new DownloadProjectQuery(workflowId));
                return Results.File(zipBytes, "application/zip", $"{workflowId}.zip");
            })
            .WithName("DownloadProject")
            .Produces(StatusCodes.Status200OK, contentType: "application/zip")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Download the generated C# project as a zip")
            .WithDescription("Requires GenerateProject to have run first for this workflow id.");
    }
}
