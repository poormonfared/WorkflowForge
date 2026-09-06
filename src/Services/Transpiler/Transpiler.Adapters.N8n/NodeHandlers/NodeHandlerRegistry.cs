namespace Transpiler.Adapters.N8n.NodeHandlers;

/// <summary>
/// Explicit static registration, keyed by lowercased n8n type string — mirrors the reference
/// Python transpiler's <c>@register</c> decorator / registry module, minus the reflection.
/// </summary>
public static class NodeHandlerRegistry
{
    private static readonly Dictionary<string, IN8nNodeHandler> Handlers = BuildRegistry();

    public static IN8nNodeHandler? Find(string n8nType) =>
        Handlers.GetValueOrDefault(n8nType.ToLowerInvariant());

    public static IReadOnlyCollection<string> SupportedTypes => Handlers.Keys;

    private static Dictionary<string, IN8nNodeHandler> BuildRegistry()
    {
        var handlers = new IN8nNodeHandler[]
        {
            new ManualTriggerHandler(),
            new WebhookTriggerHandler(),
            new ScheduleTriggerHandler(),
            new SetHandler(),
            new IfHandler(),
            new HttpRequestHandler(),
        };

        var map = new Dictionary<string, IN8nNodeHandler>();
        foreach (var handler in handlers)
        {
            foreach (var type in handler.SupportedTypes)
            {
                map[type.ToLowerInvariant()] = handler;
            }
        }

        return map;
    }
}
