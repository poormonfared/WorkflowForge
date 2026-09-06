using System.Text.Json;
using Transpiler.Core.Model;

namespace Transpiler.Adapters.N8n.Json;

/// <summary>Walks a raw n8n "parameters" JSON subtree into the platform-agnostic <see cref="ParameterValue"/> tree.</summary>
public static class ParameterValueReader
{
    public static IReadOnlyDictionary<string, ParameterValue> ReadObject(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return new Dictionary<string, ParameterValue>();
        }

        return element.EnumerateObject().ToDictionary(p => p.Name, p => Read(p.Value));
    }

    public static ParameterValue Read(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => ReadString(element.GetString() ?? string.Empty),
        JsonValueKind.Number => new LiteralParameter(element.TryGetInt64(out var i) ? i : element.GetDouble()),
        JsonValueKind.True => new LiteralParameter(true),
        JsonValueKind.False => new LiteralParameter(false),
        JsonValueKind.Null or JsonValueKind.Undefined => new LiteralParameter(null),
        JsonValueKind.Array => new CollectionParameter(element.EnumerateArray().Select(Read).ToList()),
        JsonValueKind.Object => new MapParameter(ReadObject(element)),
        _ => new LiteralParameter(null),
    };

    /// <summary>
    /// n8n marks a field as a dynamic expression either with a leading '=' (fixed-vs-expression
    /// mode toggle) or by embedding a <c>{{ ... }}</c> template fragment. Anything else is a
    /// plain literal string.
    /// </summary>
    private static ParameterValue ReadString(string raw) =>
        raw.StartsWith('=') || raw.Contains("{{", StringComparison.Ordinal)
            ? new ExpressionParameter(raw)
            : new LiteralParameter(raw);
}
