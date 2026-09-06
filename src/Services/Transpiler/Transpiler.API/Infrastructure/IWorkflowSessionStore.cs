using Transpiler.Core;
using Transpiler.Core.Model;

namespace Transpiler.API.Infrastructure;

/// <summary>
/// Holds a parsed graph and, later, its generated project between the Upload/Generate/Download
/// calls of one session. In-memory, short-TTL, single-instance for v1 — see the plan's open
/// question on session state: this doesn't survive a restart or scale past one API instance.
/// </summary>
public interface IWorkflowSessionStore
{
    string SaveGraph(WorkflowGraph graph, IReadOnlyList<ParseDiagnostic> diagnostics);

    (WorkflowGraph Graph, IReadOnlyList<ParseDiagnostic> Diagnostics)? GetGraph(string workflowId);

    void SaveGeneratedProject(string workflowId, GeneratedProject project);

    GeneratedProject? GetGeneratedProject(string workflowId);
}
