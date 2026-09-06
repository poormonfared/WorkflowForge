namespace Transpiler.Core;

public enum DiagnosticSeverity
{
    Info,
    Warning,
}

public sealed record ParseDiagnostic(DiagnosticSeverity Severity, string Message, string? NodeId = null);

public sealed record ParseOptions;
