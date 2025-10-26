using CodingAgent.Services.CICDMonitor.Domain.Services;
using CodingAgent.SharedKernel.Domain.Events;
using MassTransit;

namespace CodingAgent.Services.CICDMonitor.Infrastructure.Messaging.Consumers;

public class TaskCompletedEventConsumer : IConsumer<TaskCompletedEvent>
{
    private readonly IAutomatedFixService _automatedFixService;
    private readonly ILogger<TaskCompletedEventConsumer> _logger;

    public TaskCompletedEventConsumer(
        IAutomatedFixService automatedFixService,
        ILogger<TaskCompletedEventConsumer> logger)
    {
        _automatedFixService = automatedFixService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TaskCompletedEvent> context)
    {
        var e = context.Message;
        _logger.LogInformation(
            "[CICDMonitor] Consumed TaskCompletedEvent: TaskId={TaskId}, Success={Success}, CostUsd={CostUsd}, Duration={Duration}",
            e.TaskId, e.Success, e.CostUsd, e.Duration);

        try
        {
            // Process task completion to update fix attempt and potentially create PR
            await _automatedFixService.ProcessTaskCompletionAsync(
                e.TaskId,
                e.Success,
                context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[CICDMonitor] Failed to process TaskCompletedEvent for TaskId={TaskId}",
                e.TaskId);
            // Don't rethrow - we don't want to retry this event
        }
    }
}
