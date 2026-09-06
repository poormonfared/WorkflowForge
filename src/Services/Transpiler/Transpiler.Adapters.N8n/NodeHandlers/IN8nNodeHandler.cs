using Transpiler.Adapters.N8n.Json;
using Transpiler.Core.Model;

namespace Transpiler.Adapters.N8n.NodeHandlers;

/// <summary>
/// One small class per n8n node type (or family of type-version aliases), mirroring the reference
/// Python transpiler's per-type handler files. Registered in <see cref="NodeHandlerRegistry"/>.
/// </summary>
public interface IN8nNodeHandler
{
    IReadOnlyList<string> SupportedTypes { get; }

    WorkflowNode Map(N8nNodeDto dto);
}
