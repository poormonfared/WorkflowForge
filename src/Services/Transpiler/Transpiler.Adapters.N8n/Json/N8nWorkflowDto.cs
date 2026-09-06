using System.Text.Json;

namespace Transpiler.Adapters.N8n.Json;

/// <summary>Raw shape of an n8n workflow export — deserialization target only, never exposed outside this project.</summary>
public sealed class N8nWorkflowDto
{
    public string Name { get; set; } = "Untitled Workflow";

    public List<N8nNodeDto> Nodes { get; set; } = new();

    /// <summary>sourceNodeName -> connectionType ("main", "ai_tool", ...) -> outputs (one list per output index) -> targets.</summary>
    public Dictionary<string, Dictionary<string, List<List<N8nConnectionTargetDto>>>> Connections { get; set; } = new();
}

public sealed class N8nNodeDto
{
    public string? Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public double TypeVersion { get; set; } = 1;
    public JsonElement Parameters { get; set; }
    public bool? Disabled { get; set; }
}

public sealed class N8nConnectionTargetDto
{
    public required string Node { get; set; }
    public string Type { get; set; } = "main";
    public int Index { get; set; }
}
