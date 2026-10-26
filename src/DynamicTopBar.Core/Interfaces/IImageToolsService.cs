using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.Core.Interfaces;

/// <summary>
/// Service contract for image processing, compression, conversion, transforms, and batch pipelines.
/// </summary>
public interface IImageToolsService
{
    Task<ImageProcessResult> ProcessImageAsync(
        string sourcePath,
        ImageProcessOptions options,
        CancellationToken cancellationToken = default);

    Task<ImageBatchReport> ProcessBatchAsync(
        IEnumerable<string> sourcePaths,
        ImageProcessOptions options,
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ImageMetadataInfo> InspectImageAsync(
        string sourcePath,
        CancellationToken cancellationToken = default);
}
