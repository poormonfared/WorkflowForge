namespace Transpiler.Core.Model;

public abstract record ParameterValue;

public sealed record LiteralParameter(object? Value) : ParameterValue;

/// <summary>A source-format expression (e.g. n8n's <c>={{ $json.field }}</c>) not yet translated to C#.</summary>
public sealed record ExpressionParameter(string RawExpression) : ParameterValue;

public sealed record CollectionParameter(IReadOnlyList<ParameterValue> Items) : ParameterValue;

public sealed record MapParameter(IReadOnlyDictionary<string, ParameterValue> Entries) : ParameterValue;
