using Transpiler.Core.Model;

namespace Transpiler.CodeGen.CSharp.Emission;

public static class ParameterValueCodeGenExtensions
{
    /// <summary>The plain string for a literal string value, or null — used for structural fields (e.g. a Set field's "name") that are never expressions.</summary>
    public static string? AsLiteralString(this ParameterValue value) =>
        value is LiteralParameter { Value: string s } ? s : null;

    /// <summary>The numeric value for a literal number, or null — used for structural fields (e.g. schedule intervals) that are never expressions.</summary>
    public static double? AsNumber(this ParameterValue value) => value switch
    {
        LiteralParameter { Value: long l } => l,
        LiteralParameter { Value: double d } => d,
        LiteralParameter { Value: int i } => i,
        _ => null,
    };
}
