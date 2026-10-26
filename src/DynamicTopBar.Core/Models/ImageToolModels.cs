using System;
using System.Collections.Generic;

namespace DynamicTopBar.Core.Models;

public enum ImageFormatTarget
{
    Jpeg,
    Png,
    WebP
}

public enum ImageRotationAngle
{
    None = 0,
    Rotate90 = 90,
    Rotate180 = 180,
    Rotate270 = 270
}

public enum ImageFlipMode
{
    None = 0,
    Horizontal = 1,
    Vertical = 2
}

public record ImageCropBounds(uint X, uint Y, uint Width, uint Height);

/// <summary>
/// Configurable options controlling image compression, resize, format conversion, and transforms.
/// </summary>
public record ImageProcessOptions
{
    public ImageFormatTarget TargetFormat { get; init; } = ImageFormatTarget.Jpeg;
    public int QualityPercent { get; init; } = 80;
    public uint? TargetWidth { get; init; }
    public uint? TargetHeight { get; init; }
    public bool MaintainAspectRatio { get; init; } = true;
    public ImageCropBounds? CropBounds { get; init; }
    public ImageRotationAngle Rotation { get; init; } = ImageRotationAngle.None;
    public ImageFlipMode Flip { get; init; } = ImageFlipMode.None;
    public bool StripMetadata { get; init; } = true;
    public string? OutputDirectory { get; init; }
    public string OutputFileNameSuffix { get; init; } = "_processed";
    public bool OverwriteExisting { get; init; } = false;

    public void Validate()
    {
        if (QualityPercent is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(QualityPercent), "Quality must be between 1 and 100 percent.");
        }

        if (TargetWidth is 0)
        {
            throw new ArgumentException("Target width cannot be zero.", nameof(TargetWidth));
        }

        if (TargetHeight is 0)
        {
            throw new ArgumentException("Target height cannot be zero.", nameof(TargetHeight));
        }

        if (CropBounds != null && (CropBounds.Width == 0 || CropBounds.Height == 0))
        {
            throw new ArgumentException("Crop dimensions must be greater than zero.", nameof(CropBounds));
        }
    }
}

/// <summary>
/// Result of a single image transformation operation.
/// </summary>
public record ImageProcessResult
{
    public bool Success { get; init; }
    public string SourceFilePath { get; init; } = string.Empty;
    public string? OutputFilePath { get; init; }
    public long OriginalSizeBytes { get; init; }
    public long ProcessedSizeBytes { get; init; }
    public long SavedBytes => Math.Max(0, OriginalSizeBytes - ProcessedSizeBytes);
    public double SavingsPercentage => OriginalSizeBytes > 0 ? Math.Round((double)(OriginalSizeBytes - ProcessedSizeBytes) / OriginalSizeBytes * 100.0, 2) : 0.0;
    public uint OriginalWidth { get; init; }
    public uint OriginalHeight { get; init; }
    public uint ProcessedWidth { get; init; }
    public uint ProcessedHeight { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Aggregate summary report for batch image operations.
/// </summary>
public record ImageBatchReport
{
    public int TotalCount { get; init; }
    public int SuccessCount { get; init; }
    public int FailedCount { get; init; }
    public long TotalOriginalBytes { get; init; }
    public long TotalProcessedBytes { get; init; }
    public long TotalSavedBytes => Math.Max(0, TotalOriginalBytes - TotalProcessedBytes);
    public double OverallSavingsPercentage => TotalOriginalBytes > 0 ? Math.Round((double)(TotalOriginalBytes - TotalProcessedBytes) / TotalOriginalBytes * 100.0, 2) : 0.0;
    public IReadOnlyList<ImageProcessResult> Results { get; init; } = Array.Empty<ImageProcessResult>();
}

/// <summary>
/// Basic metadata inspected from an image file.
/// </summary>
public record ImageMetadataInfo
{
    public string Format { get; init; } = string.Empty;
    public uint Width { get; init; }
    public uint Height { get; init; }
    public long FileSizeBytes { get; init; }
    public bool HasAlphaChannel { get; init; }
}
