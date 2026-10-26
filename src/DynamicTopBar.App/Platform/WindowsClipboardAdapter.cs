using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using DynamicTopBar.Core.Interfaces;

namespace DynamicTopBar.App.Platform;

/// <summary>
/// Platform adapter connecting Core clipboard services to Windows.ApplicationModel.DataTransfer.Clipboard.
/// </summary>
public sealed class WindowsClipboardAdapter : IClipboardPlatformAdapter
{
    public async Task<string?> GetTextAsync()
    {
        try
        {
            var dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                return await dataPackageView.GetTextAsync();
            }
        }
        catch
        {
            // Clipboard may be locked by another application
        }

        return null;
    }

    public Task SetTextAsync(string text)
    {
        if (text == null) return Task.CompletedTask;

        try
        {
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
            Clipboard.Flush();
        }
        catch
        {
            // Ignore clipboard set exceptions
        }

        return Task.CompletedTask;
    }
}
