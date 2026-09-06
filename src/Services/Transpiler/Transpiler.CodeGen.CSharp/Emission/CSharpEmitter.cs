using System.Text;

namespace Transpiler.CodeGen.CSharp.Emission;

/// <summary>Walks an <see cref="EmittedNode"/> tree into indented C# source text. The only place that writes C# text.</summary>
public static class CSharpEmitter
{
    public static string EmitBlock(IEnumerable<EmittedNode> nodes, int indentLevel)
    {
        var sb = new StringBuilder();
        foreach (var node in nodes)
        {
            EmitNode(sb, node, indentLevel);
        }

        return sb.ToString();
    }

    private static void EmitNode(StringBuilder sb, EmittedNode node, int indentLevel)
    {
        var prefix = new string(' ', indentLevel * 4);

        if (node.Comment is not null)
        {
            sb.AppendLine($"{prefix}// {node.Comment}");
        }

        switch (node.Kind)
        {
            case EmittedNodeKind.Statement:
                foreach (var line in node.Lines)
                {
                    sb.AppendLine(line.Length == 0 ? line : $"{prefix}{line}");
                }

                break;

            case EmittedNodeKind.IfBranch:
                // All lines but the last are a preamble (e.g. declaring the merge variable the
                // branches assign into, since a C# `var` inside a branch block can't otherwise
                // escape it) — the last line is the "if (...)" condition itself.
                foreach (var preambleLine in node.Lines.Take(Math.Max(0, node.Lines.Count - 1)))
                {
                    sb.AppendLine($"{prefix}{preambleLine}");
                }

                var condition = node.Lines.Count > 0 ? node.Lines[^1] : "if (true)";
                sb.AppendLine($"{prefix}{condition}");
                sb.AppendLine($"{prefix}{{");
                if (node.Branches.TryGetValue("true", out var trueBranch) && trueBranch.Count > 0)
                {
                    foreach (var child in trueBranch)
                    {
                        EmitNode(sb, child, indentLevel + 1);
                    }
                }

                sb.AppendLine($"{prefix}}}");
                sb.AppendLine($"{prefix}else");
                sb.AppendLine($"{prefix}{{");
                if (node.Branches.TryGetValue("false", out var falseBranch) && falseBranch.Count > 0)
                {
                    foreach (var child in falseBranch)
                    {
                        EmitNode(sb, child, indentLevel + 1);
                    }
                }

                sb.AppendLine($"{prefix}}}");
                break;
        }
    }
}
