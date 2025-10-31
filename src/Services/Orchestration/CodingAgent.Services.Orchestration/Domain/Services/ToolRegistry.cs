using System.Text.Json;

namespace CodingAgent.Services.Orchestration.Domain.Services;

/// <summary>
/// Registry for dynamic tool discovery in agentic AI
/// </summary>
public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, Tool> _tools = new();
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<ToolRegistry> _logger;

    public ToolRegistry(
        ILogger<ToolRegistry> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        
        // Register built-in tools
        RegisterBuiltInTools();
    }

    public Task RegisterToolAsync(Tool tool, CancellationToken ct)
    {
        _logger.LogInformation("Registering tool: {ToolName}", tool.Name);
        _tools[tool.Name] = tool;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<Tool>> DiscoverToolsAsync(string query, CancellationToken ct)
    {
        _logger.LogDebug("Discovering tools for query: {Query}", query);
        
        // Simple text search - can be enhanced with semantic search
        var matchingTools = _tools.Values
            .Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       t.Description.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult<IEnumerable<Tool>>(matchingTools);
    }

    public Task<Tool?> GetToolAsync(string toolName, CancellationToken ct)
    {
        _tools.TryGetValue(toolName, out var tool);
        return Task.FromResult(tool);
    }

    public async Task<ToolInvocationResult> InvokeToolAsync(
        string toolName,
        Dictionary<string, object> parameters,
        CancellationToken ct)
    {
        _logger.LogInformation("Invoking tool: {ToolName} with {ParameterCount} parameters", toolName, parameters.Count);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var tool = await GetToolAsync(toolName, ct);
            if (tool == null)
            {
                return new ToolInvocationResult
                {
                    Success = false,
                    Error = $"Tool {toolName} not found",
                    Duration = stopwatch.Elapsed
                };
            }

            // Validate parameters
            var validationResult = ValidateParameters(tool, parameters);
            if (!validationResult.IsValid)
            {
                return new ToolInvocationResult
                {
                    Success = false,
                    Error = validationResult.Error,
                    Duration = stopwatch.Elapsed
                };
            }

            // Invoke tool based on type
            object? result = tool.Type switch
            {
                ToolType.HttpEndpoint => await InvokeHttpEndpointAsync(tool, parameters, ct),
                ToolType.InternalService => await InvokeInternalServiceAsync(tool, parameters, ct),
                ToolType.LLMFunction => await InvokeLLMFunctionAsync(tool, parameters, ct),
                _ => throw new NotSupportedException($"Tool type {tool.Type} not supported")
            };

            stopwatch.Stop();

            return new ToolInvocationResult
            {
                Success = true,
                Result = result,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to invoke tool {ToolName}", toolName);
            return new ToolInvocationResult
            {
                Success = false,
                Error = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    private (bool IsValid, string? Error) ValidateParameters(Tool tool, Dictionary<string, object> parameters)
    {
        foreach (var param in tool.Parameters.Where(p => p.Required))
        {
            if (!parameters.ContainsKey(param.Name))
            {
                return (false, $"Required parameter {param.Name} is missing");
            }
        }

        // Type validation would go here
        return (true, null);
    }

    private async Task<object?> InvokeHttpEndpointAsync(Tool tool, Dictionary<string, object> parameters, CancellationToken ct)
    {
        if (_httpClientFactory == null)
        {
            throw new InvalidOperationException("HttpClientFactory not available");
        }

        var client = _httpClientFactory.CreateClient();
        
        // Simple GET request for now - can be enhanced
        var response = await client.GetAsync(tool.Endpoint, ct);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<object>(content);
    }

    private Task<object?> InvokeInternalServiceAsync(Tool tool, Dictionary<string, object> parameters, CancellationToken ct)
    {
        // Internal service invocation would map to service methods
        // For now, return null
        _logger.LogWarning("Internal service invocation not yet implemented for tool {ToolName}", tool.Name);
        return Task.FromResult<object?>(null);
    }

    private Task<object?> InvokeLLMFunctionAsync(Tool tool, Dictionary<string, object> parameters, CancellationToken ct)
    {
        // LLM function calling would use function calling API
        // For now, return null
        _logger.LogWarning("LLM function invocation not yet implemented for tool {ToolName}", tool.Name);
        return Task.FromResult<object?>(null);
    }

    private void RegisterBuiltInTools()
    {
        // Register built-in tools
        _tools["create_pr"] = new Tool
        {
            Name = "create_pr",
            Description = "Creates a pull request in GitHub",
            Type = ToolType.InternalService,
            Parameters = new List<ToolParameter>
            {
                new ToolParameter { Name = "title", Type = "string", Required = true },
                new ToolParameter { Name = "body", Type = "string", Required = true },
                new ToolParameter { Name = "branch", Type = "string", Required = true }
            }
        };

        _tools["run_tests"] = new Tool
        {
            Name = "run_tests",
            Description = "Runs tests for a specific project",
            Type = ToolType.InternalService,
            Parameters = new List<ToolParameter>
            {
                new ToolParameter { Name = "project_path", Type = "string", Required = true }
            }
        };

        _tools["read_file"] = new Tool
        {
            Name = "read_file",
            Description = "Reads a file from the codebase",
            Type = ToolType.InternalService,
            Parameters = new List<ToolParameter>
            {
                new ToolParameter { Name = "file_path", Type = "string", Required = true }
            }
        };
    }
}

