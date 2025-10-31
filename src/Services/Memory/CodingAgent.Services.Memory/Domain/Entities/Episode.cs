namespace CodingAgent.Services.Memory.Domain.Entities;

/// <summary>
/// Represents an episodic memory - a record of what happened during task execution
/// </summary>
public class Episode
{
    public Guid Id { get; private set; }
    public Guid? TaskId { get; private set; }
    public Guid? ExecutionId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string EventType { get; private set; } = string.Empty; // 'task_started', 'error_occurred', 'success'
    public Dictionary<string, object> Context { get; private set; } = new();
    public Dictionary<string, object> Outcome { get; private set; } = new();
    public List<string> LearnedPatterns { get; private set; } = new();
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private Episode() { }

    public Episode(
        Guid? taskId,
        Guid? executionId,
        Guid userId,
        DateTime timestamp,
        string eventType,
        Dictionary<string, object> context,
        Dictionary<string, object> outcome,
        List<string>? learnedPatterns = null)
    {
        Id = Guid.NewGuid();
        TaskId = taskId;
        ExecutionId = executionId;
        UserId = userId;
        Timestamp = timestamp;
        EventType = eventType;
        Context = context;
        Outcome = outcome;
        LearnedPatterns = learnedPatterns ?? new List<string>();
        CreatedAt = DateTime.UtcNow;
    }

    public void AddLearnedPattern(string pattern)
    {
        if (!string.IsNullOrWhiteSpace(pattern) && !LearnedPatterns.Contains(pattern))
        {
            LearnedPatterns.Add(pattern);
        }
    }
}

