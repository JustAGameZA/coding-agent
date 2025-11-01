using System.Diagnostics.Metrics;

namespace CodingAgent.Services.Orchestration.Infrastructure.Metrics;

/// <summary>
/// Custom metrics for error tracking in Orchestration service.
/// </summary>
public static class ErrorMetrics
{
    private static readonly Meter Meter = new("CodingAgent.Services.Orchestration.Errors", "1.0.0");

    // Error counters by type
    public static readonly Counter<long> TaskExecutionErrors = Meter.CreateCounter<long>(
        "orchestration_task_execution_errors_total",
        "errors",
        "Total number of task execution errors");

    public static readonly Counter<long> TaskExecutionErrorsByStrategy = Meter.CreateCounter<long>(
        "orchestration_task_execution_errors_by_strategy_total",
        "errors",
        "Total number of task execution errors by strategy");

    public static readonly Counter<long> TaskExecutionErrorsByType = Meter.CreateCounter<long>(
        "orchestration_task_execution_errors_by_type_total",
        "errors",
        "Total number of task execution errors by task type");

    // LLM/Model errors
    public static readonly Counter<long> LlmErrors = Meter.CreateCounter<long>(
        "orchestration_llm_errors_total",
        "errors",
        "Total number of LLM inference errors");

    public static readonly Counter<long> LlmErrorsByModel = Meter.CreateCounter<long>(
        "orchestration_llm_errors_by_model_total",
        "errors",
        "Total number of LLM inference errors by model");

    // External service errors
    public static readonly Counter<long> ExternalServiceErrors = Meter.CreateCounter<long>(
        "orchestration_external_service_errors_total",
        "errors",
        "Total number of external service errors");

    public static readonly Counter<long> ExternalServiceErrorsByService = Meter.CreateCounter<long>(
        "orchestration_external_service_errors_by_service_total",
        "errors",
        "Total number of external service errors by service");

    // Validation errors
    public static readonly Counter<long> ValidationErrors = Meter.CreateCounter<long>(
        "orchestration_validation_errors_total",
        "errors",
        "Total number of validation errors");

    // Plan execution errors
    public static readonly Counter<long> PlanExecutionErrors = Meter.CreateCounter<long>(
        "orchestration_plan_execution_errors_total",
        "errors",
        "Total number of plan execution errors");

    // Error severity tracking
    public static readonly Counter<long> ErrorsBySeverity = Meter.CreateCounter<long>(
        "orchestration_errors_by_severity_total",
        "errors",
        "Total number of errors by severity (critical/warning/info)");

    // Model performance tracking
    public static readonly Counter<long> ModelFailures = Meter.CreateCounter<long>(
        "orchestration_model_failures_total",
        "errors",
        "Total number of model failures");

    public static readonly Counter<long> ModelFailuresByModel = Meter.CreateCounter<long>(
        "orchestration_model_failures_by_model_total",
        "errors",
        "Total number of model failures by model");

    // A/B testing errors
    public static readonly Counter<long> AbTestErrors = Meter.CreateCounter<long>(
        "orchestration_ab_test_errors_total",
        "errors",
        "Total number of A/B test errors");

    // Helper method to record task execution error
    public static void RecordTaskExecutionError(string strategy, string taskType, string? errorType = null)
    {
        var tags = new TagList { { "strategy", strategy }, { "task_type", taskType } };
        if (!string.IsNullOrEmpty(errorType))
        {
            tags.Add("error_type", errorType);
        }

        TaskExecutionErrors.Add(1, tags);
        TaskExecutionErrorsByStrategy.Add(1, new TagList { { "strategy", strategy } });
        TaskExecutionErrorsByType.Add(1, new TagList { { "task_type", taskType } });
    }

    // Helper method to record LLM error
    public static void RecordLlmError(string model, string? errorType = null)
    {
        var tags = new TagList { { "model", model } };
        if (!string.IsNullOrEmpty(errorType))
        {
            tags.Add("error_type", errorType);
        }

        LlmErrors.Add(1, tags);
        LlmErrorsByModel.Add(1, new TagList { { "model", model } });
    }

    // Helper method to record external service error
    public static void RecordExternalServiceError(string service, string? errorType = null)
    {
        var tags = new TagList { { "service", service } };
        if (!string.IsNullOrEmpty(errorType))
        {
            tags.Add("error_type", errorType);
        }

        ExternalServiceErrors.Add(1, tags);
        ExternalServiceErrorsByService.Add(1, new TagList { { "service", service } });
    }

    // Helper method to record validation error
    public static void RecordValidationError(string? validationType = null)
    {
        var tags = new TagList();
        if (!string.IsNullOrEmpty(validationType))
        {
            tags.Add("validation_type", validationType);
        }

        ValidationErrors.Add(1, tags);
    }

    // Helper method to record plan execution error
    public static void RecordPlanExecutionError(string? planId = null, string? errorType = null)
    {
        var tags = new TagList();
        if (!string.IsNullOrEmpty(planId))
        {
            tags.Add("plan_id", planId);
        }
        if (!string.IsNullOrEmpty(errorType))
        {
            tags.Add("error_type", errorType);
        }

        PlanExecutionErrors.Add(1, tags);
    }

    // Helper method to record error by severity
    public static void RecordErrorBySeverity(string severity, string? errorType = null)
    {
        var tags = new TagList { { "severity", severity } };
        if (!string.IsNullOrEmpty(errorType))
        {
            tags.Add("error_type", errorType);
        }

        ErrorsBySeverity.Add(1, tags);
    }

    // Helper method to record model failure
    public static void RecordModelFailure(string model, string? reason = null)
    {
        var tags = new TagList { { "model", model } };
        if (!string.IsNullOrEmpty(reason))
        {
            tags.Add("reason", reason);
        }

        ModelFailures.Add(1, tags);
        ModelFailuresByModel.Add(1, new TagList { { "model", model } });
    }
}

