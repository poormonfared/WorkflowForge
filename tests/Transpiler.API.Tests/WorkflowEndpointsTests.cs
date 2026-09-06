using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Transpiler.API.Tests;

/// <summary>
/// Boots the real DI container (Carter + MediatR + FluentValidation + BuildingBlocks pipeline
/// behaviors + exception handler) via <see cref="WebApplicationFactory{TEntryPoint}"/> — catches
/// wiring mistakes (missing registrations, handler discovery, etc.) that library unit tests can't.
/// </summary>
public class WorkflowEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string SampleWorkflowJson = """
        {
          "name": "Greet Webhook",
          "nodes": [
            { "id": "1", "name": "Webhook", "type": "n8n-nodes-base.webhook", "typeVersion": 1,
              "parameters": { "httpMethod": "POST", "path": "greet" } }
          ],
          "connections": {}
        }
        """;

    [Fact]
    public async Task GetSupportedNodeTypes_ReturnsTheRegisteredN8nTypes()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/workflows/supported-node-types");

        response.EnsureSuccessStatusCode();
        var types = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(types);
        Assert.Contains("n8n-nodes-base.webhook", types);
        Assert.Contains("n8n-nodes-base.if", types);
    }

    [Fact]
    public async Task UploadThenGenerateThenDownload_RoundTripsAWorkflowIntoAZip()
    {
        var client = factory.CreateClient();

        var uploadResponse = await client.PostAsync("/workflows", new StringContent(SampleWorkflowJson));
        uploadResponse.EnsureSuccessStatusCode();
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<UploadResponseDto>();
        Assert.NotNull(uploaded);

        var generateResponse = await client.PostAsync($"/workflows/{uploaded.WorkflowId}/generate", content: null);
        generateResponse.EnsureSuccessStatusCode();
        var generated = await generateResponse.Content.ReadFromJsonAsync<GenerateResponseDto>();
        Assert.NotNull(generated);
        Assert.Contains("Program.cs", generated.FilePaths);

        var downloadResponse = await client.GetAsync($"/workflows/{uploaded.WorkflowId}/download");
        downloadResponse.EnsureSuccessStatusCode();
        Assert.Equal("application/zip", downloadResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GenerateProject_UnknownWorkflowId_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsync($"/workflows/{Guid.NewGuid():n}/generate", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private sealed record UploadResponseDto(string WorkflowId, string WorkflowName, List<string> Diagnostics);

    private sealed record GenerateResponseDto(string WorkflowId, List<string> FilePaths, List<string> Warnings);
}
