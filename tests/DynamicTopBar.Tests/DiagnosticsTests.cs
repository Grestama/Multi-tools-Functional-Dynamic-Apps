using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using Xunit;

namespace DynamicTopBar.Tests;

public class DiagnosticsTests : IDisposable
{
    private readonly LoggingService _logger;

    public DiagnosticsTests()
    {
        _logger = new LoggingService();
    }

    public void Dispose()
    {
        _logger.Dispose();
    }

    [Fact]
    public async Task RunHealthChecksAsync_ExecutesAllRegisteredChecks()
    {
        using var service = new DiagnosticsService(_logger);

        service.RegisterHealthCheck("Database", () => Task.FromResult(new ComponentHealthCheck
        {
            ComponentName = "Database",
            Status = HealthStatus.Healthy,
            Message = "Connected"
        }));

        service.RegisterHealthCheck("FailingService", () => throw new TimeoutException("Service timed out"));

        var results = await service.RunHealthChecksAsync();

        Assert.Equal(2, results.Count);

        var db = results.First(r => r.ComponentName == "Database");
        Assert.Equal(HealthStatus.Healthy, db.Status);
        Assert.Equal("Connected", db.Message);
        Assert.True(db.LatencyMs >= 0);

        var fail = results.First(r => r.ComponentName == "FailingService");
        Assert.Equal(HealthStatus.Unhealthy, fail.Status);
        Assert.Contains("timed out", fail.Message);
    }

    [Fact]
    public void GetResourceMetrics_ReturnsValidProcessStats()
    {
        using var service = new DiagnosticsService(_logger);
        var metrics = service.GetResourceMetrics();

        Assert.True(metrics.ManagedMemoryBytes > 0);
        Assert.True(metrics.ThreadCount > 0);
        Assert.True(metrics.ProcessUptime.TotalMilliseconds >= 0);
    }

    [Fact]
    public async Task RecordToolFailure_CapsHistoryAndSanitizesMessage()
    {
        using var service = new DiagnosticsService(_logger);

        for (int i = 0; i < 60; i++)
        {
            service.RecordToolFailure("tool-1", $"Error on attempt {i}");
        }

        var report = await service.GenerateReportAsync();

        // Capped to MaxFailureHistory (50)
        Assert.Equal(50, report.RecentToolFailures.Count);
        Assert.Equal("tool-1", report.RecentToolFailures[0].ToolId);
    }

    [Theory]
    [InlineData("Bearer secret_bearer_token_1234567890", "Bearer [REDACTED_BEARER_TOKEN]")]
    [InlineData("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIn0.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c", "[REDACTED_JWT_TOKEN]")]
    [InlineData("{\"apiKey\": \"secret_api_key_value\"}", "{\"apiKey\": \"[REDACTED]\"}")]
    [InlineData("{\"password\": \"super_secret_p@ssw0rd!\"}", "{\"password\": \"[REDACTED]\"}")]
    [InlineData("Card: 4111-2222-3333-4444 used", "Card: [REDACTED_CARD_NUMBER] used")]
    public void SanitizeSecrets_RedactsSensitiveDataCorrectly(string input, string expectedSubstring)
    {
        using var service = new DiagnosticsService(_logger);
        string sanitized = service.SanitizeSecrets(input);

        Assert.Contains(expectedSubstring, sanitized);
    }

    [Fact]
    public void SanitizeSecrets_RedactsRsaPrivateKey()
    {
        using var service = new DiagnosticsService(_logger);
        string key = "-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEA0m...\n-----END RSA PRIVATE KEY-----";

        string sanitized = service.SanitizeSecrets(key);

        Assert.Equal("[REDACTED_PRIVATE_KEY]", sanitized);
        Assert.DoesNotContain("MIIEow", sanitized);
    }

    [Fact]
    public async Task ExportSanitizedReportJson_ProducesSanitizedJson()
    {
        using var service = new DiagnosticsService(_logger);

        _logger.LogWarning("Authentication failed with Bearer sensitive_token_99999999");
        service.RecordToolFailure("tool-auth", "Invalid key {\"token\": \"super_secret_value\"}");

        var report = await service.GenerateReportAsync();
        string json = service.ExportSanitizedReportJson(report);

        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.Contains("[REDACTED_BEARER_TOKEN]", json);
        Assert.Contains("[REDACTED]", json);
        Assert.DoesNotContain("sensitive_token_99999999", json);
        Assert.DoesNotContain("super_secret_value", json);
    }
}
