using Microsoft.Extensions.Caching.Memory;
using Transpiler.Core;
using Transpiler.Core.Model;

namespace Transpiler.API.Infrastructure;

public sealed class InMemoryWorkflowSessionStore(IMemoryCache cache) : IWorkflowSessionStore
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(30);

    public string SaveGraph(WorkflowGraph graph, IReadOnlyList<ParseDiagnostic> diagnostics)
    {
        var workflowId = Guid.NewGuid().ToString("n");
        cache.Set(GraphKey(workflowId), (graph, diagnostics), Ttl);
        return workflowId;
    }

    public (WorkflowGraph Graph, IReadOnlyList<ParseDiagnostic> Diagnostics)? GetGraph(string workflowId) =>
        cache.TryGetValue(GraphKey(workflowId), out (WorkflowGraph, IReadOnlyList<ParseDiagnostic>) entry)
            ? entry
            : null;

    public void SaveGeneratedProject(string workflowId, GeneratedProject project) =>
        cache.Set(ProjectKey(workflowId), project, Ttl);

    public GeneratedProject? GetGeneratedProject(string workflowId) =>
        cache.TryGetValue(ProjectKey(workflowId), out GeneratedProject? project) ? project : null;

    private static string GraphKey(string workflowId) => $"workflow-graph:{workflowId}";

    private static string ProjectKey(string workflowId) => $"generated-project:{workflowId}";
}
