using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using CodingAgent.Services.Ollama.Domain.ValueObjects;
using CodingAgent.Services.Ollama.Infrastructure.Http;

namespace CodingAgent.Services.Ollama.Domain.Services;

/// <summary>
/// Detects hardware capabilities for Ollama model selection
/// </summary>
public class HardwareDetector : IHardwareDetector
{
    private readonly IOllamaHttpClient _ollamaClient;
    private readonly ILogger<HardwareDetector> _logger;

    public HardwareDetector(IOllamaHttpClient ollamaClient, ILogger<HardwareDetector> logger)
    {
        _ollamaClient = ollamaClient;
        _logger = logger;
    }

    /// <summary>
    /// Detects hardware capabilities by querying system information
    /// </summary>
    public async Task<HardwareProfile> DetectHardwareAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting hardware detection...");

        try
        {
            // Try to get GPU info from Ollama backend first
            var ollamaInfo = await TryGetOllamaSystemInfoAsync(cancellationToken);
            if (ollamaInfo != null)
            {
                _logger.LogInformation(
                    "Hardware detected from Ollama: {GpuType} with {VramGB}GB VRAM, {CpuCores} CPU cores, {RamGB}GB RAM",
                    ollamaInfo.GpuType, ollamaInfo.VramGB, ollamaInfo.CpuCores, ollamaInfo.RamGB);
                return ollamaInfo;
            }

            // Fallback to system-level detection
            _logger.LogWarning("Could not get hardware info from Ollama, falling back to system detection");
            var systemProfile = await DetectSystemHardwareAsync(cancellationToken);
            
            _logger.LogInformation(
                "Hardware detected from system: {GpuType} with {VramGB}GB VRAM, {CpuCores} CPU cores, {RamGB}GB RAM",
                systemProfile.GpuType, systemProfile.VramGB, systemProfile.CpuCores, systemProfile.RamGB);

            return systemProfile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting hardware, assuming CPU-only");
            return HardwareProfile.CreateCpuOnly(
                cpuCores: Environment.ProcessorCount,
                ramGB: GetSystemRamGB());
        }
    }

    /// <summary>
    /// Determines appropriate initial models based on hardware capabilities
    /// </summary>
    public Task<List<string>> DetermineInitialModelsAsync(
        HardwareProfile hardware,
        CancellationToken cancellationToken = default)
    {
        var recommendedModels = new List<string>();

        _logger.LogInformation(
            "Determining initial models for {Tier} tier hardware ({VramGB}GB VRAM)",
            hardware.Tier, hardware.VramGB);

        switch (hardware.Tier)
        {
            case HardwareTier.High:
                // High-end GPU: 24GB+ VRAM - can run 30B+ models
                recommendedModels.AddRange(new[]
                {
                    "codellama:34b",
                    "deepseek-coder:33b",
                    "wizardcoder:34b",
                    "phind-codellama:34b"
                });
                break;

            case HardwareTier.Medium:
                // Mid-range GPU: 16-23GB VRAM - can run 13B models
                recommendedModels.AddRange(new[]
                {
                    "codellama:13b",
                    "deepseek-coder:6.7b",
                    "qwen2.5-coder:7b",
                    "starcoder2:15b"
                });
                break;

            case HardwareTier.Low:
                // Low-end GPU: 8-15GB VRAM - can run 7B models
                recommendedModels.AddRange(new[]
                {
                    "codellama:7b",
                    "deepseek-coder:6.7b",
                    "qwen2.5-coder:7b",
                    "mistral:7b"
                });
                break;

            case HardwareTier.CpuOnly:
                // CPU-only: Small quantized models
                recommendedModels.AddRange(new[]
                {
                    "codellama:7b-q4_0",      // 4-bit quantized
                    "deepseek-coder:1.3b",
                    "phi:2.7b"
                });
                break;
        }

        _logger.LogInformation(
            "Recommended {Count} models: {Models}",
            recommendedModels.Count, string.Join(", ", recommendedModels));

        return Task.FromResult(recommendedModels);
    }

    private async Task<HardwareProfile?> TryGetOllamaSystemInfoAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Try to query Ollama for system information
            // Note: Ollama doesn't have a direct system info API, so we'll use process detection
            var (gpuType, vramGB) = await DetectGpuFromOllamaProcessAsync(cancellationToken);
            
            return new HardwareProfile
            {
                GpuType = gpuType,
                VramGB = vramGB,
                CpuCores = Environment.ProcessorCount,
                RamGB = GetSystemRamGB(),
                DetectedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not detect hardware from Ollama");
            return null;
        }
    }

    private async Task<(string GpuType, double VramGB)> DetectGpuFromOllamaProcessAsync(CancellationToken cancellationToken)
    {
        // Detect NVIDIA GPU
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var (hasNvidia, nvidiaInfo) = await TryDetectNvidiaGpuAsync(cancellationToken);
            if (hasNvidia)
            {
                return nvidiaInfo;
            }

            // Check for AMD GPU
            var (hasAmd, amdInfo) = await TryDetectAmdGpuAsync(cancellationToken);
            if (hasAmd)
            {
                return amdInfo;
            }
        }

        // No GPU detected
        return ("CPU-only", 0);
    }

    private async Task<(bool HasNvidia, (string GpuType, double VramGB) Info)> TryDetectNvidiaGpuAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-gpu=name,memory.total --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return (false, ("", 0));
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                return (false, ("", 0));
            }

            // Parse output: "NVIDIA GeForce RTX 4090, 24564"
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                return (false, ("", 0));
            }

            var parts = lines[0].Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length >= 2)
            {
                var gpuName = parts[0];
                if (double.TryParse(parts[1], out var vramMB))
                {
                    var vramGB = Math.Round(vramMB / 1024.0, 1);
                    _logger.LogInformation("NVIDIA GPU detected: {GpuName} with {VramGB}GB VRAM", gpuName, vramGB);
                    return (true, (gpuName, vramGB));
                }
            }

            return (false, ("", 0));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not detect NVIDIA GPU");
            return (false, ("", 0));
        }
    }

    private async Task<(bool HasAmd, (string GpuType, double VramGB) Info)> TryDetectAmdGpuAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            // Try rocm-smi for AMD GPUs
            var startInfo = new ProcessStartInfo
            {
                FileName = "rocm-smi",
                Arguments = "--showmeminfo vram --csv",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return (false, ("", 0));
            }

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                return (false, ("", 0));
            }

            // Parse AMD GPU info (simplified)
            var match = Regex.Match(output, @"(\d+)\s*MB");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var vramMB))
            {
                var vramGB = Math.Round(vramMB / 1024.0, 1);
                _logger.LogInformation("AMD GPU detected with {VramGB}GB VRAM", vramGB);
                return (true, ("AMD Radeon", vramGB));
            }

            return (false, ("", 0));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not detect AMD GPU");
            return (false, ("", 0));
        }
    }

    private async Task<HardwareProfile> DetectSystemHardwareAsync(CancellationToken cancellationToken)
    {
        var (gpuType, vramGB) = await DetectGpuFromOllamaProcessAsync(cancellationToken);

        return new HardwareProfile
        {
            GpuType = gpuType,
            VramGB = vramGB,
            CpuCores = Environment.ProcessorCount,
            RamGB = GetSystemRamGB(),
            DetectedAt = DateTime.UtcNow
        };
    }

    private double GetSystemRamGB()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Read from /proc/meminfo
                var memInfo = File.ReadAllText("/proc/meminfo");
                var match = Regex.Match(memInfo, @"MemTotal:\s+(\d+)\s+kB");
                if (match.Success && long.TryParse(match.Groups[1].Value, out var memKB))
                {
                    return Math.Round(memKB / 1024.0 / 1024.0, 1);
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Use WMI or fallback to approximate
                // For now, return a default estimate
                return 16.0;
            }

            // Fallback estimate
            return 16.0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not detect system RAM, assuming 16GB");
            return 16.0;
        }
    }
}
