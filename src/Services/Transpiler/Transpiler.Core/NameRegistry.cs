using System.Text.RegularExpressions;

namespace Transpiler.Core;

/// <summary>Maps a source node id to a safe, unique C# identifier stem.</summary>
public sealed partial class NameRegistry
{
    private readonly Dictionary<string, string> _map = new();
    private readonly HashSet<string> _used = new();

    /// <summary>The variable name holding the pipeline's current value — what the next node reads as input.</summary>
    public string CurrentVariable { get; private set; } = "initialItem";

    /// <summary>Advances <see cref="CurrentVariable"/> to a node's own output variable, after it has run.</summary>
    public void AdvanceCurrent(string variableName) => CurrentVariable = variableName;

    public string Register(string nodeId, string displayName)
    {
        if (_map.TryGetValue(nodeId, out var existing))
        {
            return existing;
        }

        var stem = Sanitize(displayName);
        var candidate = stem;
        var suffix = 1;
        while (!_used.Add(candidate))
        {
            candidate = $"{stem}{++suffix}";
        }

        _map[nodeId] = candidate;
        return candidate;
    }

    public string Resolve(string nodeId) =>
        _map.TryGetValue(nodeId, out var name)
            ? name
            : throw new KeyNotFoundException($"Node id '{nodeId}' was never registered.");

    private static string Sanitize(string displayName)
    {
        var cleaned = NonAlphaNumeric().Replace(displayName.Trim(), "_");
        cleaned = CollapseUnderscores().Replace(cleaned, "_").Trim('_');
        if (string.IsNullOrEmpty(cleaned))
        {
            cleaned = "node";
        }

        var camel = string.Concat(cleaned.Split('_').Select((part, i) =>
            i == 0 ? part.ToLowerInvariant() : CultureInfoTitleCase(part)));

        return char.IsDigit(camel[0]) ? $"n_{camel}" : camel;
    }

    private static string CultureInfoTitleCase(string part) =>
        part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();

    [GeneratedRegex("[^a-zA-Z0-9]+")]
    private static partial Regex NonAlphaNumeric();

    [GeneratedRegex("_+")]
    private static partial Regex CollapseUnderscores();
}
