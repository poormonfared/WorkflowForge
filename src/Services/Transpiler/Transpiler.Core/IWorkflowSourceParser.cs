using Transpiler.Core.Model;

namespace Transpiler.Core;

/// <summary>
/// Adapter-pattern boundary: implemented once per workflow source format (n8n today;
/// Activepieces/Node-RED later). <see cref="CodeGen"/>'s <see cref="IWorkflowCodeGenerator"/>
/// depends only on <see cref="WorkflowGraph"/>, never on an implementation of this interface.
/// </summary>
public interface IWorkflowSourceParser
{
    /// <summary>Short identifier for this source format, e.g. "n8n".</summary>
    string SourceFormatId { get; }

    WorkflowGraph Parse(string rawSource, ParseOptions options);

    IReadOnlyList<ParseDiagnostic> Diagnostics { get; }
}
