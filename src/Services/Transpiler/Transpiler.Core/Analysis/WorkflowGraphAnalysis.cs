using Transpiler.Core.Model;

namespace Transpiler.Core.Analysis;

/// <summary>
/// Pure, hand-rolled graph algorithms over <see cref="WorkflowGraph"/> — no external graph
/// library dependency. Mirrors the reference Python transpiler's approach: a Kahn's-algorithm
/// topological sort and a reachable-set-intersection merge-point search, both restricted to
/// <see cref="ConnectionType.Main"/> edges (auxiliary edges don't affect execution order).
/// </summary>
public static class WorkflowGraphAnalysis
{
    private static ILookup<string, string> MainEdges(WorkflowGraph graph) =>
        graph.Connections
            .Where(c => c.Type == ConnectionType.Main)
            .ToLookup(c => c.SourceNodeId, c => c.TargetNodeId);

    public static bool HasCycle(WorkflowGraph graph) => !TryTopologicalSort(graph, out _);

    /// <summary>Dependency-first node-id order over main-flow edges only.</summary>
    /// <exception cref="InvalidOperationException">The main-flow subgraph contains a cycle.</exception>
    public static IReadOnlyList<string> TopologicalOrder(WorkflowGraph graph)
    {
        if (!TryTopologicalSort(graph, out var order))
        {
            throw new InvalidOperationException("Workflow contains a cycle in the main execution flow — cannot transpile.");
        }

        return order;
    }

    private static bool TryTopologicalSort(WorkflowGraph graph, out IReadOnlyList<string> order)
    {
        var edges = MainEdges(graph);
        var inDegree = graph.Nodes.ToDictionary(n => n.Id, _ => 0);
        foreach (var node in graph.Nodes)
        {
            foreach (var target in edges[node.Id])
            {
                if (inDegree.ContainsKey(target))
                {
                    inDegree[target]++;
                }
            }
        }

        var queue = new Queue<string>(graph.Nodes.Where(n => inDegree[n.Id] == 0).Select(n => n.Id));
        var result = new List<string>();

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            result.Add(id);
            foreach (var target in edges[id])
            {
                if (!inDegree.ContainsKey(target))
                {
                    continue;
                }

                if (--inDegree[target] == 0)
                {
                    queue.Enqueue(target);
                }
            }
        }

        order = result;
        return result.Count == graph.Nodes.Count;
    }

    /// <summary>
    /// The topologically-earliest node reachable from every direct successor of
    /// <paramref name="branchNodeId"/> — where an If/Switch node's branches converge.
    /// Returns null if the branches never reconverge.
    /// </summary>
    public static string? FindMergePoint(WorkflowGraph graph, string branchNodeId)
    {
        var edges = MainEdges(graph);
        var successors = edges[branchNodeId].ToList();
        if (successors.Count < 2)
        {
            return null;
        }

        var reachablePerBranch = successors
            .Select(s => ReachableFrom(edges, s))
            .ToList();

        var common = reachablePerBranch[0];
        foreach (var set in reachablePerBranch.Skip(1))
        {
            common.IntersectWith(set);
        }

        if (common.Count == 0)
        {
            return null;
        }

        if (!TryTopologicalSort(graph, out var topo))
        {
            return null;
        }

        return topo.FirstOrDefault(common.Contains);
    }

    private static HashSet<string> ReachableFrom(ILookup<string, string> edges, string start)
    {
        var visited = new HashSet<string> { start };
        var queue = new Queue<string>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            foreach (var next in edges[queue.Dequeue()])
            {
                if (visited.Add(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        return visited;
    }

    /// <summary>
    /// All nodes on the path from <paramref name="start"/> up to (not including)
    /// <paramref name="mergePoint"/>, in topological order — the body of one If/Switch branch.
    /// </summary>
    public static IReadOnlyList<string> BranchSubgraph(WorkflowGraph graph, string start, string? mergePoint)
    {
        var edges = MainEdges(graph);
        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!visited.Add(id) || id == mergePoint)
            {
                continue;
            }

            foreach (var next in edges[id])
            {
                if (!visited.Contains(next))
                {
                    queue.Enqueue(next);
                }
            }
        }

        visited.Remove(mergePoint ?? string.Empty);

        return TryTopologicalSort(graph, out var topo)
            ? topo.Where(visited.Contains).ToList()
            : visited.ToList();
    }
}
