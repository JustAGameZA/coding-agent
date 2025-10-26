using CodingAgent.Services.CICDMonitor.Domain.Entities;
using CodingAgent.Services.CICDMonitor.Domain.Services;
using CodingAgent.SharedKernel.Domain.Events;
using MassTransit;

namespace CodingAgent.Services.CICDMonitor.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer for BuildFailedEvent to trigger automated fixes.
/// </summary>
public class BuildFailedEventConsumer : IConsumer<BuildFailedEvent>
{
    private readonly IAutomatedFixService _automatedFixService;
    private readonly ILogger<BuildFailedEventConsumer> _logger;

    public BuildFailedEventConsumer(
        IAutomatedFixService automatedFixService,
        ILogger<BuildFailedEventConsumer> logger)
    {
        _automatedFixService = automatedFixService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<BuildFailedEvent> context)
    {
        var e = context.Message;

        _logger.LogInformation(
            "[CICDMonitor] Consumed BuildFailedEvent: BuildId={BuildId}, Repository={Repository}, Branch={Branch}",
            e.BuildId, e.Repository, e.Branch);

        try
        {
            // Create BuildFailure entity from event
            var buildFailure = new BuildFailure
            {
                Id = e.BuildId,
                Repository = e.Repository,
                Branch = e.Branch,
                CommitSha = e.CommitSha,
                ErrorMessage = e.ErrorMessage,
                ErrorLog = e.ErrorLog,
                WorkflowName = e.WorkflowName,
                JobName = e.JobName,
                FailedAt = e.OccurredAt
            };

            // Process the build failure and attempt automated fix
            var fixAttempt = await _automatedFixService.ProcessBuildFailureAsync(
                buildFailure,
                context.CancellationToken);

            if (fixAttempt != null)
            {
                _logger.LogInformation(
                    "[CICDMonitor] Created fix attempt {FixAttemptId} for build failure {BuildId}",
                    fixAttempt.Id, e.BuildId);
            }
            else
            {
                _logger.LogInformation(
                    "[CICDMonitor] No fix attempt created for build failure {BuildId} - error pattern not recognized",
                    e.BuildId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[CICDMonitor] Failed to process BuildFailedEvent for BuildId={BuildId}",
                e.BuildId);
            throw;
        }
    }
}
