using System.Text.RegularExpressions;
using Transpiler.Core;
using Transpiler.Core.Model;

namespace Transpiler.CodeGen.CSharp.Emission;

/// <summary>
/// Deliberately narrow v1 scope (see the plan's open question on expression syntax): translates
/// only <c>$json.field</c> / <c>$json["field"]</c> variable references against a
/// <see cref="System.Text.Json.Nodes.JsonNode"/>-shaped current item. Everything else — ternaries,
/// arrow-function array methods, template literals — falls back to a TODO placeholder rather than
/// guessing, matching the same non-throwing fallback philosophy as unsupported nodes (§4).
/// </summary>
public static partial class ExpressionTranslator
{
    [GeneratedRegex(@"^=?\s*\{\{\s*\$json(?:\.(?<dot>[a-zA-Z_]\w*)|\[""(?<bracket>[^""]+)""\])\s*\}\}\s*$")]
    private static partial Regex JsonFieldTemplate();

    /// <summary>Renders a literal or expression parameter as a C# expression string, assignable to a <c>JsonNode?</c>.</summary>
    public static string Translate(ParameterValue value, CodeGenContext ctx, string nodeId)
    {
        switch (value)
        {
            case LiteralParameter { Value: null }:
                return "null";
            case LiteralParameter { Value: bool b }:
                return b ? "true" : "false";
            case LiteralParameter { Value: string s }:
                return EscapeStringLiteral(s);
            case LiteralParameter { Value: long or int or double }:
                return Convert.ToString(((LiteralParameter)value).Value, System.Globalization.CultureInfo.InvariantCulture) ?? "0";
            case ExpressionParameter expr:
                return TranslateExpression(expr.RawExpression, ctx, nodeId);
            default:
                return "null";
        }
    }

    private static string TranslateExpression(string raw, CodeGenContext ctx, string nodeId)
    {
        var match = JsonFieldTemplate().Match(raw);
        if (match.Success)
        {
            var field = match.Groups["dot"].Success ? match.Groups["dot"].Value : match.Groups["bracket"].Value;
            return $"{ctx.Names.CurrentVariable}[\"{field}\"]?.DeepClone()";
        }

        ctx.Warn($"Expression not translated in v1 (falls back to null): {raw}", nodeId);
        return $"null /* TODO(WorkflowForge): translate expression: {raw.Replace("*/", "* /")} */";
    }

    private static string EscapeStringLiteral(string s) => $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
}
