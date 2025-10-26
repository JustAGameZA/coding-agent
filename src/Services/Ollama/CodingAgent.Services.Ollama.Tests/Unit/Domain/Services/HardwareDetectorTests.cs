using CodingAgent.Services.Ollama.Domain.Services;
using CodingAgent.Services.Ollama.Domain.ValueObjects;
using CodingAgent.Services.Ollama.Infrastructure.Http;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CodingAgent.Services.Ollama.Tests.Unit.Domain.Services;

[Trait("Category", "Unit")]
public class HardwareDetectorTests
{
    private readonly Mock<IOllamaHttpClient> _ollamaClientMock;
    private readonly Mock<ILogger<HardwareDetector>> _loggerMock;
    private readonly HardwareDetector _sut;

    public HardwareDetectorTests()
    {
        _ollamaClientMock = new Mock<IOllamaHttpClient>();
        _loggerMock = new Mock<ILogger<HardwareDetector>>();
        _sut = new HardwareDetector(_ollamaClientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task DetectHardwareAsync_WhenOllamaUnavailable_ShouldReturnCpuOnlyProfile()
    {
        // Arrange
        _ollamaClientMock
            .Setup(x => x.IsHealthyAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.DetectHardwareAsync();

        // Assert
        result.Should().NotBeNull();
        result.GpuType.Should().Be("CPU-only");
        result.VramGB.Should().Be(0);
        result.HasGpu.Should().BeFalse();
        result.Tier.Should().Be(HardwareTier.CpuOnly);
        result.CpuCores.Should().BeGreaterThan(0);
        result.DetectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DetermineInitialModelsAsync_ForHighTierHardware_ShouldRecommend34BModels()
    {
        // Arrange
        var hardware = new HardwareProfile
        {
            GpuType = "NVIDIA RTX 4090",
            VramGB = 24,
            CpuCores = 16,
            RamGB = 64
        };

        // Act
        var models = await _sut.DetermineInitialModelsAsync(hardware);

        // Assert
        models.Should().NotBeEmpty();
        models.Should().Contain("codellama:34b");
        models.Should().Contain("deepseek-coder:33b");
        models.Should().Contain("wizardcoder:34b");
        models.Should().Contain("phind-codellama:34b");
        models.Count.Should().Be(4);
    }

    [Fact]
    public async Task DetermineInitialModelsAsync_ForMediumTierHardware_ShouldRecommend13BModels()
    {
        // Arrange
        var hardware = new HardwareProfile
        {
            GpuType = "NVIDIA RTX 3090",
            VramGB = 16,
            CpuCores = 12,
            RamGB = 32
        };

        // Act
        var models = await _sut.DetermineInitialModelsAsync(hardware);

        // Assert
        models.Should().NotBeEmpty();
        models.Should().Contain("codellama:13b");
        models.Should().Contain("deepseek-coder:6.7b");
        models.Should().Contain("qwen2.5-coder:7b");
        models.Should().Contain("starcoder2:15b");
        models.Count.Should().Be(4);
    }

    [Fact]
    public async Task DetermineInitialModelsAsync_ForLowTierHardware_ShouldRecommend7BModels()
    {
        // Arrange
        var hardware = new HardwareProfile
        {
            GpuType = "NVIDIA GTX 1080",
            VramGB = 8,
            CpuCores = 8,
            RamGB = 16
        };

        // Act
        var models = await _sut.DetermineInitialModelsAsync(hardware);

        // Assert
        models.Should().NotBeEmpty();
        models.Should().Contain("codellama:7b");
        models.Should().Contain("deepseek-coder:6.7b");
        models.Should().Contain("qwen2.5-coder:7b");
        models.Should().Contain("mistral:7b");
        models.Count.Should().Be(4);
    }

    [Fact]
    public async Task DetermineInitialModelsAsync_ForCpuOnlyHardware_ShouldRecommendQuantizedModels()
    {
        // Arrange
        var hardware = HardwareProfile.CreateCpuOnly(cpuCores: 8, ramGB: 16);

        // Act
        var models = await _sut.DetermineInitialModelsAsync(hardware);

        // Assert
        models.Should().NotBeEmpty();
        models.Should().Contain("codellama:7b-q4_0");
        models.Should().Contain("deepseek-coder:1.3b");
        models.Should().Contain("phi:2.7b");
        models.Count.Should().Be(3);
    }

    [Theory]
    [InlineData(24, HardwareTier.High)]
    [InlineData(30, HardwareTier.High)]
    [InlineData(16, HardwareTier.Medium)]
    [InlineData(20, HardwareTier.Medium)]
    [InlineData(8, HardwareTier.Low)]
    [InlineData(12, HardwareTier.Low)]
    [InlineData(4, HardwareTier.CpuOnly)]
    [InlineData(0, HardwareTier.CpuOnly)]
    public void HardwareProfile_Tier_ShouldBeCorrectlyCalculated(double vramGB, HardwareTier expectedTier)
    {
        // Arrange & Act
        var hardware = new HardwareProfile
        {
            GpuType = "Test GPU",
            VramGB = vramGB,
            CpuCores = 8,
            RamGB = 16
        };

        // Assert
        hardware.Tier.Should().Be(expectedTier);
    }

    [Fact]
    public void HardwareProfile_HasGpu_ShouldBeTrueWhenVramGreaterThanZero()
    {
        // Arrange & Act
        var hardware = new HardwareProfile
        {
            GpuType = "NVIDIA RTX 4090",
            VramGB = 24,
            CpuCores = 16,
            RamGB = 64
        };

        // Assert
        hardware.HasGpu.Should().BeTrue();
    }

    [Fact]
    public void HardwareProfile_HasGpu_ShouldBeFalseForCpuOnly()
    {
        // Arrange & Act
        var hardware = HardwareProfile.CreateCpuOnly(cpuCores: 8, ramGB: 16);

        // Assert
        hardware.HasGpu.Should().BeFalse();
        hardware.GpuType.Should().Be("CPU-only");
        hardware.VramGB.Should().Be(0);
    }

    [Fact]
    public void HardwareProfile_ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var hardware = new HardwareProfile
        {
            GpuType = "NVIDIA RTX 4090",
            VramGB = 24,
            CpuCores = 16,
            RamGB = 64
        };

        // Act
        var result = hardware.ToString();

        // Assert
        result.Should().Contain("NVIDIA RTX 4090");
        result.Should().Contain("24GB");
        result.Should().Contain("16");
        result.Should().Contain("64GB");
        result.Should().Contain("High");
    }

    [Fact]
    public async Task DetermineInitialModelsAsync_ShouldAlwaysReturnNonEmptyList()
    {
        // Arrange - Test all tiers
        var testCases = new[]
        {
            new HardwareProfile { VramGB = 24, CpuCores = 16, RamGB = 64, GpuType = "High-end" },
            new HardwareProfile { VramGB = 16, CpuCores = 12, RamGB = 32, GpuType = "Mid-range" },
            new HardwareProfile { VramGB = 8, CpuCores = 8, RamGB = 16, GpuType = "Low-end" },
            HardwareProfile.CreateCpuOnly(cpuCores: 8, ramGB: 16)
        };

        foreach (var hardware in testCases)
        {
            // Act
            var models = await _sut.DetermineInitialModelsAsync(hardware);

            // Assert
            models.Should().NotBeEmpty($"Hardware tier {hardware.Tier} should have model recommendations");
            models.Should().OnlyContain(m => !string.IsNullOrWhiteSpace(m), "All model names should be valid");
        }
    }

    [Fact]
    public async Task DetectHardwareAsync_ShouldLogDetectionResults()
    {
        // Act
        await _sut.DetectHardwareAsync();

        // Assert - Verify logging occurred
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting hardware detection")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task DetermineInitialModelsAsync_ShouldLogRecommendations()
    {
        // Arrange
        var hardware = new HardwareProfile
        {
            GpuType = "NVIDIA RTX 4090",
            VramGB = 24,
            CpuCores = 16,
            RamGB = 64
        };

        // Act
        await _sut.DetermineInitialModelsAsync(hardware);

        // Assert - Verify logging occurred
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Determining initial models")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Recommended")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
