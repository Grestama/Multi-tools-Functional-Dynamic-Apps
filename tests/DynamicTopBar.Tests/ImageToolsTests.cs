using System;
using System.Collections.Generic;
using DynamicTopBar.Core.Models;
using Xunit;

namespace DynamicTopBar.Tests;

public class ImageToolsTests
{
    [Fact]
    public void ImageProcessOptions_ValidOptions_PassesValidation()
    {
        var options = new ImageProcessOptions
        {
            QualityPercent = 85,
            TargetWidth = 800,
            TargetHeight = 600,
            TargetFormat = ImageFormatTarget.WebP,
            MaintainAspectRatio = true,
            Rotation = ImageRotationAngle.Rotate90,
            Flip = ImageFlipMode.Horizontal
        };

        options.Validate(); // Should not throw
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(-5)]
    public void ImageProcessOptions_InvalidQuality_ThrowsArgumentOutOfRangeException(int quality)
    {
        var options = new ImageProcessOptions { QualityPercent = quality };
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void ImageProcessOptions_ZeroDimensions_ThrowsArgumentException()
    {
        var optionsW = new ImageProcessOptions { TargetWidth = 0 };
        Assert.Throws<ArgumentException>(() => optionsW.Validate());

        var optionsH = new ImageProcessOptions { TargetHeight = 0 };
        Assert.Throws<ArgumentException>(() => optionsH.Validate());

        var optionsCrop = new ImageProcessOptions
        {
            CropBounds = new ImageCropBounds(0, 0, 0, 100)
        };
        Assert.Throws<ArgumentException>(() => optionsCrop.Validate());
    }

    [Fact]
    public void ImageProcessResult_CalculatesSavingsCorrectly()
    {
        var result = new ImageProcessResult
        {
            Success = true,
            OriginalSizeBytes = 1000000, // 1MB
            ProcessedSizeBytes = 250000  // 250KB
        };

        Assert.Equal(750000, result.SavedBytes);
        Assert.Equal(75.0, result.SavingsPercentage);
    }

    [Fact]
    public void ImageProcessResult_WhenOutputLarger_ReportsZeroSavings()
    {
        // Example: PNG re-encoding that is larger than source
        var result = new ImageProcessResult
        {
            Success = true,
            OriginalSizeBytes = 50000,
            ProcessedSizeBytes = 60000
        };

        Assert.Equal(0, result.SavedBytes);
        Assert.Equal(-20.0, result.SavingsPercentage);
    }

    [Fact]
    public void ImageBatchReport_AggregatesTotalsAccurately()
    {
        var items = new List<ImageProcessResult>
        {
            new() { Success = true, OriginalSizeBytes = 500000, ProcessedSizeBytes = 200000 },
            new() { Success = true, OriginalSizeBytes = 500000, ProcessedSizeBytes = 300000 },
            new() { Success = false, OriginalSizeBytes = 200000, ProcessedSizeBytes = 0 }
        };

        var report = new ImageBatchReport
        {
            TotalCount = 3,
            SuccessCount = 2,
            FailedCount = 1,
            TotalOriginalBytes = 1000000,
            TotalProcessedBytes = 500000,
            Results = items
        };

        Assert.Equal(3, report.TotalCount);
        Assert.Equal(2, report.SuccessCount);
        Assert.Equal(1, report.FailedCount);
        Assert.Equal(500000, report.TotalSavedBytes);
        Assert.Equal(50.0, report.OverallSavingsPercentage);
    }
}
