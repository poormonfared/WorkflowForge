namespace Transpiler.Core.Model;

public sealed record WorkflowGraph(
    string Name,
    IReadOnlyList<WorkflowNode> Nodes,
    IReadOnlyList<Connection> Connections,
    WorkflowMetadata Metadata);
