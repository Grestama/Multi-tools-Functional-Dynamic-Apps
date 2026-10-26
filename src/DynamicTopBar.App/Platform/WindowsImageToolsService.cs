using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.App.Platform;

/// <summary>
/// Hardware-accelerated Windows Imaging Component (WIC) implementation of IImageToolsService.
/// Supports JPEG, PNG, and WebP compression, transforms, cropping, and batch processing.
/// </summary>
public sealed class WindowsImageToolsService : IImageToolsService
{
    // Well-known WIC encoder GUID for WebP (available on Windows 10/11 with WebP Image Extension)
    private static readonly Guid WebpEncoderId = new("808077e5-0321-4e46-8094-5bc77192c2ff");

    public async Task<ImageProcessResult> ProcessImageAsync(
        string sourcePath,
        ImageProcessOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath))
        {
            return new ImageProcessResult
            {
                Success = false,
                SourceFilePath = sourcePath,
                ErrorMessage = $"Source image file does not exist: {sourcePath}"
            };
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var originalFileInfo = new FileInfo(sourcePath);
            long originalSizeBytes = originalFileInfo.Length;

            using var inputStream = File.OpenRead(sourcePath);
            using var inputRandomStream = inputStream.AsRandomAccessStream();

            var decoder = await BitmapDecoder.CreateAsync(inputRandomStream);
            uint origW = decoder.PixelWidth;
            uint origH = decoder.PixelHeight;

            // Prepare transforms (scaling, crop, rotate, flip)
            var transform = new BitmapTransform();
            uint finalW = origW;
            uint finalH = origH;

            if (options.TargetWidth.HasValue || options.TargetHeight.HasValue)
            {
                uint reqW = options.TargetWidth ?? origW;
                uint reqH = options.TargetHeight ?? origH;

                if (options.MaintainAspectRatio)
                {
                    if (options.TargetWidth.HasValue && !options.TargetHeight.HasValue)
                    {
                        finalW = reqW;
                        finalH = (uint)Math.Max(1, Math.Round((double)origH * reqW / origW));
                    }
                    else if (!options.TargetWidth.HasValue && options.TargetHeight.HasValue)
                    {
                        finalH = reqH;
                        finalW = (uint)Math.Max(1, Math.Round((double)origW * reqH / origH));
                    }
                    else
                    {
                        double ratio = Math.Min((double)reqW / origW, (double)reqH / origH);
                        finalW = (uint)Math.Max(1, Math.Round(origW * ratio));
                        finalH = (uint)Math.Max(1, Math.Round(origH * ratio));
                    }
                }
                else
                {
                    finalW = reqW;
                    finalH = reqH;
                }

                transform.ScaledWidth = finalW;
                transform.ScaledHeight = finalH;
                transform.InterpolationMode = BitmapInterpolationMode.Cubic;
            }

            if (options.CropBounds != null)
            {
                transform.Bounds = new BitmapBounds
                {
                    X = options.CropBounds.X,
                    Y = options.CropBounds.Y,
                    Width = Math.Min(options.CropBounds.Width, finalW - options.CropBounds.X),
                    Height = Math.Min(options.CropBounds.Height, finalH - options.CropBounds.Y)
                };
                finalW = transform.Bounds.Width;
                finalH = transform.Bounds.Height;
            }

            if (options.Rotation != ImageRotationAngle.None)
            {
                transform.Rotation = options.Rotation switch
                {
                    ImageRotationAngle.Rotate90 => BitmapRotation.Clockwise90Degrees,
                    ImageRotationAngle.Rotate180 => BitmapRotation.Clockwise180Degrees,
                    ImageRotationAngle.Rotate270 => BitmapRotation.Clockwise270Degrees,
                    _ => BitmapRotation.None
                };

                if (options.Rotation is ImageRotationAngle.Rotate90 or ImageRotationAngle.Rotate270)
                {
                    (finalW, finalH) = (finalH, finalW);
                }
            }

            if (options.Flip != ImageFlipMode.None)
            {
                transform.Flip = options.Flip switch
                {
                    ImageFlipMode.Horizontal => BitmapFlip.Horizontal,
                    ImageFlipMode.Vertical => BitmapFlip.Vertical,
                    _ => BitmapFlip.None
                };
            }

            // Extract pixels with transforms applied
            var pixelData = await decoder.GetPixelDataAsync(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                transform,
                ExifOrientationMode.RespectExifOrientation,
                ColorManagementMode.ColorManageToSRgb);

            byte[] pixels = pixelData.DetachPixelData();

            // Destination directory & file name calculation
            string destDir = string.IsNullOrWhiteSpace(options.OutputDirectory)
                ? Path.GetDirectoryName(sourcePath) ?? Environment.CurrentDirectory
                : options.OutputDirectory;

            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            string ext = options.TargetFormat switch
            {
                ImageFormatTarget.Png => ".png",
                ImageFormatTarget.WebP => ".webp",
                _ => ".jpg"
            };

            string baseName = Path.GetFileNameWithoutExtension(sourcePath);
            string outputFileName;

            if (options.OverwriteExisting && string.IsNullOrWhiteSpace(options.OutputDirectory))
            {
                outputFileName = baseName + ext;
            }
            else
            {
                outputFileName = $"{baseName}{options.OutputFileNameSuffix}{ext}";
            }

            string outputPath = Path.Combine(destDir, outputFileName);

            // If file already exists and overwrite is disabled, append numeric discriminator
            if (File.Exists(outputPath) && !options.OverwriteExisting)
            {
                int counter = 1;
                while (File.Exists(outputPath))
                {
                    outputPath = Path.Combine(destDir, $"{baseName}{options.OutputFileNameSuffix}_{counter}{ext}");
                    counter++;
                }
            }

            // Prepare encoder properties
            Guid encoderId = options.TargetFormat switch
            {
                ImageFormatTarget.Png => BitmapEncoder.PngEncoderId,
                ImageFormatTarget.WebP => WebpEncoderId,
                _ => BitmapEncoder.JpegEncoderId
            };

            var propertySet = new BitmapPropertySet();
            if (options.TargetFormat is ImageFormatTarget.Jpeg or ImageFormatTarget.WebP)
            {
                // Quality from 0.0 to 1.0
                float quality = Math.Clamp(options.QualityPercent / 100.0f, 0.01f, 1.0f);
                propertySet.Add("ImageQuality", new BitmapTypedValue(quality, Windows.Foundation.PropertyType.Single));
            }

            cancellationToken.ThrowIfCancellationRequested();

            using (var outStream = File.Create(outputPath))
            {
                using var outRandomStream = outStream.AsRandomAccessStream();
                BitmapEncoder encoder;
                try
                {
                    encoder = await BitmapEncoder.CreateAsync(encoderId, outRandomStream, propertySet);
                }
                catch
                {
                    // If WebP is not supported on this specific system WIC, fallback gracefully to JPEG
                    encoderId = BitmapEncoder.JpegEncoderId;
                    encoder = await BitmapEncoder.CreateAsync(encoderId, outRandomStream, propertySet);
                }

                encoder.SetPixelData(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    finalW,
                    finalH,
                    decoder.DpiX,
                    decoder.DpiY,
                    pixels);

                await encoder.FlushAsync();
            }

            long processedSizeBytes = new FileInfo(outputPath).Length;

            return new ImageProcessResult
            {
                Success = true,
                SourceFilePath = sourcePath,
                OutputFilePath = outputPath,
                OriginalSizeBytes = originalSizeBytes,
                ProcessedSizeBytes = processedSizeBytes,
                OriginalWidth = origW,
                OriginalHeight = origH,
                ProcessedWidth = finalW,
                ProcessedHeight = finalH
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ImageProcessResult
            {
                Success = false,
                SourceFilePath = sourcePath,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<ImageBatchReport> ProcessBatchAsync(
        IEnumerable<string> sourcePaths,
        ImageProcessOptions options,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);
        ArgumentNullException.ThrowIfNull(options);

        var list = sourcePaths.ToList();
        var results = new List<ImageProcessResult>();
        int completed = 0;

        foreach (var path in list)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await ProcessImageAsync(path, options, cancellationToken);
            results.Add(result);

            completed++;
            progress?.Report((int)Math.Round((double)completed / list.Count * 100));
        }

        return new ImageBatchReport
        {
            TotalCount = list.Count,
            SuccessCount = results.Count(r => r.Success),
            FailedCount = results.Count(r => !r.Success),
            TotalOriginalBytes = results.Where(r => r.Success).Sum(r => r.OriginalSizeBytes),
            TotalProcessedBytes = results.Where(r => r.Success).Sum(r => r.ProcessedSizeBytes),
            Results = results
        };
    }

    public async Task<ImageMetadataInfo> InspectImageAsync(
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException("Image file not found.", sourcePath);
        }

        var fi = new FileInfo(sourcePath);
        using var stream = File.OpenRead(sourcePath);
        using var randomStream = stream.AsRandomAccessStream();

        var decoder = await BitmapDecoder.CreateAsync(randomStream);

        return new ImageMetadataInfo
        {
            Format = decoder.DecoderInformation.CodecId.ToString(),
            Width = decoder.PixelWidth,
            Height = decoder.PixelHeight,
            FileSizeBytes = fi.Length,
            HasAlphaChannel = decoder.BitmapAlphaMode != BitmapAlphaMode.Ignore
        };
    }
}
