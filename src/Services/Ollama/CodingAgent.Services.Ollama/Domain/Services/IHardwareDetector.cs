using CodingAgent.Services.Ollama.Domain.ValueObjects;

namespace CodingAgent.Services.Ollama.Domain.Services;

/// <summary>
/// Detects hardware capabilities (GPU, VRAM, CPU, RAM) for model selection
/// </summary>
public interface IHardwareDetector
{
    /// <summary>
    /// Detects the hardware profile of the system
    /// </summary>
    Task<HardwareProfile> DetectHardwareAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines which models are appropriate for the detected hardware
    /// </summary>
    Task<List<string>> DetermineInitialModelsAsync(HardwareProfile hardware, CancellationToken cancellationToken = default);
}
