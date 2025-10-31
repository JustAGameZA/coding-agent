namespace CodingAgent.Services.Memory.Domain.Entities;

/// <summary>
/// Represents an association between related memories
/// </summary>
public class MemoryAssociation
{
    public Guid Id { get; private set; }
    public Guid SourceMemoryId { get; private set; }
    public string SourceMemoryType { get; private set; } = string.Empty; // 'episode', 'semantic', 'procedure'
    public Guid TargetMemoryId { get; private set; }
    public string TargetMemoryType { get; private set; } = string.Empty;
    public string AssociationType { get; private set; } = string.Empty; // 'similar', 'used_together', 'caused'
    public float Strength { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private MemoryAssociation() { }

    public MemoryAssociation(
        Guid sourceMemoryId,
        string sourceMemoryType,
        Guid targetMemoryId,
        string targetMemoryType,
        string associationType,
        float strength = 1.0f)
    {
        Id = Guid.NewGuid();
        SourceMemoryId = sourceMemoryId;
        SourceMemoryType = sourceMemoryType;
        TargetMemoryId = targetMemoryId;
        TargetMemoryType = targetMemoryType;
        AssociationType = associationType;
        Strength = Math.Clamp(strength, 0.0f, 1.0f);
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStrength(float newStrength)
    {
        Strength = Math.Clamp(newStrength, 0.0f, 1.0f);
    }
}

