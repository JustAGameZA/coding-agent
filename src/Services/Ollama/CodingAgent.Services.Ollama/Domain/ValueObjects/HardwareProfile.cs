namespace CodingAgent.Services.Ollama.Domain.ValueObjects;

/// <summary>
/// Represents the hardware capabilities detected on the system.
/// Used to determine which models can be loaded and run efficiently.
/// </summary>
public class HardwareProfile
{
    /// <summary>
    /// Type of GPU detected (e.g., "NVIDIA GeForce RTX 4090", "AMD Radeon RX 7900", "CPU-only")
    /// </summary>
    public string GpuType { get; init; } = "CPU-only";

    /// <summary>
    /// Available VRAM in GB. 0 for CPU-only systems.
    /// </summary>
    public double VramGB { get; init; }

    /// <summary>
    /// Number of CPU cores available
    /// </summary>
    public int CpuCores { get; init; }

    /// <summary>
    /// Total system RAM in GB
    /// </summary>
    public double RamGB { get; init; }

    /// <summary>
    /// When the hardware was detected
    /// </summary>
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether GPU acceleration is available
    /// </summary>
    public bool HasGpu => VramGB > 0 && GpuType != "CPU-only";

    /// <summary>
    /// Hardware tier based on VRAM capacity
    /// </summary>
    public HardwareTier Tier => VramGB switch
    {
        >= 24 => HardwareTier.High,
        >= 16 => HardwareTier.Medium,
        >= 8 => HardwareTier.Low,
        _ => HardwareTier.CpuOnly
    };

    /// <summary>
    /// Creates a CPU-only hardware profile
    /// </summary>
    public static HardwareProfile CreateCpuOnly(int cpuCores, double ramGB)
    {
        return new HardwareProfile
        {
            GpuType = "CPU-only",
            VramGB = 0,
            CpuCores = cpuCores,
            RamGB = ramGB,
            DetectedAt = DateTime.UtcNow
        };
    }

    public override string ToString()
    {
        return $"GPU: {GpuType}, VRAM: {VramGB}GB, CPU Cores: {CpuCores}, RAM: {RamGB}GB, Tier: {Tier}";
    }
}

/// <summary>
/// Hardware capability tiers
/// </summary>
public enum HardwareTier
{
    /// <summary>
    /// CPU-only, no GPU acceleration
    /// </summary>
    CpuOnly = 0,

    /// <summary>
    /// Low-end GPU (8-15GB VRAM) - can run 7B models
    /// </summary>
    Low = 1,

    /// <summary>
    /// Mid-range GPU (16-23GB VRAM) - can run 13B models
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High-end GPU (24GB+ VRAM) - can run 30B+ models
    /// </summary>
    High = 3
}
