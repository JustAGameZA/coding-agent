namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Registry for dynamic tool discovery in agentic AI
/// </summary>
public interface IToolRegistry
{
    Task RegisterToolAsync(Tool tool, CancellationToken ct);
    Task<IEnumerable<Tool>> DiscoverToolsAsync(string query, CancellationToken ct);
    Task<Tool?> GetToolAsync(string toolName, CancellationToken ct);
    Task<ToolInvocationResult> InvokeToolAsync(string toolName, Dictionary<string, object> parameters, CancellationToken ct);
}

/// <summary>
/// Tool definition for function calling
/// </summary>
public class Tool
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ToolParameter> Parameters { get; set; } = new();
    public string Endpoint { get; set; } = string.Empty; // HTTP endpoint or internal service method
    public ToolType Type { get; set; }
}

public enum ToolType
{
    HttpEndpoint,
    InternalService,
    LLMFunction
}

/// <summary>
/// Tool parameter definition
/// </summary>
public class ToolParameter
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // "string", "number", "boolean", "object"
    public string? Description { get; set; }
    public bool Required { get; set; }
}

/// <summary>
/// Result of tool invocation
/// </summary>
public class ToolInvocationResult
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    public TimeSpan Duration { get; set; }
}

