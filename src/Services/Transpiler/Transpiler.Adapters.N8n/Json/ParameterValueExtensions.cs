using Transpiler.Core.Model;

namespace Transpiler.Adapters.N8n.Json;

public static class ParameterValueExtensions
{
    public static string GetString(this IReadOnlyDictionary<string, ParameterValue> parameters, string key, string fallback = "") =>
        parameters.TryGetValue(key, out var value) ? value.AsRawText() ?? fallback : fallback;

    /// <summary>The literal text, or the raw (untranslated) expression text — never null for string-shaped values.</summary>
    public static string? AsRawText(this ParameterValue value) => value switch
    {
        LiteralParameter { Value: string s } => s,
        LiteralParameter { Value: { } v } => v.ToString(),
        LiteralParameter => null,
        ExpressionParameter e => e.RawExpression,
        _ => null,
    };

    public static IReadOnlyDictionary<string, ParameterValue> GetMap(this IReadOnlyDictionary<string, ParameterValue> parameters, string key) =>
        parameters.TryGetValue(key, out var value) && value is MapParameter map
            ? map.Entries
            : new Dictionary<string, ParameterValue>();

    public static IReadOnlyList<ParameterValue> GetCollection(this IReadOnlyDictionary<string, ParameterValue> parameters, string key) =>
        parameters.TryGetValue(key, out var value) && value is CollectionParameter collection
            ? collection.Items
            : Array.Empty<ParameterValue>();
}
