using System.Diagnostics;
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
        if (!string.IsNullOrEmpty(errorType))
        {
            TaskExecutionErrors.Add(1, new KeyValuePair<string, object?>("strategy", strategy), 
                new KeyValuePair<string, object?>("task_type", taskType),
                new KeyValuePair<string, object?>("error_type", errorType));
        }
        else
        {
            TaskExecutionErrors.Add(1, new KeyValuePair<string, object?>("strategy", strategy), 
                new KeyValuePair<string, object?>("task_type", taskType));
        }

        TaskExecutionErrorsByStrategy.Add(1, new KeyValuePair<string, object?>("strategy", strategy));
        TaskExecutionErrorsByType.Add(1, new KeyValuePair<string, object?>("task_type", taskType));
    }

    // Helper method to record LLM error
    public static void RecordLlmError(string model, string? errorType = null)
    {
        if (!string.IsNullOrEmpty(errorType))
        {
            LlmErrors.Add(1, new KeyValuePair<string, object?>("model", model),
                new KeyValuePair<string, object?>("error_type", errorType));
        }
        else
        {
            LlmErrors.Add(1, new KeyValuePair<string, object?>("model", model));
        }

        LlmErrorsByModel.Add(1, new KeyValuePair<string, object?>("model", model));
    }

    // Helper method to record external service error
    public static void RecordExternalServiceError(string service, string? errorType = null)
    {
        if (!string.IsNullOrEmpty(errorType))
        {
            ExternalServiceErrors.Add(1, new KeyValuePair<string, object?>("service", service),
                new KeyValuePair<string, object?>("error_type", errorType));
        }
        else
        {
            ExternalServiceErrors.Add(1, new KeyValuePair<string, object?>("service", service));
        }

        ExternalServiceErrorsByService.Add(1, new KeyValuePair<string, object?>("service", service));
    }

    // Helper method to record validation error
    public static void RecordValidationError(string? validationType = null)
    {
        if (!string.IsNullOrEmpty(validationType))
        {
            ValidationErrors.Add(1, new KeyValuePair<string, object?>("validation_type", validationType));
        }
        else
        {
            ValidationErrors.Add(1);
        }
    }

    // Helper method to record plan execution error
    public static void RecordPlanExecutionError(string? planId = null, string? errorType = null)
    {
        var tags = new List<KeyValuePair<string, object?>>();
        if (!string.IsNullOrEmpty(planId))
        {
            tags.Add(new KeyValuePair<string, object?>("plan_id", planId));
        }
        if (!string.IsNullOrEmpty(errorType))
        {
            tags.Add(new KeyValuePair<string, object?>("error_type", errorType));
        }

        if (tags.Count > 0)
        {
            PlanExecutionErrors.Add(1, tags.ToArray());
        }
        else
        {
            PlanExecutionErrors.Add(1);
        }
    }

    // Helper method to record error by severity
    public static void RecordErrorBySeverity(string severity, string? errorType = null)
    {
        if (!string.IsNullOrEmpty(errorType))
        {
            ErrorsBySeverity.Add(1, new KeyValuePair<string, object?>("severity", severity),
                new KeyValuePair<string, object?>("error_type", errorType));
        }
        else
        {
            ErrorsBySeverity.Add(1, new KeyValuePair<string, object?>("severity", severity));
        }
    }

    // Helper method to record model failure
    public static void RecordModelFailure(string model, string? reason = null)
    {
        if (!string.IsNullOrEmpty(reason))
        {
            ModelFailures.Add(1, new KeyValuePair<string, object?>("model", model),
                new KeyValuePair<string, object?>("reason", reason));
        }
        else
        {
            ModelFailures.Add(1, new KeyValuePair<string, object?>("model", model));
        }

        ModelFailuresByModel.Add(1, new KeyValuePair<string, object?>("model", model));
    }
}

