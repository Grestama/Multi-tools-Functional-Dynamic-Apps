using System;
using System.IO;
using System.Threading;
using Microsoft.UI.Xaml;
using DynamicTopBar.App.Platform;
using DynamicTopBar.App.UI;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Services;

namespace DynamicTopBar.App;

/// <summary>
/// Main application lifetime and single-instance manager.
/// </summary>
public partial class App : Application
{
    private const string MutexName = "DynamicTopBar.SingleInstance.Mutex.7B0F33A9";
    private static Mutex? _singleInstanceMutex;

    private readonly ILoggingService _logger;
    private TopBarWindow? _window;
    private TrayIcon? _trayIcon;
    private WindowsHotKeyAdapter? _hotKeyAdapter;

    public App()
    {
        _logger = new LoggingService();
        this.UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        InitializeComponent();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        _logger.LogError($"[XamlUnhandledException] {e.Message}", e.Exception, "Lifecycle");
    }

    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
    {
        _logger.LogError($"[AppDomainUnhandledException] {e.ExceptionObject}", null, "Lifecycle");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            // Enforce single instance policy
            _singleInstanceMutex = new Mutex(true, MutexName, out bool isInitialInstance);
            if (!isInitialInstance)
            {
                // Signal the running instance to show overlay
                _logger.LogInfo("Secondary instance launched. Broadcasting show signal and exiting.", "Lifecycle");
                Win32Interop.PostMessage(Win32Interop.HWND_BROADCAST, TrayIcon.WmShowDynamicTopBar, IntPtr.Zero, IntPtr.Zero);

                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                Environment.Exit(0);
                return;
            }

            _logger.LogInfo("Primary instance starting.", "Lifecycle");

            // Create overlay window
            _window = new TopBarWindow();

            // Resolve application icon path
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");

            // Initialize system tray icon
            _trayIcon = new TrayIcon(_window.Hwnd, iconPath);
            _trayIcon.IsOverlayVisible = true;

            // Connect global shortcut adapter to native message loop
            _hotKeyAdapter = new WindowsHotKeyAdapter(_window.Hwnd, _window.ShortcutService, _trayIcon);

            _trayIcon.ToggleVisibilityRequested += OnToggleVisibilityRequested;
            _trayIcon.ShowOverlayRequested += OnShowOverlayRequested;
            _trayIcon.CommandPaletteRequested += OnCommandPaletteRequested;
            _trayIcon.SettingsRequested += OnSettingsRequested;
            _trayIcon.SystemTimeChanged += OnSystemTimeChanged;
            _trayIcon.SystemResumed += OnSystemResumed;
            _trayIcon.QuitRequested += OnQuitRequested;

            // Display the overlay without stealing keyboard focus
            _window.ShowOverlay();
            _logger.LogInfo("Primary instance overlay displayed successfully.", "Lifecycle");
        }
        catch (Exception ex)
        {
            _logger.LogError("Exception in OnLaunched", ex, "Lifecycle");
            throw;
        }
    }

    private void OnShowOverlayRequested()
    {
        if (_window == null || _trayIcon == null) return;

        _window.DispatcherQueue.TryEnqueue(() =>
        {
            _window.ShowOverlay();
            _trayIcon.IsOverlayVisible = true;
        });
    }

    private void OnCommandPaletteRequested()
    {
        if (_window == null) return;
        _window.DispatcherQueue.TryEnqueue(() =>
        {
            _window.OpenCommandPaletteWindow();
        });
    }

    private void OnSettingsRequested()
    {
        if (_window == null) return;
        _window.DispatcherQueue.TryEnqueue(() =>
        {
            _window.OpenSettingsWindow();
        });
    }

    private void OnSystemTimeChanged()
    {
        _logger.LogInfo("System time change detected via WM_TIMECHANGE.", "SystemEvents");
        _window?.HandleSystemTimeChanged();
    }

    private void OnSystemResumed()
    {
        _logger.LogInfo("System resume from sleep detected via WM_POWERBROADCAST.", "SystemEvents");
        _window?.HandleSystemResumed();
    }

    private void OnToggleVisibilityRequested()
    {
        if (_window == null || _trayIcon == null)
        {
            return;
        }

        if (_window.IsOverlayVisible)
        {
            _window.HideOverlay();
            _trayIcon.IsOverlayVisible = false;
        }
        else
        {
            _window.ShowOverlay();
            _trayIcon.IsOverlayVisible = true;
        }
    }

    private void OnQuitRequested()
    {
        _logger.LogInfo("Quit requested from System Tray. Shutting down application.", "Lifecycle");
        ShutdownApplication();
    }

    private void ShutdownApplication()
    {
        if (_hotKeyAdapter != null)
        {
            _hotKeyAdapter.Dispose();
            _hotKeyAdapter = null;
        }

        if (_trayIcon != null)
        {
            _trayIcon.ToggleVisibilityRequested -= OnToggleVisibilityRequested;
            _trayIcon.ShowOverlayRequested -= OnShowOverlayRequested;
            _trayIcon.CommandPaletteRequested -= OnCommandPaletteRequested;
            _trayIcon.SettingsRequested -= OnSettingsRequested;
            _trayIcon.SystemTimeChanged -= OnSystemTimeChanged;
            _trayIcon.SystemResumed -= OnSystemResumed;
            _trayIcon.QuitRequested -= OnQuitRequested;
            _trayIcon.Dispose();
            _trayIcon = null;
        }

        if (_window != null)
        {
            _window.Dispose();
            _window.Close();
            _window = null;
        }

        if (_singleInstanceMutex != null)
        {
            try
            {
                _singleInstanceMutex.ReleaseMutex();
            }
            catch (ApplicationException)
            {
                // Mutex wasn't acquired or already released
            }
            finally
            {
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }
        }

        _logger.LogInfo("Application shutdown completed.", "Lifecycle");
        _logger.Dispose();

        this.Exit();
    }
}
