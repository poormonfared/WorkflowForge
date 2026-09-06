using System.Net.Http.Json;

namespace WorkflowForge.Web.Services;

public sealed record UploadWorkflowResult(string WorkflowId, string WorkflowName, IReadOnlyList<string> Diagnostics);

public sealed record GenerateProjectResult(string WorkflowId, IReadOnlyList<string> FilePaths, IReadOnlyList<string> Warnings);

/// <summary>Typed client calling through <c>WorkflowForge.Gateway</c> — never talks to Transpiler.API directly.</summary>
public sealed class TranspilerClient(HttpClient httpClient)
{
    public async Task<UploadWorkflowResult> UploadAsync(Stream workflowJson, CancellationToken cancellationToken = default)
    {
        using var content = new StreamContent(workflowJson);
        var response = await httpClient.PostAsync("/transpiler-service/workflows", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UploadWorkflowResult>(cancellationToken))!;
    }

    public async Task<GenerateProjectResult> GenerateAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsync($"/transpiler-service/workflows/{workflowId}/generate", content: null, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GenerateProjectResult>(cancellationToken))!;
    }

    public async Task<byte[]> DownloadAsync(string workflowId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/transpiler-service/workflows/{workflowId}/download", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }
}
