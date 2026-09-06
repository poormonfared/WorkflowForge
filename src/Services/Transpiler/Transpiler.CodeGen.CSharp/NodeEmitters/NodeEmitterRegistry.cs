using Transpiler.Core;
using Transpiler.Core.Model;
using Transpiler.CodeGen.CSharp.Emission;

namespace Transpiler.CodeGen.CSharp.NodeEmitters;

/// <summary>
/// Explicit static registration keyed by lowercased source type string, mirroring
/// <c>Transpiler.Adapters.N8n.NodeHandlers.NodeHandlerRegistry</c> on the parsing side. Also the
/// single dispatch point <see cref="IfEmitter"/> recurses through for its branch bodies.
/// </summary>
public sealed class NodeEmitterRegistry
{
    private readonly Dictionary<string, INodeEmitter> _handlers;

    public NodeEmitterRegistry()
    {
        var handlers = new INodeEmitter[]
        {
            new ManualTriggerEmitter(),
            new WebhookTriggerEmitter(),
            new ScheduleTriggerEmitter(),
            new SetEmitter(),
            new IfEmitter(this),
            new HttpRequestEmitter(),
        };

        _handlers = new Dictionary<string, INodeEmitter>();
        foreach (var handler in handlers)
        {
            foreach (var type in handler.SupportedSourceTypes)
            {
                _handlers[type.ToLowerInvariant()] = handler;
            }
        }
    }

    /// <summary>Dispatches to the registered emitter for the node's source type, or <see cref="FallbackEmitter"/> if none.</summary>
    public EmittedNode Emit(WorkflowNode node, WorkflowGraph graph, CodeGenContext ctx)
    {
        if (node.Kind != NodeKind.Unsupported && _handlers.TryGetValue(node.SourceTypeIdentifier.ToLowerInvariant(), out var handler))
        {
            return handler.Emit(node, graph, ctx);
        }

        return FallbackEmitter.Instance.Emit(node, graph, ctx);
    }
}
