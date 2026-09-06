using Transpiler.Core.Analysis;
using Transpiler.Core.Model;

namespace Transpiler.Core.Tests;

public class WorkflowGraphAnalysisTests
{
    private static WorkflowNode Node(string id, NodeKind kind = NodeKind.Action) =>
        new(id, id, kind, $"test.{id}", new Dictionary<string, ParameterValue>(), Disabled: false);

    private static Connection MainEdge(string from, string to, int outputIndex = 0) =>
        new(from, to, ConnectionType.Main, AuxiliaryTag: null, outputIndex);

    [Fact]
    public void TopologicalOrder_OrdersLinearChainByDependency()
    {
        var graph = new WorkflowGraph(
            "linear",
            [Node("A"), Node("B"), Node("C")],
            [MainEdge("A", "B"), MainEdge("B", "C")],
            new WorkflowMetadata(TriggerKind.Manual, new Dictionary<string, string>()));

        var order = WorkflowGraphAnalysis.TopologicalOrder(graph);

        Assert.Equal(["A", "B", "C"], order);
    }

    [Fact]
    public void TopologicalOrder_ThrowsOnCycle()
    {
        var graph = new WorkflowGraph(
            "cyclic",
            [Node("A"), Node("B")],
            [MainEdge("A", "B"), MainEdge("B", "A")],
            new WorkflowMetadata(TriggerKind.Manual, new Dictionary<string, string>()));

        Assert.Throws<InvalidOperationException>(() => WorkflowGraphAnalysis.TopologicalOrder(graph));
        Assert.True(WorkflowGraphAnalysis.HasCycle(graph));
    }

    [Fact]
    public void FindMergePoint_FindsFirstNodeCommonToBothBranches()
    {
        // If(A) -> true: B -> D ; false: C -> D
        var graph = new WorkflowGraph(
            "branching",
            [Node("A", NodeKind.Condition), Node("B"), Node("C"), Node("D")],
            [MainEdge("A", "B", 0), MainEdge("A", "C", 1), MainEdge("B", "D"), MainEdge("C", "D")],
            new WorkflowMetadata(TriggerKind.Manual, new Dictionary<string, string>()));

        var mergePoint = WorkflowGraphAnalysis.FindMergePoint(graph, "A");

        Assert.Equal("D", mergePoint);
    }

    [Fact]
    public void FindMergePoint_ReturnsNullWhenBranchesNeverReconverge()
    {
        var graph = new WorkflowGraph(
            "divergent",
            [Node("A", NodeKind.Condition), Node("B"), Node("C")],
            [MainEdge("A", "B", 0), MainEdge("A", "C", 1)],
            new WorkflowMetadata(TriggerKind.Manual, new Dictionary<string, string>()));

        Assert.Null(WorkflowGraphAnalysis.FindMergePoint(graph, "A"));
    }

    [Fact]
    public void BranchSubgraph_StopsBeforeMergePoint()
    {
        var graph = new WorkflowGraph(
            "branching",
            [Node("A", NodeKind.Condition), Node("B"), Node("C"), Node("D")],
            [MainEdge("A", "B", 0), MainEdge("A", "C", 1), MainEdge("B", "D"), MainEdge("C", "D")],
            new WorkflowMetadata(TriggerKind.Manual, new Dictionary<string, string>()));

        var branch = WorkflowGraphAnalysis.BranchSubgraph(graph, "B", "D");

        Assert.Equal(["B"], branch);
    }
}
