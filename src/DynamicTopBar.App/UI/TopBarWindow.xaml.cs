using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;
using Windows.Storage.Streams;
using DynamicTopBar.App.Platform;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using DynamicTopBar.Core.Services;
using DynamicTopBar.Core.Services.Plugins;
using DynamicTopBar.Core.States;
using WinRT;
using WinRT.Interop;

namespace DynamicTopBar.App.UI;

/// <summary>
/// Borderless, non-activating, topmost top bar overlay window that transitions smoothly
/// between a discreet idle capsule and an expanded modular panel.
/// </summary>
public sealed partial class TopBarWindow : Window, IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly AppWindow _appWindow;
    private readonly IMediaSessionService _mediaService;
    private readonly OverlayStateMachine _stateMachine;
    private readonly IFeatureRegistry _featureRegistry;
    private readonly IFeatureNavigator _featureNavigator;
    private readonly IFeatureStateManager _featureStateManager;
    private readonly ISettingsService _settingsService;
    private readonly IFeaturePreferencesService _preferencesService;
    private readonly IFocusTimerService _focusTimerService;
    private readonly IQuickLaunchService _quickLaunchService;
    private readonly ISystemGlanceService _systemGlanceService;
    private readonly IStopwatchService _stopwatchService;
    private readonly ISpotifyService _spotifyService;
    private readonly IWeatherService _weatherService;
    private readonly ITodoService _todoService;
    private readonly ICalendarService _calendarService;
    private readonly INotesService _notesService;
    private readonly IClipboardService _clipboardService;
    private readonly IReminderService _reminderService;
    private readonly IShortcutService _shortcutService;
    private readonly ICommandPaletteService _commandPaletteService;
    private readonly INotificationService _notificationService;
    private readonly IGamificationService _gamificationService;
    private readonly IProductivityHubService _productivityHubService;
    private readonly IToolRegistry _toolRegistry;
    private readonly IDeveloperUtilitiesService _devUtilsService;
    private readonly IFileToolsService _fileToolsService;
    private readonly IImageToolsService _imageToolsService;
    private readonly IPluginManager _pluginManager;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IAiAssistantService _aiAssistantService;
    private readonly IPersonalizationService _personalizationService;
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly DispatcherTimer _clockTimer;
    private readonly CancellationTokenSource _windowCts = new();
    private DispatcherTimer? _notesAutosaveTimer;
    private bool _isUpdatingNoteUi;

    private MediaSnapshot _currentSnapshot = MediaSnapshot.Empty;
    private int _currentWidthDip = 340;
    private int _currentHeightDip = 44;
    private bool _isPositioning;
    private bool _isFavorite;
    private bool _isSettingsOpen;
    private bool _isCommandPaletteOpen;
    private bool _isDisposed;
    private bool _isAutoHideEnabled = true;
    private bool _isConcealed;
    private bool _isPointerOver;
    private bool _isInputFocused;
    private bool _isSuspendedForFullscreen;
    private SettingsWindow? _settingsWindow;
    private CommandPaletteWindow? _commandPaletteWindow;
    private DispatcherTimer? _autoHideTimer;
    private string _selectedToolCategory = "Developer";
    private IDesktopTool? _selectedTool;
    private string? _lastToolResultText;

    public IFeatureRegistry FeatureRegistry => _featureRegistry;
    public IFeatureNavigator FeatureNavigator => _featureNavigator;
    public IFeatureStateManager FeatureStateManager => _featureStateManager;
    public ISettingsService SettingsService => _settingsService;
    public IFeaturePreferencesService PreferencesService => _preferencesService;
    public IFocusTimerService FocusTimerService => _focusTimerService;
    public IQuickLaunchService QuickLaunchService => _quickLaunchService;
    public ISystemGlanceService SystemGlanceService => _systemGlanceService;
    public IStopwatchService StopwatchService => _stopwatchService;
    public ISpotifyService SpotifyService => _spotifyService;
    public IWeatherService WeatherService => _weatherService;
    public ITodoService TodoService => _todoService;
    public ICalendarService CalendarService => _calendarService;
    public INotesService NotesService => _notesService;
    public IClipboardService ClipboardService => _clipboardService;
    public IReminderService ReminderService => _reminderService;
    public IShortcutService ShortcutService => _shortcutService;
    public ICommandPaletteService CommandPaletteService => _commandPaletteService;
    public INotificationService NotificationService => _notificationService;
    public IGamificationService GamificationService => _gamificationService;
    public IProductivityHubService ProductivityHubService => _productivityHubService;
    public IToolRegistry ToolRegistry => _toolRegistry;
    public IDeveloperUtilitiesService DeveloperUtilitiesService => _devUtilsService;
    public IFileToolsService FileToolsService => _fileToolsService;
    public IImageToolsService ImageToolsService => _imageToolsService;
    public IPluginManager PluginManager => _pluginManager;
    public IWorkflowEngine WorkflowEngine => _workflowEngine;
    public IAiAssistantService AiAssistantService => _aiAssistantService;
    public IPersonalizationService PersonalizationService => _personalizationService;
    public IDiagnosticsService DiagnosticsService => _diagnosticsService;

    public TopBarWindow(
        IMediaSessionService? mediaService = null,
        OverlayStateMachine? stateMachine = null,
        IFeatureNavigator? featureNavigator = null,
        IFocusTimerService? focusTimerService = null,
        IQuickLaunchService? quickLaunchService = null,
        ISystemGlanceService? systemGlanceService = null,
        IStopwatchService? stopwatchService = null,
        IFeaturePreferencesService? preferencesService = null,
        IFeatureRegistry? featureRegistry = null,
        IFeatureStateManager? featureStateManager = null,
        ISettingsService? settingsService = null,
        ISpotifyService? spotifyService = null,
        IWeatherService? weatherService = null,
        ITodoService? todoService = null,
        ICalendarService? calendarService = null,
        INotesService? notesService = null,
        IClipboardService? clipboardService = null,
        IReminderService? reminderService = null,
        IShortcutService? shortcutService = null,
        ICommandPaletteService? commandPaletteService = null,
        INotificationService? notificationService = null,
        IGamificationService? gamificationService = null,
        IProductivityHubService? productivityHubService = null,
        IToolRegistry? toolRegistry = null,
        IDeveloperUtilitiesService? devUtilsService = null,
        IFileToolsService? fileToolsService = null,
        IImageToolsService? imageToolsService = null,
        IPluginManager? pluginManager = null,
        IWorkflowEngine? workflowEngine = null,
        IAiAssistantService? aiAssistantService = null,
        IPersonalizationService? personalizationService = null,
        IDiagnosticsService? diagnosticsService = null)
    {
        InitializeComponent();

        _featureRegistry = featureRegistry ?? DynamicTopBar.Core.Services.FeatureRegistry.CreateDefaultMultifunctionRegistry();
        if (!_featureRegistry.Contains("tools"))
        {
            _featureRegistry.Register(new FeatureDescriptor
            {
                Id = "tools",
                DisplayName = "Tools Engine",
                IconKey = "\uE74C",
                DisplayOrder = 12,
                RoadmapPhase = 3,
                IsPlaceholder = false,
                Description = "Universal desktop tools: Image utilities, File tools, and Developer helpers"
            });
        }
        _featureStateManager = featureStateManager ?? new FeatureStateManager();
        _settingsService = settingsService ?? (preferencesService != null ? new SettingsService(preferencesService) : new SettingsService());
        _preferencesService = _settingsService;

        _mediaService = mediaService ?? new WindowsMediaSessionService();
        _stateMachine = stateMachine ?? new OverlayStateMachine();
        _featureNavigator = featureNavigator ?? new FeatureNavigator(_featureRegistry);

        _focusTimerService = focusTimerService ?? new FocusTimerService();
        _quickLaunchService = quickLaunchService ?? new QuickLaunchService(new WindowsProcessLauncher());
        _systemGlanceService = systemGlanceService ?? new WindowsSystemGlanceService();
        _stopwatchService = stopwatchService ?? new StopwatchService();
        _spotifyService = spotifyService ?? new SpotifyService(new WindowsProtectedDataTokenStore());
        _weatherService = weatherService ?? new WeatherService(new OpenMeteoWeatherProvider());
        _todoService = todoService ?? new TodoService(new FileTodoRepository());
        _calendarService = calendarService ?? new CalendarService(new FileCalendarRepository());
        var dataProtector = new WindowsDpapiDataProtector();
        _notesService = notesService ?? new NotesService(new FileNotesRepository(dataProtector: dataProtector));
        _clipboardService = clipboardService ?? new ClipboardService(new WindowsClipboardAdapter(), dataProtector: dataProtector);
        _reminderService = reminderService ?? new ReminderService(_todoService, _calendarService);

        _shortcutService = shortcutService ?? new ShortcutService();
        _commandPaletteService = commandPaletteService ?? new CommandPaletteService();
        _notificationService = notificationService ?? new NotificationService();
        _devUtilsService = devUtilsService ?? new DeveloperUtilitiesService();
        _fileToolsService = fileToolsService ?? new FileToolsService();
        _imageToolsService = imageToolsService ?? new WindowsImageToolsService();

        _gamificationService = gamificationService ?? new GamificationEngineService(
            focusTimerService: _focusTimerService,
            todoService: _todoService,
            devUtilsService: _devUtilsService);
        _gamificationService.LevelUp += OnLevelUp;
        _gamificationService.BadgeUnlocked += OnBadgeUnlocked;

        _productivityHubService = productivityHubService ?? new ProductivityHubService(
            _calendarService, _todoService, _notesService, _focusTimerService, _gamificationService);
        _toolRegistry = toolRegistry ?? new ToolRegistry();

        foreach (var tool in StandardDesktopTools.CreateAll(_devUtilsService, _fileToolsService, _imageToolsService))
        {
            _toolRegistry.Register(tool);
        }

        var appLogger = new LoggingService();
        _diagnosticsService = diagnosticsService ?? new DiagnosticsService(appLogger);
        _pluginManager = pluginManager ?? new PluginManager(_toolRegistry, _commandPaletteService, appLogger);
        _workflowEngine = workflowEngine ?? new WorkflowEngine(_toolRegistry, _commandPaletteService, appLogger);
        _aiAssistantService = aiAssistantService ?? new AiAssistantService(_toolRegistry, appLogger);
        _personalizationService = personalizationService ?? new PersonalizationService(_settingsService, appLogger);

        // Load & apply saved module preferences
        var initialPrefs = _settingsService.LoadPreferences();
        _isAutoHideEnabled = initialPrefs.AutoHideEnabled;
        _featureNavigator.ApplyPreferences(initialPrefs);
        if (initialPrefs.ClipboardOptIn)
        {
            _clipboardService.SetOptIn(true);
        }

        _hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = this.AppWindow;

        ConfigureWindowStyles();
        RootGrid.GotFocus += OnAnyControlGotFocus;
        RootGrid.LostFocus += OnAnyControlLostFocus;
        PositionAndSizeWindow(_currentWidthDip, _currentHeightDip, customTopInset: 8, cornerRadiusDip: 22);

        // System clock timer ticking once per second
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += OnClockTimerTick;
        _clockTimer.Start();
        UpdateClock();

        // Auto-hide inactivity timer (smooth delay before concealing to trigger notch)
        _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(450) };
        _autoHideTimer.Tick += OnAutoHideTimerTick;

        // Wire window state, navigation, and module events
        _appWindow.Changed += OnAppWindowChanged;
        _stateMachine.StateChanged += OnStateMachineStateChanged;
        _mediaService.SnapshotChanged += OnMediaSnapshotChanged;
        _featureNavigator.CurrentFeatureChanged += OnCurrentFeatureChanged;
        _focusTimerService.SnapshotChanged += OnFocusTimerSnapshotChanged;
        _systemGlanceService.SnapshotChanged += OnSystemGlanceSnapshotChanged;
        _stopwatchService.SnapshotChanged += OnStopwatchSnapshotChanged;
        _spotifyService.AuthStateChanged += OnSpotifyAuthStateChanged;
        _spotifyService.PlaybackChanged += OnSpotifyPlaybackChanged;
        _weatherService.SnapshotChanged += OnWeatherSnapshotChanged;
        _todoService.ItemsChanged += OnTodoItemsChanged;
        _calendarService.GlanceChanged += OnCalendarGlanceChanged;
        _notesService.NotesChanged += OnNotesChanged;
        _notesService.SelectedNoteChanged += OnSelectedNoteChanged;
        _clipboardService.StateChanged += OnClipboardStateChanged;
        _clipboardService.SnippetsChanged += OnClipboardSnippetsChanged;
        _reminderService.ReminderFired += OnReminderFired;

        _shortcutService.ShortcutTriggered += OnShortcutTriggered;
        _commandPaletteService.CommandFailed += OnCommandFailed;
        _notificationService.ActiveNotificationChanged += OnActiveNotificationChanged;
        _productivityHubService.GlanceChanged += OnProductivityGlanceChanged;

        // Hook animation completions
        if (CollapseStoryboard != null)
        {
            CollapseStoryboard.Completed += OnCollapseStoryboardCompleted;
        }
        if (SlideUpStoryboard != null)
        {
            SlideUpStoryboard.Completed += OnSlideUpStoryboardCompleted;
        }

        // Apply initial feature navigation UI and module snapshots
        UpdateNavigationUi();
        UpdatePinButtonUi();

        // Start in concealed state at top of screen if Auto-Hide is enabled
        if (_isAutoHideEnabled)
        {
            FinishConceal();
        }
        RenderMediaSnapshot(_mediaService.CurrentSnapshot);
        RenderFocusTimerSnapshot(_focusTimerService.CurrentSnapshot);
        RenderSystemGlanceSnapshot(_systemGlanceService.CurrentSnapshot);
        RenderStopwatchSnapshot(_stopwatchService.CurrentSnapshot);
        RenderSpotifySnapshot(_spotifyService.CurrentPlayback);
        RenderWeatherSnapshot(_weatherService.CurrentSnapshot);
        RenderTodoList(_todoService.Items);
        RenderCalendarGlance(_calendarService.Glance);
        RenderNotes(_notesService.Notes, _notesService.SelectedNote);
        RenderClipboard(_clipboardService.State, _clipboardService.Snippets);

        ApplyMotionAccessibilityPreferences();

        _ = InitializePhase2ServicesAsync(_windowCts.Token);
    }

    private void ApplyMotionAccessibilityPreferences()
    {
        if (!MotionTokens.AreAnimationsEnabled)
        {
            var instant = new Microsoft.UI.Xaml.Duration(TimeSpan.FromMilliseconds(1));
            SetStoryboardDuration(ExpandStoryboard, instant);
            SetStoryboardDuration(CollapseStoryboard, instant);
            SetStoryboardDuration(SlideUpStoryboard, instant);
            SetStoryboardDuration(SlideDownStoryboard, instant);
        }
    }

    private static void SetStoryboardDuration(Storyboard? sb, Microsoft.UI.Xaml.Duration duration)
    {
        if (sb == null) return;
        foreach (var child in sb.Children)
        {
            child.Duration = duration;
        }
    }

    private void ConfigureWindowStyles()
    {
        this.ExtendsContentIntoTitleBar = true;

        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        int exStyle = Win32Interop.GetWindowLong(_hwnd, Win32Interop.GWL_EXSTYLE);
        Win32Interop.SetWindowLong(
            _hwnd,
            Win32Interop.GWL_EXSTYLE,
            exStyle | Win32Interop.WS_EX_TOOLWINDOW | Win32Interop.WS_EX_TOPMOST | Win32Interop.WS_EX_NOACTIVATE);

        // Turn off Windows 11 DWM white outlier border (DWMWA_COLOR_NONE = 0xFFFFFFFE)
        uint borderColor = Win32Interop.DWMWA_COLOR_NONE;
        Win32Interop.DwmSetWindowAttribute(_hwnd, Win32Interop.DWMWA_BORDER_COLOR, ref borderColor, sizeof(uint));

        // Use modern rounded corners for DWM
        uint cornerPref = Win32Interop.DWMWCP_ROUND;
        Win32Interop.DwmSetWindowAttribute(_hwnd, Win32Interop.DWMWA_WINDOW_CORNER_PREFERENCE, ref cornerPref, sizeof(uint));

        // Extend DWM frame into client area to enable true transparent background in RootGrid
        var margins = new Win32Interop.MARGINS { cxLeftWidth = -1, cxRightWidth = -1, cyTopHeight = -1, cyBottomHeight = -1 };
        Win32Interop.DwmExtendFrameIntoClientArea(_hwnd, ref margins);

        this.SystemBackdrop = null;
    }

    private void PositionAndSizeWindow(int widthDip, int heightDip, int? customTopInset = null, int cornerRadiusDip = 22)
    {
        if (_isDisposed || _isPositioning) return;
        _isPositioning = true;

        try
        {
            _currentWidthDip = widthDip;
            _currentHeightDip = heightDip;

            uint dpi = Win32Interop.GetDpiForWindow(_hwnd);
            double scaling = dpi > 0 ? dpi / 96.0 : 1.0;

            int pixelWidth = (int)(widthDip * scaling);
            int pixelHeight = (int)(heightDip * scaling);
            int topInsetDip = customTopInset ?? 8;
            int topInset = (int)(topInsetDip * scaling);

            var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
            int screenWidth = displayArea.WorkArea.Width;
            int screenX = displayArea.WorkArea.X;
            int screenY = displayArea.WorkArea.Y;

            int posX = screenX + (screenWidth - pixelWidth) / 2;
            int posY = screenY + topInset;

            // Only call MoveAndResize if position or dimensions actually changed
            if (_appWindow.Position.X != posX ||
                _appWindow.Position.Y != posY ||
                _appWindow.Size.Width != pixelWidth ||
                _appWindow.Size.Height != pixelHeight)
            {
                _appWindow.MoveAndResize(new RectInt32(posX, posY, pixelWidth, pixelHeight));
            }

            // Clip the native Win32 window to the exact pill/panel shape.
            // This eliminates rectangular corners and prevents black box borders on any version of Windows.
            int pixelRadius = (int)(cornerRadiusDip * scaling);
            IntPtr hRgn = Win32Interop.CreateRoundRectRgn(0, 0, pixelWidth + 1, pixelHeight + 1, pixelRadius * 2, pixelRadius * 2);
            Win32Interop.SetWindowRgn(_hwnd, hRgn, true);
        }
        finally
        {
            _isPositioning = false;
        }
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (_isDisposed) return;

        // Re-center only when presenter changes or display metrics change externally
        if (args.DidPresenterChange)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (!_isDisposed)
                {
                    int radius = _stateMachine.CurrentState == OverlayState.Expanded ? 20 : 22;
                    int inset = _isConcealed ? 0 : 8;
                    PositionAndSizeWindow(_currentWidthDip, _currentHeightDip, inset, radius);
                }
            });
        }
    }

    private void OnClockTimerTick(object? sender, object e)
    {
        UpdateClock();
        CheckForegroundFullscreen();
    }

    private void CheckForegroundFullscreen()
    {
        if (_isDisposed) return;

        try
        {
            IntPtr fgHwnd = Win32Interop.GetForegroundWindow();
            if (fgHwnd == IntPtr.Zero || fgHwnd == _hwnd)
            {
                RestoreFromFullscreenSuspension();
                return;
            }

            IntPtr shell = Win32Interop.GetShellWindow();
            IntPtr desktop = Win32Interop.GetDesktopWindow();
            if (fgHwnd == shell || fgHwnd == desktop)
            {
                RestoreFromFullscreenSuspension();
                return;
            }

            if (!Win32Interop.GetWindowRect(fgHwnd, out var fgRect))
            {
                RestoreFromFullscreenSuspension();
                return;
            }

            IntPtr fgMon = Win32Interop.MonitorFromWindow(fgHwnd, Win32Interop.MONITOR_DEFAULTTONEAREST);
            IntPtr barMon = Win32Interop.MonitorFromWindow(_hwnd, Win32Interop.MONITOR_DEFAULTTONEAREST);

            if (fgMon != barMon)
            {
                // Fullscreen window is on a different monitor
                RestoreFromFullscreenSuspension();
                return;
            }

            var mi = new Win32Interop.MONITORINFO { cbSize = System.Runtime.InteropServices.Marshal.SizeOf<Win32Interop.MONITORINFO>() };
            if (Win32Interop.GetMonitorInfo(fgMon, ref mi))
            {
                // Verify if the active window covers the entire active monitor
                bool isFullscreen = fgRect.Left <= mi.rcMonitor.Left &&
                                    fgRect.Top <= mi.rcMonitor.Top &&
                                    fgRect.Right >= mi.rcMonitor.Right &&
                                    fgRect.Bottom >= mi.rcMonitor.Bottom;

                if (isFullscreen)
                {
                    if (!_isSuspendedForFullscreen)
                    {
                        _isSuspendedForFullscreen = true;
                        // Suspend topmost to prevent z-fighting, flicker, and screen tearing during gaming/media playback
                        Win32Interop.SetWindowPos(_hwnd, Win32Interop.HWND_NOTOPMOST, 0, 0, 0, 0,
                            Win32Interop.SWP_NOMOVE | Win32Interop.SWP_NOSIZE | Win32Interop.SWP_NOACTIVATE);
                    }
                }
                else
                {
                    RestoreFromFullscreenSuspension();
                }
            }
        }
        catch
        {
            // Defensive recovery
        }
    }

    private void RestoreFromFullscreenSuspension()
    {
        if (_isSuspendedForFullscreen)
        {
            _isSuspendedForFullscreen = false;
            Win32Interop.SetWindowPos(_hwnd, Win32Interop.HWND_TOPMOST, 0, 0, 0, 0,
                Win32Interop.SWP_NOMOVE | Win32Interop.SWP_NOSIZE | Win32Interop.SWP_NOACTIVATE);
        }
    }

    public void HandleSystemTimeChanged()
    {
        if (_isDisposed) return;
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateClock();
            _focusTimerService.Synchronize();
            _systemGlanceService.Refresh();
            _reminderService.CheckReminders();
        });
    }

    public void HandleSystemResumed()
    {
        if (_isDisposed) return;
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateClock();
            _focusTimerService.Synchronize();
            _systemGlanceService.Refresh();
            _reminderService.CheckReminders();
            _ = _weatherService.RefreshAsync(force: false, _windowCts.Token);
        });
    }

    private void UpdateClock()
    {
        var now = DateTime.Now;
        var prefs = _preferencesService.Load();
        bool use24 = prefs.ClockUse24HourFormat;

        ClockText.Text = now.ToString(use24 ? "HH:mm" : "h:mm tt");
        if (LiveActivityDateText != null)
        {
            LiveActivityDateText.Text = now.ToString("ddd, MMM d");
        }

        if (ClockLargeTimeText != null)
        {
            ClockLargeTimeText.Text = now.ToString(use24 ? "HH:mm:ss" : "h:mm:ss tt");
            ClockDayOfWeekText.Text = now.ToString("dddd");
            ClockFullDateText.Text = now.ToString("MMMM dd, yyyy");
            var tz = TimeZoneInfo.Local;
            ClockTzAbbrText.Text = tz.IsDaylightSavingTime(now) ? "DST" : "STD";
            ClockTimeZoneNameText.Text = $"{(tz.BaseUtcOffset >= TimeSpan.Zero ? "+" : "-")}{tz.BaseUtcOffset:hh\\:mm} ({tz.DisplayName})";
        }
    }

    private void OnHeaderBarTapped(object sender, TappedRoutedEventArgs e)
    {
        // If the top bar is already expanded, do not re-expand
        if (_stateMachine.CurrentState == OverlayState.Expanded)
        {
            return;
        }

        // If the tap originated from an interactive button or control, ignore it
        if (e.OriginalSource is DependencyObject dep)
        {
            if (FindVisualParent<Button>(dep) != null)
            {
                return;
            }
        }

        _stateMachine.UserInteracted();
        _stateMachine.ForceExpand();
        e.Handled = true;
    }

    private void OnAppSourcePillTapped(object sender, TappedRoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (_stateMachine.CurrentState == OverlayState.Expanded)
        {
            _stateMachine.ForceIdle();
        }
        else
        {
            _stateMachine.ForceExpand();
        }
        e.Handled = true;
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T parent) return parent;
            child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private void OnRootPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _isPointerOver = true;
        _autoHideTimer?.Stop();
        if (_isConcealed)
        {
            RevealBarFromConcealed();
        }
        _stateMachine.PointerEntered();
    }

    private void OnRootPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (_isPositioning) return;

        var point = e.GetCurrentPoint(RootGrid).Position;
        if (point.X >= -4 && point.X <= RootGrid.ActualWidth + 4 &&
            point.Y >= -4 && point.Y <= RootGrid.ActualHeight + 4)
        {
            // Pointer is still inside the RootGrid bounds (e.g. crossing child elements)
            return;
        }

        _isPointerOver = false;
        _stateMachine.PointerExited();

        if (_isAutoHideEnabled && !_isSettingsOpen && !_isCommandPaletteOpen && !_isInputFocused)
        {
            if (_stateMachine.CurrentState == OverlayState.Expanded)
            {
                _stateMachine.ForceIdle();
            }

            _autoHideTimer?.Stop();
            _autoHideTimer?.Start();
        }
    }

    private void OnStateMachineStateChanged(object? sender, OverlayStateChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ApplyOverlayState(e.NewState);
        });
    }

    private void ApplyOverlayState(OverlayState state)
    {
        switch (state)
        {
            case OverlayState.Idle:
                // Start smooth collapse fade-out animation
                UpdateLiveActivityState();
                CollapseStoryboard?.Begin();
                break;

            case OverlayState.HoverPending:
                // User is hovering; waiting for 500ms delay to complete
                _autoHideTimer?.Stop();
                break;

            case OverlayState.Expanded:
                // Expand window downwards and slide-fade in the panel
                _autoHideTimer?.Stop();
                ExpandedPanelGrid.Visibility = Visibility.Visible;
                CollapseButton.Visibility = Visibility.Visible;
                AppChevronIcon.Glyph = "\uE70E"; // Chevron up

                MainContainer.CornerRadius = new CornerRadius(20);
                PositionAndSizeWindow(380, 240, customTopInset: 8, cornerRadiusDip: 20);
                UpdateLiveActivityState();
                UpdateNavigationUi();
                ExpandStoryboard?.Begin();
                break;

            case OverlayState.CollapsePending:
                // Waiting for 4000ms collapse delay; re-entry returns cleanly to Expanded
                break;
        }
    }

    private void OnCollapseStoryboardCompleted(object? sender, object e)
    {
        if (_stateMachine.CurrentState == OverlayState.Idle)
        {
            ExpandedPanelGrid.Visibility = Visibility.Collapsed;
            CollapseButton.Visibility = Visibility.Collapsed;
            AppChevronIcon.Glyph = "\uE70D"; // Chevron down
            MainContainer.CornerRadius = new CornerRadius(22);
            PositionAndSizeWindow(340, 44, customTopInset: 8, cornerRadiusDip: 22);
            UpdateLiveActivityState();

            if (_isAutoHideEnabled && !_isPointerOver && !_isSettingsOpen && !_isCommandPaletteOpen && !_isInputFocused)
            {
                _autoHideTimer?.Stop();
                _autoHideTimer?.Start();
            }
        }
    }

    private void UpdateLiveActivityState()
    {
        if (_isDisposed) return;

        bool isExpanded = _stateMachine.CurrentState == OverlayState.Expanded;

        if (isExpanded)
        {
            // When expanded, show active module with chevron up to collapse
            AppSourcePillBorder.Visibility = Visibility.Visible;
            AppChevronIcon.Glyph = "\uE70E"; // Chevron up
            CollapseChevronIcon.Glyph = "\uE70E"; // Chevron up
            ToolTipService.SetToolTip(AppSourcePillBorder, "Tutup Dropdown Apps");
            ToolTipService.SetToolTip(CollapseButton, "Tutup Dropdown Apps");
            AppSourceText.Text = _featureNavigator.CurrentFeature?.DisplayName ?? "Apps";

            LiveActivityIdlePanel.Visibility = Visibility.Collapsed;
            LiveActivityMediaPanel.Visibility = Visibility.Collapsed;
            LiveActivityTimerPanel.Visibility = Visibility.Collapsed;
            LiveActivityWaveVisualizer.Visibility = Visibility.Collapsed;
            NotificationCapsulePill.Visibility = Visibility.Collapsed;

            SystemStatusGlyphsPanel.Visibility = Visibility.Visible;
            ClockText.Visibility = Visibility.Visible;
            CollapseButton.Visibility = Visibility.Visible;
            PinButton.Visibility = Visibility.Visible;

            WaveStoryboard?.Stop();
            return;
        }

        // When collapsed/idle: contextual Dynamic Island priority engine
        AppChevronIcon.Glyph = "\uE70D"; // Chevron down
        CollapseChevronIcon.Glyph = "\uE70D"; // Chevron down
        ToolTipService.SetToolTip(AppSourcePillBorder, "Buka Dropdown Apps");
        ToolTipService.SetToolTip(CollapseButton, "Buka Dropdown Apps");
        CollapseButton.Visibility = Visibility.Visible;

        // Priority 1: Active Alert Notification
        if (_notificationService.CurrentActiveNotification != null)
        {
            AppSourcePillBorder.Visibility = Visibility.Collapsed;
            NotificationCapsulePill.Visibility = Visibility.Visible;
            LiveActivityMediaPanel.Visibility = Visibility.Collapsed;
            LiveActivityTimerPanel.Visibility = Visibility.Collapsed;
            LiveActivityIdlePanel.Visibility = Visibility.Collapsed;
            LiveActivityWaveVisualizer.Visibility = Visibility.Collapsed;

            SystemStatusGlyphsPanel.Visibility = Visibility.Collapsed;
            ClockText.Visibility = Visibility.Visible;
            PinButton.Visibility = Visibility.Visible;

            WaveStoryboard?.Stop();
            return;
        }

        NotificationCapsulePill.Visibility = Visibility.Collapsed;

        // Priority 2: Media Playback Live Activity
        bool isGsmtcPlaying = _currentSnapshot.HasActiveSession && _currentSnapshot.IsPlaying && !string.IsNullOrWhiteSpace(_currentSnapshot.Track.Title);
        bool isSpotifyPlaying = _spotifyService.AuthState == SpotifyAuthState.Authenticated && _spotifyService.CurrentPlayback.IsPlaying && _spotifyService.CurrentPlayback.Track != null;

        if (isGsmtcPlaying || isSpotifyPlaying)
        {
            AppSourcePillBorder.Visibility = Visibility.Collapsed;
            LiveActivityMediaPanel.Visibility = Visibility.Visible;
            LiveActivityTimerPanel.Visibility = Visibility.Collapsed;
            LiveActivityIdlePanel.Visibility = Visibility.Collapsed;

            if (isGsmtcPlaying)
            {
                LiveActivityTrackText.Text = _currentSnapshot.Track.Title;
                LiveActivityArtistText.Text = !string.IsNullOrWhiteSpace(_currentSnapshot.Track.Artist) ? _currentSnapshot.Track.Artist : "Unknown Artist";
            }
            else
            {
                var sp = _spotifyService.CurrentPlayback;
                LiveActivityTrackText.Text = sp.Track!.Name;
                LiveActivityArtistText.Text = !string.IsNullOrWhiteSpace(sp.Track.Artist) ? sp.Track.Artist : "Spotify";
            }

            LiveActivityWaveVisualizer.Visibility = Visibility.Visible;
            SystemStatusGlyphsPanel.Visibility = Visibility.Collapsed;
            ClockText.Visibility = Visibility.Visible;
            PinButton.Visibility = Visibility.Visible;

            WaveStoryboard?.Begin();
            return;
        }

        // Priority 3: Focus Timer Live Activity
        if (_focusTimerService.CurrentSnapshot.State == FocusTimerState.Running)
        {
            AppSourcePillBorder.Visibility = Visibility.Collapsed;
            LiveActivityMediaPanel.Visibility = Visibility.Collapsed;
            LiveActivityTimerPanel.Visibility = Visibility.Visible;
            LiveActivityIdlePanel.Visibility = Visibility.Collapsed;
            LiveActivityWaveVisualizer.Visibility = Visibility.Collapsed;

            LiveActivityTimerCountdownText.Text = _focusTimerService.CurrentSnapshot.FormattedRemaining;
            LiveActivityTimerStateText.Text = _focusTimerService.CurrentSnapshot.Mode switch
            {
                FocusSessionMode.ShortBreak => "Break",
                FocusSessionMode.LongBreak => "Break",
                _ => "Focus"
            };

            SystemStatusGlyphsPanel.Visibility = Visibility.Visible;
            ClockText.Visibility = Visibility.Visible;
            PinButton.Visibility = Visibility.Visible;

            WaveStoryboard?.Stop();
            return;
        }

        // Priority 4: Default Idle Mode (Prominent Apps Dropdown Button)
        LiveActivityMediaPanel.Visibility = Visibility.Collapsed;
        LiveActivityTimerPanel.Visibility = Visibility.Collapsed;
        LiveActivityIdlePanel.Visibility = Visibility.Collapsed;
        LiveActivityWaveVisualizer.Visibility = Visibility.Collapsed;

        AppSourcePillBorder.Visibility = Visibility.Visible;
        AppSourceIcon.Glyph = "\uEA86"; // Apps grid icon
        AppSourceText.Text = "Apps";

        SystemStatusGlyphsPanel.Visibility = Visibility.Visible;
        ClockText.Visibility = Visibility.Visible;
        PinButton.Visibility = Visibility.Visible;

        WaveStoryboard?.Stop();
    }

    private void OnMediaSnapshotChanged(object? sender, MediaSnapshotChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderMediaSnapshot(e.Snapshot);
        });
    }

    private void RenderMediaSnapshot(MediaSnapshot snapshot)
    {
        _currentSnapshot = snapshot;

        if (SpotifyQuickConnectIcon != null)
        {
            if (_spotifyService.AuthState == SpotifyAuthState.Authenticated)
            {
                SpotifyQuickConnectIcon.Foreground = (Brush)Application.Current.Resources["OverlayAccentBrush"];
                ToolTipService.SetToolTip(SpotifyQuickConnectButton, $"Spotify Connected: {_spotifyService.CurrentUser?.DisplayName ?? "Active"}");
            }
            else
            {
                SpotifyQuickConnectIcon.Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"];
                ToolTipService.SetToolTip(SpotifyQuickConnectButton, "Connect Spotify Web API");
            }
        }

        // Resolve friendly application name for chip when on media feature
        if (_featureNavigator.CurrentFeature == null || _featureNavigator.CurrentFeature.Id == "media")
        {
            AppSourceText.Text = ResolveFriendlyAppName(snapshot.AppSourceId);
        }

        if (snapshot.HasActiveSession && !string.IsNullOrWhiteSpace(snapshot.Track.Title))
        {
            TrackTitleText.Text = snapshot.Track.Title;
            TrackArtistText.Text = !string.IsNullOrWhiteSpace(snapshot.Track.Artist) 
                ? snapshot.Track.Artist 
                : "Unknown Artist";

            // Play/Pause icon (E769 = Pause, E768 = Play)
            PlayPauseIcon.Glyph = snapshot.IsPlaying ? "\uE769" : "\uE768";

            PreviousButton.IsEnabled = snapshot.Actions.CanSkipPrevious;
            PlayPauseButton.IsEnabled = snapshot.Actions.CanTogglePlayPause || snapshot.Actions.CanPlay || snapshot.Actions.CanPause;
            NextButton.IsEnabled = snapshot.Actions.CanSkipNext;

            // Timeline and progress display
            if (snapshot.Timeline.Duration.HasValue && snapshot.Timeline.Duration.Value > TimeSpan.Zero)
            {
                ProgressContainer.Visibility = Visibility.Visible;
                var pos = snapshot.Timeline.Position ?? TimeSpan.Zero;
                var dur = snapshot.Timeline.Duration.Value;

                double percent = Math.Clamp((pos.TotalSeconds / dur.TotalSeconds) * 100.0, 0, 100);
                TrackProgressBar.Value = percent;

                PositionText.Text = FormatTimeSpan(pos);
                var remaining = dur > pos ? dur - pos : TimeSpan.Zero;
                DurationText.Text = $"-{FormatTimeSpan(remaining)}";
            }
            else
            {
                ProgressContainer.Visibility = Visibility.Collapsed;
            }

            _ = UpdateArtworkAsync(snapshot.Track.ArtworkBytes);
        }
        else if (_spotifyService.AuthState == SpotifyAuthState.Authenticated && _spotifyService.CurrentPlayback.IsActive && _spotifyService.CurrentPlayback.Track != null)
        {
            var sp = _spotifyService.CurrentPlayback;
            TrackTitleText.Text = sp.Track.Name;
            TrackArtistText.Text = !string.IsNullOrWhiteSpace(sp.Track.Artist) ? sp.Track.Artist : "Spotify";

            PlayPauseIcon.Glyph = sp.IsPlaying ? "\uE769" : "\uE768";
            PreviousButton.IsEnabled = true;
            PlayPauseButton.IsEnabled = true;
            NextButton.IsEnabled = true;

            if (sp.Track.Duration > TimeSpan.Zero)
            {
                ProgressContainer.Visibility = Visibility.Visible;
                double percent = Math.Clamp((sp.Position.TotalSeconds / sp.Track.Duration.TotalSeconds) * 100.0, 0, 100);
                TrackProgressBar.Value = percent;

                PositionText.Text = FormatTimeSpan(sp.Position);
                var remaining = sp.Track.Duration > sp.Position ? sp.Track.Duration - sp.Position : TimeSpan.Zero;
                DurationText.Text = $"-{FormatTimeSpan(remaining)}";
            }
            else
            {
                ProgressContainer.Visibility = Visibility.Collapsed;
            }

            if (_featureNavigator.CurrentFeature == null || _featureNavigator.CurrentFeature.Id == "media")
            {
                AppSourceText.Text = "Spotify";
            }

            _ = UpdateArtworkFromUrlAsync(sp.Track.ArtworkUrl);
        }
        else
        {
            TrackTitleText.Text = "No media playing";
            TrackArtistText.Text = "Play audio in any app";

            PlayPauseIcon.Glyph = "\uE768";
            PreviousButton.IsEnabled = false;
            PlayPauseButton.IsEnabled = false;
            NextButton.IsEnabled = false;
            ProgressContainer.Visibility = Visibility.Collapsed;

            _ = UpdateArtworkAsync(null);
        }

        UpdateLiveActivityState();
    }

    private static string ResolveFriendlyAppName(string? appSourceId)
    {
        if (string.IsNullOrWhiteSpace(appSourceId))
        {
            return "Music";
        }

        if (appSourceId.Contains("Spotify", StringComparison.OrdinalIgnoreCase))
            return "Spotify";
        if (appSourceId.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
            return "Chrome";
        if (appSourceId.Contains("Edge", StringComparison.OrdinalIgnoreCase))
            return "Edge";
        if (appSourceId.Contains("AppleMusic", StringComparison.OrdinalIgnoreCase))
            return "Apple Music";
        if (appSourceId.Contains("VLC", StringComparison.OrdinalIgnoreCase))
            return "VLC";

        return "Music";
    }

    private static string FormatTimeSpan(TimeSpan ts)
    {
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
            : $"{ts.Minutes}:{ts.Seconds:D2}";
    }

    private async Task UpdateArtworkAsync(byte[]? artworkBytes)
    {
        if (artworkBytes == null || artworkBytes.Length == 0)
        {
            ArtworkImage.Source = null;
            ArtworkImage.Visibility = Visibility.Collapsed;
            ArtworkFallbackIcon.Visibility = Visibility.Visible;

            if (LiveActivityAlbumArtImage != null)
            {
                LiveActivityAlbumArtImage.Source = null;
                LiveActivityAlbumArtImage.Visibility = Visibility.Collapsed;
            }
            if (LiveActivityDefaultMusicIcon != null)
            {
                LiveActivityDefaultMusicIcon.Visibility = Visibility.Visible;
            }
            return;
        }

        try
        {
            using var stream = new InMemoryRandomAccessStream();
            using (var writer = new DataWriter(stream.GetOutputStreamAt(0)))
            {
                writer.WriteBytes(artworkBytes);
                await writer.StoreAsync();
                await writer.FlushAsync();
                writer.DetachStream();
            }

            stream.Seek(0);
            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(stream);

            ArtworkImage.Source = bitmapImage;
            ArtworkImage.Visibility = Visibility.Visible;
            ArtworkFallbackIcon.Visibility = Visibility.Collapsed;

            if (LiveActivityAlbumArtImage != null)
            {
                LiveActivityAlbumArtImage.Source = bitmapImage;
                LiveActivityAlbumArtImage.Visibility = Visibility.Visible;
            }
            if (LiveActivityDefaultMusicIcon != null)
            {
                LiveActivityDefaultMusicIcon.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception)
        {
            ArtworkImage.Source = null;
            ArtworkImage.Visibility = Visibility.Collapsed;
            ArtworkFallbackIcon.Visibility = Visibility.Visible;

            if (LiveActivityAlbumArtImage != null)
            {
                LiveActivityAlbumArtImage.Source = null;
                LiveActivityAlbumArtImage.Visibility = Visibility.Collapsed;
            }
            if (LiveActivityDefaultMusicIcon != null)
            {
                LiveActivityDefaultMusicIcon.Visibility = Visibility.Visible;
            }
        }
    }

    private async void OnPreviousButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (_currentSnapshot.HasActiveSession)
        {
            await _mediaService.SkipPreviousAsync();
        }
        else if (_spotifyService.AuthState == SpotifyAuthState.Authenticated && _spotifyService.CurrentPlayback.IsActive)
        {
            await _spotifyService.SkipPreviousAsync();
        }
    }

    private async void OnPlayPauseButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (_currentSnapshot.HasActiveSession)
        {
            await _mediaService.TogglePlayPauseAsync();
        }
        else if (_spotifyService.AuthState == SpotifyAuthState.Authenticated)
        {
            await _spotifyService.TogglePlayPauseAsync();
        }
    }

    private async void OnNextButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (_currentSnapshot.HasActiveSession)
        {
            await _mediaService.SkipNextAsync();
        }
        else if (_spotifyService.AuthState == SpotifyAuthState.Authenticated && _spotifyService.CurrentPlayback.IsActive)
        {
            await _spotifyService.SkipNextAsync();
        }
    }

    private void OnSpotifyQuickConnectButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (sender is FrameworkElement target)
        {
            var flyout = new Flyout();
            var panel = new StackPanel { Spacing = 6, Width = 220, Padding = new Thickness(6) };

            if (_spotifyService.AuthState == SpotifyAuthState.Authenticated)
            {
                var user = _spotifyService.CurrentUser;
                var pb = _spotifyService.CurrentPlayback;
                panel.Children.Add(new TextBlock
                {
                    Text = "Spotify Web Connected",
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = (Brush)Application.Current.Resources["OverlayAccentBrush"]
                });
                panel.Children.Add(new TextBlock
                {
                    Text = $"User: {user?.DisplayName ?? "Active"} ({(user?.IsPremium == true ? "Premium" : "Free")})",
                    FontSize = 10,
                    Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"]
                });
                if (!string.IsNullOrEmpty(pb.DeviceName))
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"Device: {pb.DeviceName}",
                        FontSize = 9,
                        Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"]
                    });
                }

                var disconnectBtn = new Button
                {
                    Content = "Disconnect Spotify",
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 4, 0, 0),
                    Padding = new Thickness(6, 4, 6, 4)
                };
                disconnectBtn.Click += async (s, args) =>
                {
                    flyout.Hide();
                    await _spotifyService.DisconnectAsync();
                    RenderMediaSnapshot(_currentSnapshot);
                };
                panel.Children.Add(disconnectBtn);
            }
            else
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "Spotify Web Connect",
                    FontSize = 11,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"]
                });
                panel.Children.Add(new TextBlock
                {
                    Text = "Directly control Spotify playback via official Spotify Web API (PKCE OAuth).",
                    FontSize = 9,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"]
                });

                var connectBtn = new Button
                {
                    Content = "Connect Spotify Account...",
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 4, 0, 0),
                    Padding = new Thickness(6, 4, 6, 4),
                    Background = (Brush)Application.Current.Resources["OverlayAccentBrush"],
                    Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"]
                };
                connectBtn.Click += (s, args) =>
                {
                    flyout.Hide();
                    _featureNavigator.NavigateTo("spotify");
                    UpdateNavigationUi();
                };
                panel.Children.Add(connectBtn);
            }

            flyout.Content = panel;
            flyout.ShowAt(target);
        }
    }

    private async Task UpdateArtworkFromUrlAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            await UpdateArtworkAsync(null);
            return;
        }

        try
        {
            var bitmap = new BitmapImage(new Uri(url));
            ArtworkImage.Source = bitmap;
            ArtworkImage.Visibility = Visibility.Visible;
            ArtworkFallbackIcon.Visibility = Visibility.Collapsed;

            if (LiveActivityAlbumArtImage != null)
            {
                LiveActivityAlbumArtImage.Source = bitmap;
                LiveActivityAlbumArtImage.Visibility = Visibility.Visible;
            }
            if (LiveActivityDefaultMusicIcon != null)
            {
                LiveActivityDefaultMusicIcon.Visibility = Visibility.Collapsed;
            }
        }
        catch
        {
            await UpdateArtworkAsync(null);
        }
    }

    private void OnFavoriteButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _isFavorite = !_isFavorite;
        FavoriteIcon.Glyph = _isFavorite ? "\uE735" : "\uE734"; // E735 = filled star, E734 = outline star
        FavoriteIcon.Foreground = _isFavorite
            ? (SolidColorBrush)Application.Current.Resources["OverlayAccentBrush"]
            : (SolidColorBrush)Application.Current.Resources["OverlayTextSecondaryBrush"];
    }

    private void OnDeviceButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        try
        {
            _ = Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:sound"));
        }
        catch { }
    }

    private void OnCollapseButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (_stateMachine.CurrentState == OverlayState.Expanded)
        {
            _stateMachine.ForceIdle();
        }
        else
        {
            _stateMachine.ForceExpand();
        }
    }

    private void OnNavPreviousButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _isCommandPaletteOpen = false;
        _featureNavigator.NavigatePrevious();
    }

    private void OnNavNextButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _isCommandPaletteOpen = false;
        _featureNavigator.NavigateNext();
    }

    private void OnNavSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        OpenSettingsWindow();
    }

    private void OnAnyControlGotFocus(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TextBox || e.OriginalSource is AutoSuggestBox || e.OriginalSource is PasswordBox)
        {
            _isInputFocused = true;
            _autoHideTimer?.Stop();
            Win32Interop.SetForegroundWindow(_hwnd);
        }
    }

    private void OnAnyControlLostFocus(object sender, RoutedEventArgs e)
    {
        _isInputFocused = false;
        if (_stateMachine.CurrentState == OverlayState.Idle && _isAutoHideEnabled && !_isPointerOver)
        {
            _autoHideTimer?.Stop();
            _autoHideTimer?.Start();
        }
    }

    public void OpenSettingsWindow()
    {
        if (_settingsWindow == null || _settingsWindow.IsDisposed)
        {
            _settingsWindow = new SettingsWindow(
                _settingsService,
                _featureNavigator,
                _featureRegistry,
                _shortcutService,
                _clipboardService,
                _diagnosticsService,
                _notificationService,
                onSettingsChanged: () =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        var prefs = _settingsService.LoadPreferences();
                        _isAutoHideEnabled = prefs.AutoHideEnabled;
                        UpdatePinButtonUi();
                        UpdateNavigationUi();
                    });
                });
        }

        _settingsWindow.Activate();
    }

    public void OpenCommandPaletteWindow()
    {
        if (_commandPaletteWindow == null || _commandPaletteWindow.IsDisposed)
        {
            _commandPaletteWindow = new CommandPaletteWindow(
                _commandPaletteService,
                _featureNavigator,
                onModuleNavigated: (moduleId) =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (_stateMachine.CurrentState != OverlayState.Expanded)
                        {
                            _stateMachine.ForceExpand();
                        }
                        UpdateNavigationUi();
                    });
                });
        }

        _commandPaletteWindow.Activate();
        _commandPaletteWindow.FocusSearchBox();
    }

    private void SyncPreferenceCheckboxes()
    {
        var prefs = _preferencesService.Load();
        PrefMediaCheck.IsChecked = prefs.EnabledFeatureIds.Contains("media");
        PrefSpotifyCheck.IsChecked = prefs.EnabledFeatureIds.Contains("spotify");
        PrefClockCheck.IsChecked = prefs.EnabledFeatureIds.Contains("clock");
        PrefWeatherCheck.IsChecked = prefs.EnabledFeatureIds.Contains("weather");
        PrefTodoCheck.IsChecked = prefs.EnabledFeatureIds.Contains("todo");
        PrefFocusCheck.IsChecked = prefs.EnabledFeatureIds.Contains("focus");
        PrefQuickLaunchCheck.IsChecked = prefs.EnabledFeatureIds.Contains("quick-launch");
        PrefGlanceCheck.IsChecked = prefs.EnabledFeatureIds.Contains("system-glance");
        PrefCalendarCheck.IsChecked = prefs.EnabledFeatureIds.Contains("calendar");
        PrefNotesCheck.IsChecked = prefs.EnabledFeatureIds.Contains("notes");
        PrefClipboardCheck.IsChecked = prefs.EnabledFeatureIds.Contains("clipboard");

        Pref24HourClockCheck.IsChecked = prefs.ClockUse24HourFormat;
        PrefFahrenheitCheck.IsChecked = prefs.WeatherUnit == WeatherUnit.Fahrenheit;
        PrefRemindersCheck.IsChecked = prefs.RemindersEnabled;
        PrefClipboardOptInSettingCheck.IsChecked = prefs.ClipboardOptIn;
        PrefNotificationPrivacyCheck.IsChecked = _notificationService.Preferences.PrivacyMode;
    }

    private void OnModulePreferenceCheckClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();

        var enabled = new System.Collections.Generic.List<string>();
        if (PrefMediaCheck.IsChecked == true) enabled.Add("media");
        if (PrefSpotifyCheck.IsChecked == true) enabled.Add("spotify");
        if (PrefClockCheck.IsChecked == true) enabled.Add("clock");
        if (PrefWeatherCheck.IsChecked == true) enabled.Add("weather");
        if (PrefTodoCheck.IsChecked == true) enabled.Add("todo");
        if (PrefFocusCheck.IsChecked == true) enabled.Add("focus");
        if (PrefQuickLaunchCheck.IsChecked == true) enabled.Add("quick-launch");
        if (PrefGlanceCheck.IsChecked == true) enabled.Add("system-glance");
        if (PrefCalendarCheck.IsChecked == true) enabled.Add("calendar");
        if (PrefNotesCheck.IsChecked == true) enabled.Add("notes");
        if (PrefClipboardCheck.IsChecked == true) enabled.Add("clipboard");

        var currentPrefs = _preferencesService.Load();
        var updated = currentPrefs with { EnabledFeatureIds = enabled };
        _preferencesService.Save(updated);

        _featureNavigator.ApplyPreferences(updated);
        UpdateNavigationUi();
    }

    private void OnCurrentFeatureChanged(object? sender, FeatureNavigationChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (e.PreviousFeature != null)
            {
                _featureRegistry.GetModule(e.PreviousFeature.Id)?.Deactivate();
            }
            if (e.CurrentFeature != null)
            {
                _featureRegistry.GetModule(e.CurrentFeature.Id)?.Activate();
            }

            UpdateNavigationUi();
        });
    }

    private void UpdateNavigationUi()
    {
        if (_isCommandPaletteOpen)
        {
            ActiveFeatureIcon.Glyph = "\uE721"; // Magnifier icon
            ActiveFeatureNameText.Text = "Command Palette";
            AppSourceText.Text = "Commands";
            NavigationControlsPanel.Visibility = Visibility.Collapsed;

            MediaContentHost.Visibility = Visibility.Collapsed;
            SpotifyContentHost.Visibility = Visibility.Collapsed;
            FocusContentHost.Visibility = Visibility.Collapsed;
            QuickLaunchContentHost.Visibility = Visibility.Collapsed;
            SystemGlanceContentHost.Visibility = Visibility.Collapsed;
            StopwatchContentHost.Visibility = Visibility.Collapsed;
            ClockContentHost.Visibility = Visibility.Collapsed;
            WeatherContentHost.Visibility = Visibility.Collapsed;
            TodoContentHost.Visibility = Visibility.Collapsed;
            CalendarContentHost.Visibility = Visibility.Collapsed;
            NotesContentHost.Visibility = Visibility.Collapsed;
            ClipboardContentHost.Visibility = Visibility.Collapsed;
            SharedPlaceholderContentHost.Visibility = Visibility.Collapsed;
            EmptyModulesContentHost.Visibility = Visibility.Collapsed;
            SettingsContentHost.Visibility = Visibility.Collapsed;
            ProductivityContentHost.Visibility = Visibility.Collapsed;
            ToolsContentHost.Visibility = Visibility.Collapsed;

            CommandPaletteContentHost.Visibility = Visibility.Visible;
            return;
        }

        CommandPaletteContentHost.Visibility = Visibility.Collapsed;

        if (_isSettingsOpen)
        {
            ActiveFeatureIcon.Glyph = "\uE713";
            ActiveFeatureNameText.Text = "Preferences";
            NavSettingsIcon.Glyph = "\uE711"; // Close 'X' icon
            ToolTipService.SetToolTip(NavSettingsButton, "Close Preferences");

            NavigationControlsPanel.Visibility = Visibility.Collapsed;

            MediaContentHost.Visibility = Visibility.Collapsed;
            SpotifyContentHost.Visibility = Visibility.Collapsed;
            FocusContentHost.Visibility = Visibility.Collapsed;
            QuickLaunchContentHost.Visibility = Visibility.Collapsed;
            SystemGlanceContentHost.Visibility = Visibility.Collapsed;
            StopwatchContentHost.Visibility = Visibility.Collapsed;
            ClockContentHost.Visibility = Visibility.Collapsed;
            WeatherContentHost.Visibility = Visibility.Collapsed;
            TodoContentHost.Visibility = Visibility.Collapsed;
            CalendarContentHost.Visibility = Visibility.Collapsed;
            NotesContentHost.Visibility = Visibility.Collapsed;
            ClipboardContentHost.Visibility = Visibility.Collapsed;
            SharedPlaceholderContentHost.Visibility = Visibility.Collapsed;
            EmptyModulesContentHost.Visibility = Visibility.Collapsed;
            ProductivityContentHost.Visibility = Visibility.Collapsed;
            ToolsContentHost.Visibility = Visibility.Collapsed;
            SettingsContentHost.Visibility = Visibility.Visible;

            AppSourceText.Text = "Settings";
            SyncPreferenceCheckboxes();
            return;
        }

        NavSettingsIcon.Glyph = "\uE713"; // Gear icon
        ToolTipService.SetToolTip(NavSettingsButton, "Module Preferences");
        SettingsContentHost.Visibility = Visibility.Collapsed;

        var current = _featureNavigator.CurrentFeature;
        if (current == null)
        {
            ActiveFeatureIcon.Glyph = "\uE946";
            ActiveFeatureNameText.Text = "No Modules";
            AppSourceText.Text = "TopBar";

            NavigationControlsPanel.Visibility = Visibility.Collapsed;
            MediaContentHost.Visibility = Visibility.Collapsed;
            SpotifyContentHost.Visibility = Visibility.Collapsed;
            FocusContentHost.Visibility = Visibility.Collapsed;
            QuickLaunchContentHost.Visibility = Visibility.Collapsed;
            SystemGlanceContentHost.Visibility = Visibility.Collapsed;
            StopwatchContentHost.Visibility = Visibility.Collapsed;
            ClockContentHost.Visibility = Visibility.Collapsed;
            WeatherContentHost.Visibility = Visibility.Collapsed;
            TodoContentHost.Visibility = Visibility.Collapsed;
            CalendarContentHost.Visibility = Visibility.Collapsed;
            NotesContentHost.Visibility = Visibility.Collapsed;
            ClipboardContentHost.Visibility = Visibility.Collapsed;
            SharedPlaceholderContentHost.Visibility = Visibility.Collapsed;
            ProductivityContentHost.Visibility = Visibility.Collapsed;
            ToolsContentHost.Visibility = Visibility.Collapsed;
            EmptyModulesContentHost.Visibility = Visibility.Visible;
            ModuleSwitcherDock?.Children.Clear();
            return;
        }

        EmptyModulesContentHost.Visibility = Visibility.Collapsed;

        ActiveFeatureIcon.Glyph = current.IconKey;
        ActiveFeatureNameText.Text = current.DisplayName;

        NavPageIndicatorText.Text = $"{_featureNavigator.CurrentIndex + 1} / {_featureNavigator.TotalFeatures}";
        NavPreviousButton.IsEnabled = _featureNavigator.CanNavigatePrevious;
        NavNextButton.IsEnabled = _featureNavigator.CanNavigateNext;
        NavigationControlsPanel.Visibility = _featureNavigator.TotalFeatures > 1 ? Visibility.Visible : Visibility.Collapsed;

        string activeId = current.Id;
        UpdateModuleSwitcherDock(activeId);
        bool isPlaceholder = current.IsPlaceholder;

        MediaContentHost.Visibility = activeId == "media" ? Visibility.Visible : Visibility.Collapsed;
        SpotifyContentHost.Visibility = activeId == "spotify" ? Visibility.Visible : Visibility.Collapsed;
        FocusContentHost.Visibility = activeId == "focus" ? Visibility.Visible : Visibility.Collapsed;
        QuickLaunchContentHost.Visibility = activeId == "quick-launch" ? Visibility.Visible : Visibility.Collapsed;
        SystemGlanceContentHost.Visibility = activeId == "system-glance" ? Visibility.Visible : Visibility.Collapsed;
        StopwatchContentHost.Visibility = activeId == "stopwatch" ? Visibility.Visible : Visibility.Collapsed;
        ClockContentHost.Visibility = activeId == "clock" ? Visibility.Visible : Visibility.Collapsed;
        WeatherContentHost.Visibility = activeId == "weather" ? Visibility.Visible : Visibility.Collapsed;
        TodoContentHost.Visibility = activeId == "todo" ? Visibility.Visible : Visibility.Collapsed;
        CalendarContentHost.Visibility = activeId == "calendar" ? Visibility.Visible : Visibility.Collapsed;
        NotesContentHost.Visibility = activeId == "notes" ? Visibility.Visible : Visibility.Collapsed;
        ClipboardContentHost.Visibility = activeId == "clipboard" ? Visibility.Visible : Visibility.Collapsed;
        ProductivityContentHost.Visibility = activeId == "productivity" ? Visibility.Visible : Visibility.Collapsed;
        ToolsContentHost.Visibility = activeId == "tools" ? Visibility.Visible : Visibility.Collapsed;

        if (activeId == "tools")
        {
            RenderToolsHub();
        }

        if (isPlaceholder)
        {
            SharedPlaceholderContentHost.Visibility = Visibility.Visible;
            RenderPlaceholderModule(current);
        }
        else
        {
            SharedPlaceholderContentHost.Visibility = Visibility.Collapsed;
        }

        if (activeId == "media")
        {
            AppSourceText.Text = ResolveFriendlyAppName(_currentSnapshot.AppSourceId);
        }
        else
        {
            AppSourceText.Text = current.DisplayName;
        }

        var prefs = _preferencesService.Load();
        if (prefs.LastActiveFeatureId != activeId)
        {
            _preferencesService.Save(prefs with { LastActiveFeatureId = activeId });
        }
    }

    private void UpdateModuleSwitcherDock(string activeId)
    {
        if (ModuleSwitcherDock == null) return;

        ModuleSwitcherDock.Children.Clear();
        var enabledFeatures = _featureNavigator.EnabledFeatures;

        foreach (var feature in enabledFeatures)
        {
            var btn = new Button
            {
                Width = 26,
                Height = 26,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(5),
                Tag = feature.Id,
                Content = new FontIcon
                {
                    Glyph = feature.IconKey,
                    FontSize = 12
                }
            };

            bool isActive = (feature.Id == activeId);
            if (isActive)
            {
                btn.Background = (Brush)Application.Current.Resources["OverlayAccentBrush"];
                btn.Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"];
                btn.BorderThickness = new Thickness(1);
                btn.BorderBrush = (Brush)Application.Current.Resources["OverlayBorderBrush"];
            }
            else
            {
                btn.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                btn.Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"];
                btn.BorderThickness = new Thickness(0);
            }

            ToolTipService.SetToolTip(btn, feature.DisplayName);
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(btn, feature.DisplayName);

            btn.Click += (s, e) =>
            {
                _stateMachine.UserInteracted();
                _isCommandPaletteOpen = false;
                _isSettingsOpen = false;
                if (s is Button clickedBtn && clickedBtn.Tag is string featId)
                {
                    _featureNavigator.NavigateTo(featId);
                    UpdateNavigationUi();
                }
            };

            ModuleSwitcherDock.Children.Add(btn);
        }
    }

    private void RenderPlaceholderModule(FeatureDescriptor feature)
    {
        PlaceholderFeatureIcon.Glyph = feature.IconKey;
        PlaceholderFeatureTitle.Text = feature.DisplayName;

        var module = _featureRegistry.GetModule(feature.Id);
        if (module is PlaceholderFeatureModule placeholderMod)
        {
            PlaceholderMilestoneText.Text = placeholderMod.Milestone;
            PlaceholderScopeText.Text = placeholderMod.PlannedScope;
            PlaceholderDependencyText.Text = $"Prerequisite: {placeholderMod.DependencyNotes}";
        }
        else
        {
            PlaceholderMilestoneText.Text = $"Phase {feature.RoadmapPhase} Roadmap";
            PlaceholderScopeText.Text = feature.Description ?? "Planned multifunction tool.";
            PlaceholderDependencyText.Text = "Deliberate placeholder — No fake live data";
        }
    }

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_stateMachine.CurrentState != OverlayState.Expanded) return;

        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            _stateMachine.UserInteracted();
            if (_isSettingsOpen)
            {
                _isSettingsOpen = false;
                UpdateNavigationUi();
                e.Handled = true;
                return;
            }

            _stateMachine.DismissExplicit();
            e.Handled = true;
            return;
        }

        if (_isSettingsOpen) return;

        if (e.Key == Windows.System.VirtualKey.Left)
        {
            _stateMachine.UserInteracted();
            _featureNavigator.NavigatePrevious();
            e.Handled = true;
        }
        else if (e.Key == Windows.System.VirtualKey.Right)
        {
            _stateMachine.UserInteracted();
            _featureNavigator.NavigateNext();
            e.Handled = true;
        }
    }

    // =========================================================================
    // Focus Timer Module Handlers
    // =========================================================================

    private void OnFocusTimerSnapshotChanged(object? sender, FocusTimerSnapshot e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderFocusTimerSnapshot(e);
        });
    }

    private void RenderFocusTimerSnapshot(FocusTimerSnapshot snapshot)
    {
        FocusCountdownText.Text = snapshot.FormattedRemaining;
        FocusProgressBar.Value = snapshot.ProgressPercentage;
        FocusPlayPauseIcon.Glyph = snapshot.State == FocusTimerState.Running ? "\uE769" : "\uE768"; // Pause vs Play
        FocusModeText.Text = snapshot.Mode switch
        {
            FocusSessionMode.ShortBreak => "Short Break",
            FocusSessionMode.LongBreak => "Long Break",
            _ => "Work Session"
        };
        FocusCompletedText.Text = $"Completed: {snapshot.CompletedSessionsCount}";
        FocusResetButton.IsEnabled = snapshot.CanReset;
        UpdateLiveActivityState();
    }

    private void OnFocusPlayPauseButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        var snapshot = _focusTimerService.CurrentSnapshot;
        if (snapshot.State == FocusTimerState.Running)
        {
            _focusTimerService.Pause();
        }
        else if (snapshot.State == FocusTimerState.Paused)
        {
            _focusTimerService.Resume();
        }
        else
        {
            _focusTimerService.Start();
        }
    }

    private void OnFocusResetButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _focusTimerService.Reset();
    }

    private void OnFocusPreset25mClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (_focusTimerService.AvailablePresets.Count > 0)
        {
            _focusTimerService.SelectPreset(_focusTimerService.AvailablePresets[0]);
        }
    }

    private void OnFocusPreset5mClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (_focusTimerService.AvailablePresets.Count > 1)
        {
            _focusTimerService.SelectPreset(_focusTimerService.AvailablePresets[1]);
        }
    }

    private void OnFocusPreset50mClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (_focusTimerService.AvailablePresets.Count > 2)
        {
            _focusTimerService.SelectPreset(_focusTimerService.AvailablePresets[2]);
        }
    }

    // =========================================================================
    // Quick Launch Module Handlers
    // =========================================================================

    private void OnQuickLaunchExplorerClick(object sender, RoutedEventArgs e) => LaunchQuickShortcut("explorer");
    private void OnQuickLaunchBrowserClick(object sender, RoutedEventArgs e) => LaunchQuickShortcut("browser");
    private void OnQuickLaunchTerminalClick(object sender, RoutedEventArgs e) => LaunchQuickShortcut("terminal");
    private void OnQuickLaunchCalculatorClick(object sender, RoutedEventArgs e) => LaunchQuickShortcut("calculator");
    private void OnQuickLaunchSettingsClick(object sender, RoutedEventArgs e) => LaunchQuickShortcut("settings");
    private void OnQuickLaunchTaskMgrClick(object sender, RoutedEventArgs e) => LaunchQuickShortcut("taskmgr");

    private void LaunchQuickShortcut(string id)
    {
        _stateMachine.UserInteracted();
        var result = _quickLaunchService.Launch(id);
        QuickLaunchStatusText.Text = result.Success
            ? $"Opened {id}"
            : (result.ErrorMessage ?? $"Failed to open {id}");
    }

    // =========================================================================
    // System Glance Module Handlers
    // =========================================================================

    private void OnSystemGlanceSnapshotChanged(object? sender, SystemGlanceSnapshot e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderSystemGlanceSnapshot(e);
        });
    }

    private void RenderSystemGlanceSnapshot(SystemGlanceSnapshot snapshot)
    {
        GlanceTimeText.Text = snapshot.FormattedTime;
        GlanceDateText.Text = snapshot.FormattedDate;
        GlanceBatteryText.Text = snapshot.Battery.StatusText;
        GlanceBatteryIcon.Glyph = snapshot.Battery.IconGlyph;
        GlancePowerSupplyText.Text = snapshot.Battery.HasBattery
            ? (snapshot.Battery.Status == PowerStatusKind.Charging ? "Charging" : "On Battery")
            : "Desktop Power";
        GlanceDeviceNameText.Text = snapshot.MachineName;
        GlanceUptimeText.Text = $"Uptime: {snapshot.UptimeText}";

        if (GlanceNetworkIcon != null) GlanceNetworkIcon.Glyph = snapshot.Network.IconGlyph;
        if (GlanceNetworkNameText != null) GlanceNetworkNameText.Text = snapshot.Network.NetworkName;
        if (GlanceNetworkTypeText != null) GlanceNetworkTypeText.Text = snapshot.Network.ConnectionType;
    }

    // =========================================================================
    // Phase 2 Asynchronous Initialization
    // =========================================================================

    private async Task InitializePhase2ServicesAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        try
        {
            await _spotifyService.InitializeAsync(cancellationToken);
        }
        catch { }

        try
        {
            await _weatherService.InitializeAsync(cancellationToken);
        }
        catch { }

        try
        {
            await _todoService.InitializeAsync(cancellationToken);
        }
        catch { }

        try
        {
            await _calendarService.InitializeAsync(cancellationToken);
        }
        catch { }

        try
        {
            await _notesService.InitializeAsync(cancellationToken);
        }
        catch { }

        try
        {
            await _clipboardService.InitializeAsync(cancellationToken);
        }
        catch { }

        try
        {
            await _gamificationService.InitializeAsync(cancellationToken);
        }
        catch { }

        try
        {
            await _productivityHubService.InitializeAsync(cancellationToken);
        }
        catch { }

        try
        {
            await _personalizationService.InitializeAsync(cancellationToken);
        }
        catch { }

        try
        {
            await _pluginManager.InitializeAsync(cancellationToken);
            var devPlugin = new DeveloperToolkitPlugin(_devUtilsService);
            _pluginManager.RegisterPlugin(devPlugin);
            await _pluginManager.EnablePluginAsync(devPlugin.Manifest.Id);
        }
        catch { }

        try
        {
            await _workflowEngine.InitializeAsync(cancellationToken);
        }
        catch { }

        // Register default health checks
        _diagnosticsService.RegisterHealthCheck("MediaService", () => Task.FromResult(new ComponentHealthCheck
        {
            ComponentName = "MediaService",
            Status = _mediaService.CurrentSnapshot != null ? HealthStatus.Healthy : HealthStatus.Degraded,
            Message = "GSMTC Adapter responsive"
        }));

        _diagnosticsService.RegisterHealthCheck("ToolRegistry", () => Task.FromResult(new ComponentHealthCheck
        {
            ComponentName = "ToolRegistry",
            Status = _toolRegistry.Tools.Count > 0 ? HealthStatus.Healthy : HealthStatus.Degraded,
            Message = $"{_toolRegistry.Tools.Count} tools loaded"
        }));

        _diagnosticsService.RegisterHealthCheck("PluginManager", () => Task.FromResult(new ComponentHealthCheck
        {
            ComponentName = "PluginManager",
            Status = HealthStatus.Healthy,
            Message = $"{_pluginManager.ActivePlugins.Count} active / {_pluginManager.Plugins.Count} registered"
        }));

        _diagnosticsService.RegisterHealthCheck("WorkflowEngine", () => Task.FromResult(new ComponentHealthCheck
        {
            ComponentName = "WorkflowEngine",
            Status = HealthStatus.Healthy,
            Message = $"{_workflowEngine.Workflows.Count} workflows available"
        }));

        RegisterBuiltInCommands();
    }

    // =========================================================================
    // Spotify Module Handlers (Web API + PKCE)
    // =========================================================================

    private void OnSpotifyAuthStateChanged(object? sender, SpotifyAuthState e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderSpotifySnapshot(_spotifyService.CurrentPlayback);
        });
    }

    private void OnSpotifyPlaybackChanged(object? sender, SpotifyPlaybackSnapshot e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderSpotifySnapshot(e);
            if (!_currentSnapshot.HasActiveSession)
            {
                RenderMediaSnapshot(_currentSnapshot);
            }
        });
    }

    private void RenderSpotifySnapshot(SpotifyPlaybackSnapshot playback)
    {
        bool isAuth = _spotifyService.AuthState == SpotifyAuthState.Authenticated;

        SpotifyDisconnectedPanel.Visibility = isAuth ? Visibility.Collapsed : Visibility.Visible;
        SpotifyConnectedPanel.Visibility = isAuth ? Visibility.Visible : Visibility.Collapsed;

        if (!isAuth)
        {
            if (string.IsNullOrEmpty(SpotifyClientIdTextBox.Text) && !string.IsNullOrEmpty(_spotifyService.StoredClientId))
            {
                SpotifyClientIdTextBox.Text = _spotifyService.StoredClientId;
            }
            return;
        }

        var user = _spotifyService.CurrentUser;
        SpotifyUserNameText.Text = user?.DisplayName ?? "Spotify User";
        SpotifyProductText.Text = user?.IsPremium == true ? "Premium" : "Free";

        if (playback.IsActive && playback.Track != null)
        {
            SpotifyTrackTitleText.Text = playback.Track.Name;
            SpotifyTrackArtistText.Text = playback.Track.Artist;
            SpotifyDeviceText.Text = !string.IsNullOrEmpty(playback.DeviceName) ? $"Device: {playback.DeviceName}" : "";
            SpotifyPlayPauseIcon.Glyph = playback.IsPlaying ? "\uE769" : "\uE768";
            SpotifyStatusNoticeText.Text = user?.IsPremium == true ? "" : "Note: Web API control requires Spotify Premium. Media Player controls desktop app.";
        }
        else
        {
            SpotifyTrackTitleText.Text = "No active track";
            SpotifyTrackArtistText.Text = "Play music on any Spotify device";
            SpotifyDeviceText.Text = "";
            SpotifyPlayPauseIcon.Glyph = "\uE768";
            SpotifyStatusNoticeText.Text = "";
        }

        if (playback.AvailableDevices.Count > 1)
        {
            SpotifyDevicesButton.Visibility = Visibility.Visible;
            SpotifyDevicesButton.Content = $"Switch ({playback.AvailableDevices.Count})";
        }
        else
        {
            SpotifyDevicesButton.Visibility = Visibility.Collapsed;
        }

        UpdateLiveActivityState();
    }

    private int _selectedSpotifyDeviceIndex;

    private async void OnSpotifyDevicesButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        var devices = _spotifyService.CurrentPlayback.AvailableDevices;
        if (devices.Count <= 1) return;

        _selectedSpotifyDeviceIndex = (_selectedSpotifyDeviceIndex + 1) % devices.Count;
        var targetDevice = devices[_selectedSpotifyDeviceIndex];
        await _spotifyService.TransferPlaybackAsync(targetDevice.Id);
    }

    private async void OnSpotifyConnectButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        string clientId = SpotifyClientIdTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return;
        }

        SpotifyConnectButton.IsEnabled = false;
        SpotifyConnectButton.Content = "Authorizing...";

        try
        {
            bool ok = await _spotifyService.StartPkceAuthAsync(clientId);
            if (!ok)
            {
                SpotifyConnectButton.Content = "Auth failed — Try again";
            }
        }
        finally
        {
            SpotifyConnectButton.IsEnabled = true;
        }
    }

    private async void OnSpotifyDisconnectButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        await _spotifyService.DisconnectAsync();
    }

    private async void OnSpotifyPlayPauseButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        await _spotifyService.TogglePlayPauseAsync();
        await _spotifyService.RefreshPlaybackAsync();
    }

    private async void OnSpotifyPrevButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        await _spotifyService.SkipPreviousAsync();
        await _spotifyService.RefreshPlaybackAsync();
    }

    private async void OnSpotifyNextButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        await _spotifyService.SkipNextAsync();
        await _spotifyService.RefreshPlaybackAsync();
    }

    // =========================================================================
    // Weather Module Handlers (Open-Meteo)
    // =========================================================================

    private void OnWeatherSnapshotChanged(object? sender, WeatherSnapshot e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderWeatherSnapshot(e);
        });
    }

    private void RenderWeatherSnapshot(WeatherSnapshot snapshot)
    {
        var prefs = _preferencesService.Load();
        bool isFahrenheit = prefs.WeatherUnit == WeatherUnit.Fahrenheit;

        if (snapshot.Location != null)
        {
            WeatherCityText.Text = snapshot.Location.DisplayName;
        }
        else
        {
            WeatherCityText.Text = "No city selected";
        }

        if (snapshot.Current != null)
        {
            WeatherConditionGlyph.Glyph = snapshot.Current.IconGlyph;
            double displayTemp = isFahrenheit
                ? (snapshot.Current.TemperatureC * 9.0 / 5.0 + 32.0)
                : snapshot.Current.TemperatureC;
            string unitSymbol = isFahrenheit ? "°F" : "°C";

            WeatherTemperatureText.Text = $"{displayTemp:0.#}{unitSymbol}";
            WeatherConditionText.Text = snapshot.Current.ConditionText;
            WeatherHumidityText.Text = $"Humidity: {snapshot.Current.RelativeHumidity}%";
            WeatherPrecipText.Text = $"Rain: {snapshot.Current.PrecipitationMm:0.#}mm";
        }
        else
        {
            WeatherConditionGlyph.Glyph = "\uE753";
            WeatherTemperatureText.Text = isFahrenheit ? "--°F" : "--°C";
            WeatherConditionText.Text = snapshot.StatusMessage ?? "Loading...";
            WeatherHumidityText.Text = "Humidity: --%";
            WeatherPrecipText.Text = "Rain: --mm";
        }

        string timeStr = snapshot.LastUpdated?.LocalDateTime.ToString("h:mm tt") ?? "Recently";
        WeatherLastUpdatedText.Text = snapshot.IsOffline
            ? $"Updated: {timeStr} (Offline/Cached)"
            : $"Updated: {timeStr}";

        var forecasts = snapshot.Forecast;
        if (forecasts.Count > 0)
        {
            WeatherDay1Name.Text = forecasts[0].Date.ToString("ddd");
            WeatherDay1Glyph.Glyph = forecasts[0].IconGlyph;
            double t1 = isFahrenheit ? (forecasts[0].MaxTempC * 9.0 / 5.0 + 32.0) : forecasts[0].MaxTempC;
            WeatherDay1Temp.Text = $"{t1:0}°";
        }
        if (forecasts.Count > 1)
        {
            WeatherDay2Name.Text = forecasts[1].Date.ToString("ddd");
            WeatherDay2Glyph.Glyph = forecasts[1].IconGlyph;
            double t2 = isFahrenheit ? (forecasts[1].MaxTempC * 9.0 / 5.0 + 32.0) : forecasts[1].MaxTempC;
            WeatherDay2Temp.Text = $"{t2:0}°";
        }
        if (forecasts.Count > 2)
        {
            WeatherDay3Name.Text = forecasts[2].Date.ToString("ddd");
            WeatherDay3Glyph.Glyph = forecasts[2].IconGlyph;
            double t3 = isFahrenheit ? (forecasts[2].MaxTempC * 9.0 / 5.0 + 32.0) : forecasts[2].MaxTempC;
            WeatherDay3Temp.Text = $"{t3:0}°";
        }
    }

    private async void OnWeatherSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            _stateMachine.UserInteracted();
            e.Handled = true;
            await ExecuteWeatherSearchAsync();
        }
    }

    private async void OnWeatherSearchButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        await ExecuteWeatherSearchAsync();
    }

    private async Task ExecuteWeatherSearchAsync()
    {
        string query = WeatherSearchBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            WeatherSearchResultsBorder.Visibility = Visibility.Collapsed;
            WeatherMainCard.Visibility = Visibility.Visible;
            return;
        }

        var results = await _weatherService.SearchLocationsAsync(query);
        WeatherSearchResultsPanel.Children.Clear();

        if (results.Count == 0)
        {
            var noResultsText = new TextBlock
            {
                Text = "No locations found.",
                FontSize = 10,
                Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextTertiaryBrush"],
                Margin = new Thickness(8, 4, 8, 4)
            };
            WeatherSearchResultsPanel.Children.Add(noResultsText);
        }
        else
        {
            foreach (var loc in results)
            {
                var locBtn = new Button
                {
                    Content = loc.DisplayName,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Background = (SolidColorBrush)Application.Current.Resources["OverlaySurfaceBrush"],
                    Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextPrimaryBrush"],
                    BorderThickness = new Thickness(0),
                    FontSize = 10,
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(0, 1, 0, 1),
                    CornerRadius = new CornerRadius(4)
                };

                locBtn.Click += async (s, args) =>
                {
                    _stateMachine.UserInteracted();
                    WeatherSearchResultsBorder.Visibility = Visibility.Collapsed;
                    WeatherMainCard.Visibility = Visibility.Visible;
                    WeatherSearchBox.Text = string.Empty;
                    await _weatherService.SetLocationAsync(loc);
                };

                WeatherSearchResultsPanel.Children.Add(locBtn);
            }
        }

        WeatherSearchResultsBorder.Visibility = Visibility.Visible;
        WeatherMainCard.Visibility = Visibility.Collapsed;
    }

    private async void OnWeatherRefreshButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        WeatherSearchResultsBorder.Visibility = Visibility.Collapsed;
        WeatherMainCard.Visibility = Visibility.Visible;
        await _weatherService.RefreshAsync(force: true);
    }

    // =========================================================================
    // To-do List Module Handlers (Task CRUD & Local Persistence)
    // =========================================================================

    private void OnTodoItemsChanged(object? sender, IReadOnlyList<TodoItem> e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderTodoList(e);
        });
    }

    private void RenderTodoList(IReadOnlyList<TodoItem> items)
    {
        TodoListContainer.Children.Clear();

        if (items.Count == 0)
        {
            TodoEmptyNotice.Visibility = Visibility.Visible;
            TodoListContainer.Children.Add(TodoEmptyNotice);
        }
        else
        {
            TodoEmptyNotice.Visibility = Visibility.Collapsed;

            foreach (var item in items)
            {
                var itemGrid = new Grid
                {
                    Margin = new Thickness(0, 1, 0, 1)
                };
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // Checkbox
                var check = new CheckBox
                {
                    IsChecked = item.IsCompleted,
                    MinWidth = 0,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0, 0, 6, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                check.Click += async (s, e) =>
                {
                    _stateMachine.UserInteracted();
                    await _todoService.ToggleCompleteAsync(item.Id);
                };
                Grid.SetColumn(check, 0);
                itemGrid.Children.Add(check);

                // Title
                var titleText = new TextBlock
                {
                    Text = item.Title,
                    FontSize = 11,
                    Foreground = item.IsCompleted
                        ? (SolidColorBrush)Application.Current.Resources["OverlayTextTertiaryBrush"]
                        : (SolidColorBrush)Application.Current.Resources["OverlayTextPrimaryBrush"],
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };
                if (item.IsCompleted)
                {
                    titleText.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough;
                }
                Grid.SetColumn(titleText, 1);
                itemGrid.Children.Add(titleText);

                // Priority Badge (only if High)
                if (item.Priority == TodoPriority.High)
                {
                    var prioBorder = new Border
                    {
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 75, 75)),
                        BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(120, 255, 75, 75)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(4, 0, 4, 0),
                        Margin = new Thickness(4, 0, 4, 0),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    prioBorder.Child = new TextBlock
                    {
                        Text = "High",
                        FontSize = 8,
                        Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 100, 100))
                    };
                    Grid.SetColumn(prioBorder, 2);
                    itemGrid.Children.Add(prioBorder);
                }

                // Delete Button
                var delBtn = new Button
                {
                    Width = 20,
                    Height = 20,
                    MinWidth = 0,
                    MinHeight = 0,
                    Padding = new Thickness(0),
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
                    BorderThickness = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                delBtn.Content = new FontIcon
                {
                    Glyph = "\uE74D",
                    FontSize = 9,
                    Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextTertiaryBrush"]
                };
                delBtn.Click += async (s, e) =>
                {
                    _stateMachine.UserInteracted();
                    await _todoService.DeleteItemAsync(item.Id);
                };
                Grid.SetColumn(delBtn, 3);
                itemGrid.Children.Add(delBtn);

                TodoListContainer.Children.Add(itemGrid);
            }
        }

        TodoSummaryText.Text = $"{_todoService.ActiveCount} active tasks ({_todoService.CompletedCount} done)";
    }

    private async void OnTodoNewTaskBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            _stateMachine.UserInteracted();
            e.Handled = true;
            await AddTodoTaskFromInputAsync();
        }
    }

    private async void OnTodoAddButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        await AddTodoTaskFromInputAsync();
    }

    private async Task AddTodoTaskFromInputAsync()
    {
        string text = TodoNewTaskBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        int idx = TodoPriorityComboBox.SelectedIndex;
        TodoPriority prio = idx switch
        {
            0 => TodoPriority.Low,
            2 => TodoPriority.High,
            _ => TodoPriority.Normal
        };

        await _todoService.AddItemAsync(text, prio);
        TodoNewTaskBox.Text = string.Empty;
    }

    private async void OnTodoClearDoneButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        await _todoService.ClearCompletedAsync();
    }

    // =========================================================================
    // Stopwatch Module Handlers
    // =========================================================================

    private void OnStopwatchSnapshotChanged(object? sender, StopwatchSnapshot e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderStopwatchSnapshot(e);
        });
    }

    private void RenderStopwatchSnapshot(StopwatchSnapshot snapshot)
    {
        StopwatchElapsedText.Text = snapshot.FormattedElapsed;
        StopwatchPlayPauseIcon.Glyph = snapshot.IsRunning ? "\uE769" : "\uE768";
        StopwatchStatusText.Text = snapshot.IsRunning ? "Running" : (snapshot.Elapsed > TimeSpan.Zero ? "Paused" : "Ready");
        StopwatchLapCountText.Text = $"Laps: {snapshot.Laps.Count}";

        StopwatchResetButton.IsEnabled = !snapshot.IsRunning && snapshot.Elapsed > TimeSpan.Zero;
        StopwatchLapButton.IsEnabled = snapshot.IsRunning;

        if (snapshot.Laps.Count > 0)
        {
            var lastLap = snapshot.Laps[^1];
            int m = (int)lastLap.TotalMinutes;
            int s = lastLap.Seconds;
            int ms = lastLap.Milliseconds / 100;
            StopwatchLapsSummaryText.Text = $"Lap {snapshot.Laps.Count}: {m:D2}:{s:D2}.{ms} (Total Laps: {snapshot.Laps.Count})";
        }
        else
        {
            StopwatchLapsSummaryText.Text = snapshot.IsRunning 
                ? "Stopwatch running. Press Lap to record split." 
                : "No laps recorded. Press Lap while running.";
        }
    }

    private void OnStopwatchPlayPauseButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (_stopwatchService.CurrentSnapshot.IsRunning)
        {
            _stopwatchService.Stop();
        }
        else
        {
            _stopwatchService.Start();
        }
    }

    private void OnStopwatchResetButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _stopwatchService.Reset();
    }

    private void OnStopwatchLapButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _stopwatchService.Lap();
    }

    // =========================================================================
    // Calendar & Upcoming Events Module Handlers
    // =========================================================================

    private void OnCalendarGlanceChanged(object? sender, CalendarGlance e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderCalendarGlance(e);
        });
    }

    private void RenderCalendarGlance(CalendarGlance glance)
    {
        CalendarNextCountdownText.Text = glance.NextEventCountdown ?? "No upcoming events";

        CalendarEventsContainer.Children.Clear();
        var allEvents = glance.TodayEvents.Concat(glance.UpcomingEvents).DistinctBy(e => e.Id).ToList();

        if (allEvents.Count == 0)
        {
            CalendarEmptyNotice.Visibility = Visibility.Visible;
            CalendarEventsContainer.Children.Add(CalendarEmptyNotice);
        }
        else
        {
            CalendarEmptyNotice.Visibility = Visibility.Collapsed;
            foreach (var evt in allEvents.Take(6))
            {
                var rowGrid = new Grid
                {
                    Margin = new Thickness(0, 1, 0, 1)
                };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var infoStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                infoStack.Children.Add(new TextBlock
                {
                    Text = evt.Title,
                    FontSize = 10,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextPrimaryBrush"],
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                infoStack.Children.Add(new TextBlock
                {
                    Text = $"{evt.FormattedDate} • {evt.FormattedTimeRange}{(string.IsNullOrEmpty(evt.Location) ? "" : $" ({evt.Location})")}",
                    FontSize = 8,
                    Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextSecondaryBrush"],
                    TextTrimming = TextTrimming.CharacterEllipsis
                });
                Grid.SetColumn(infoStack, 0);
                rowGrid.Children.Add(infoStack);

                if (evt.IsAllDay)
                {
                    var allDayBadge = new Border
                    {
                        Background = (SolidColorBrush)Application.Current.Resources["OverlayBorderBrush"],
                        CornerRadius = new CornerRadius(3),
                        Padding = new Thickness(4, 0, 4, 0),
                        Margin = new Thickness(4, 0, 4, 0),
                        VerticalAlignment = VerticalAlignment.Center,
                        Child = new TextBlock
                        {
                            Text = "All Day",
                            FontSize = 8,
                            Foreground = (SolidColorBrush)Application.Current.Resources["OverlayAccentBrush"]
                        }
                    };
                    Grid.SetColumn(allDayBadge, 1);
                    rowGrid.Children.Add(allDayBadge);
                }

                var delBtn = new Button
                {
                    Width = 20,
                    Height = 20,
                    MinWidth = 0,
                    MinHeight = 0,
                    Padding = new Thickness(0),
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
                    BorderThickness = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Content = new FontIcon
                    {
                        Glyph = "\uE74D",
                        FontSize = 9,
                        Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextTertiaryBrush"]
                    }
                };
                delBtn.Click += async (s, e) =>
                {
                    _stateMachine.UserInteracted();
                    await _calendarService.DeleteEventAsync(evt.Id);
                };
                Grid.SetColumn(delBtn, 2);
                rowGrid.Children.Add(delBtn);

                CalendarEventsContainer.Children.Add(rowGrid);
            }
        }

        CalendarSummaryText.Text = $"{glance.TodayEvents.Count} event(s) today";
    }

    private async void OnCalendarAddEventButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        var start = DateTimeOffset.Now.AddHours(1);
        start = new DateTimeOffset(start.Year, start.Month, start.Day, start.Hour, 0, 0, start.Offset);
        var end = start.AddHours(1);

        await _calendarService.AddEventAsync(
            title: "New Event",
            startTime: start,
            endTime: end,
            location: "Office / Remote");
    }

    private async void OnCalendarImportIcsButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        string appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicTopBar");
        string importPath = Path.Combine(appDir, "import.ics");

        if (File.Exists(importPath))
        {
            try
            {
                string ics = await File.ReadAllTextAsync(importPath);
                await _calendarService.ImportIcsAsync(ics);
            }
            catch { }
        }
        else
        {
            var start = DateTimeOffset.Now.AddDays(1);
            await _calendarService.AddEventAsync(
                title: "Imported Demo Event",
                startTime: start,
                endTime: start.AddHours(1),
                location: "Online Meeting");
        }
    }

    private async void OnCalendarExportIcsButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        try
        {
            string ics = _calendarService.ExportToIcs();
            string appDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DynamicTopBar");
            Directory.CreateDirectory(appDir);
            string exportPath = Path.Combine(appDir, "calendar_export.ics");
            await File.WriteAllTextAsync(exportPath, ics);
        }
        catch { }
    }

    // =========================================================================
    // Quick Notes Module Handlers
    // =========================================================================

    private void OnNotesChanged(object? sender, IReadOnlyList<NoteItem> notes)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderNotes(notes, _notesService.SelectedNote);
        });
    }

    private void OnSelectedNoteChanged(object? sender, NoteItem? note)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            SyncSelectedNote(note);
        });
    }

    private void RenderNotes(IReadOnlyList<NoteItem> notes, NoteItem? selected)
    {
        SyncSelectedNote(selected);
        NoteStatusText.Text = notes.Count > 0 
            ? $"Note 1 of {notes.Count} (Autosaved)"
            : "No notes (Type to create)";
    }

    private void SyncSelectedNote(NoteItem? note)
    {
        _isUpdatingNoteUi = true;
        try
        {
            if (note != null)
            {
                NoteTitleBox.Text = note.Title;
                NoteContentBox.Text = note.Content;
                NotePinIcon.Glyph = note.IsPinned ? "\uE840" : "\uE718";
                NotePinIcon.Foreground = note.IsPinned 
                    ? (SolidColorBrush)Application.Current.Resources["OverlayAccentBrush"]
                    : (SolidColorBrush)Application.Current.Resources["OverlayTextSecondaryBrush"];
            }
            else
            {
                NoteTitleBox.Text = string.Empty;
                NoteContentBox.Text = string.Empty;
                NotePinIcon.Glyph = "\uE718";
                NotePinIcon.Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextSecondaryBrush"];
            }
        }
        finally
        {
            _isUpdatingNoteUi = false;
        }
    }

    private void OnNoteTitleChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingNoteUi) return;
        _stateMachine.UserInteracted();
        ScheduleNoteAutosave();
    }

    private void OnNoteContentChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingNoteUi) return;
        _stateMachine.UserInteracted();
        ScheduleNoteAutosave();
    }

    private void ScheduleNoteAutosave()
    {
        _notesAutosaveTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _notesAutosaveTimer.Stop();
        _notesAutosaveTimer.Tick -= OnNotesAutosaveTick;
        _notesAutosaveTimer.Tick += OnNotesAutosaveTick;
        _notesAutosaveTimer.Start();
    }

    private async void OnNotesAutosaveTick(object? sender, object e)
    {
        _notesAutosaveTimer?.Stop();
        var selected = _notesService.SelectedNote;
        string title = NoteTitleBox.Text.Trim();
        string content = NoteContentBox.Text;

        if (selected != null)
        {
            await _notesService.UpdateNoteAsync(selected.Id, title, content);
        }
        else if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(content))
        {
            await _notesService.CreateNoteAsync(string.IsNullOrWhiteSpace(title) ? "New Note" : title, content);
        }
    }

    private async void OnNotePinButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        var selected = _notesService.SelectedNote;
        if (selected != null)
        {
            await _notesService.TogglePinAsync(selected.Id);
        }
    }

    private async void OnNoteNewButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        await _notesService.CreateNoteAsync("New Note", "");
    }

    private void OnNoteCycleListClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        var all = _notesService.Notes;
        if (all.Count <= 1) return;

        var current = _notesService.SelectedNote;
        int idx = current == null ? 0 : all.ToList().FindIndex(n => n.Id == current.Id);
        int next = (idx + 1) % all.Count;
        _notesService.SelectNote(all[next].Id);
    }

    private async void OnNoteDeleteButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        var selected = _notesService.SelectedNote;
        if (selected != null)
        {
            await _notesService.DeleteNoteAsync(selected.Id);
        }
    }

    // =========================================================================
    // Clipboard History Module Handlers (Strict Opt-In Privacy)
    // =========================================================================

    private void OnClipboardStateChanged(object? sender, ClipboardState e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderClipboard(e, _clipboardService.Snippets);
        });
    }

    private void OnClipboardSnippetsChanged(object? sender, IReadOnlyList<ClipboardSnippet> e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderClipboard(_clipboardService.State, e);
        });
    }

    private void RenderClipboard(ClipboardState state, IReadOnlyList<ClipboardSnippet> snippets)
    {
        if (!state.IsOptInEnabled)
        {
            ClipboardOptInPanel.Visibility = Visibility.Visible;
            ClipboardActivePanel.Visibility = Visibility.Collapsed;
            return;
        }

        ClipboardOptInPanel.Visibility = Visibility.Collapsed;
        ClipboardActivePanel.Visibility = Visibility.Visible;

        ClipboardStatusText.Text = state.StatusText;
        ClipboardPauseButton.Content = state.IsMonitoring ? "Pause" : "Resume";

        ClipboardSnippetsContainer.Children.Clear();
        if (snippets.Count == 0)
        {
            ClipboardEmptyNotice.Visibility = Visibility.Visible;
            ClipboardSnippetsContainer.Children.Add(ClipboardEmptyNotice);
        }
        else
        {
            ClipboardEmptyNotice.Visibility = Visibility.Collapsed;
            foreach (var snippet in snippets.Take(12))
            {
                var rowGrid = new Grid
                {
                    Margin = new Thickness(0, 1, 0, 1)
                };
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var copyBtn = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Background = (SolidColorBrush)Application.Current.Resources["OverlaySurfaceBrush"],
                    BorderBrush = (SolidColorBrush)Application.Current.Resources["OverlayBorderBrush"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 3, 6, 3)
                };
                copyBtn.Content = new TextBlock
                {
                    Text = snippet.PreviewText,
                    FontSize = 9,
                    Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextPrimaryBrush"],
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                copyBtn.Click += async (s, e) =>
                {
                    _stateMachine.UserInteracted();
                    await _clipboardService.CopyToClipboardAsync(snippet.Id);
                };
                Grid.SetColumn(copyBtn, 0);
                rowGrid.Children.Add(copyBtn);

                var pinBtn = new Button
                {
                    Width = 20,
                    Height = 20,
                    MinWidth = 0,
                    MinHeight = 0,
                    Padding = new Thickness(0),
                    Margin = new Thickness(3, 0, 0, 0),
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
                    BorderThickness = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Content = new FontIcon
                    {
                        Glyph = snippet.IsPinned ? "\uE840" : "\uE718",
                        FontSize = 9,
                        Foreground = snippet.IsPinned 
                            ? (SolidColorBrush)Application.Current.Resources["OverlayAccentBrush"]
                            : (SolidColorBrush)Application.Current.Resources["OverlayTextTertiaryBrush"]
                    }
                };
                pinBtn.Click += (s, e) =>
                {
                    _stateMachine.UserInteracted();
                    _clipboardService.TogglePin(snippet.Id);
                };
                Grid.SetColumn(pinBtn, 1);
                rowGrid.Children.Add(pinBtn);

                var delBtn = new Button
                {
                    Width = 20,
                    Height = 20,
                    MinWidth = 0,
                    MinHeight = 0,
                    Padding = new Thickness(0),
                    Margin = new Thickness(2, 0, 0, 0),
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
                    BorderThickness = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center,
                    Content = new FontIcon
                    {
                        Glyph = "\uE74D",
                        FontSize = 9,
                        Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextTertiaryBrush"]
                    }
                };
                delBtn.Click += (s, e) =>
                {
                    _stateMachine.UserInteracted();
                    _clipboardService.DeleteSnippet(snippet.Id);
                };
                Grid.SetColumn(delBtn, 2);
                rowGrid.Children.Add(delBtn);

                ClipboardSnippetsContainer.Children.Add(rowGrid);
            }
        }
    }

    private void OnClipboardEnableOptInClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _clipboardService.SetOptIn(true);
        var current = _preferencesService.Load();
        _preferencesService.Save(current with { ClipboardOptIn = true });
        PrefClipboardOptInSettingCheck.IsChecked = true;
    }

    private void OnClipboardPauseToggleClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (_clipboardService.State.IsMonitoring)
        {
            _clipboardService.PauseMonitoring();
        }
        else
        {
            _clipboardService.ResumeMonitoring();
        }
    }

    private void OnClipboardClearHistoryClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _clipboardService.ClearHistory();
    }

    // =========================================================================
    // Reminders & Personalization Handlers
    // =========================================================================

    private void OnReminderFired(object? sender, AppReminder reminder)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            AppSourceText.Text = $"🔔 {reminder.Title}";
            _notificationService.PostNotification(new AppNotificationItem
            {
                SourceApp = "Reminders",
                Title = reminder.Title,
                Message = reminder.DueTime.ToString("t"),
                Priority = NotificationPriority.High
            });
        });
    }

    private void OnPersonalizationSettingChanged(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        var current = _preferencesService.Load();
        bool use24 = Pref24HourClockCheck.IsChecked == true;
        var unit = PrefFahrenheitCheck.IsChecked == true ? WeatherUnit.Fahrenheit : WeatherUnit.Celsius;
        bool reminders = PrefRemindersCheck.IsChecked == true;
        bool clipOptIn = PrefClipboardOptInSettingCheck.IsChecked == true;
        bool privacyMode = PrefNotificationPrivacyCheck.IsChecked == true;

        var updated = current with
        {
            ClockUse24HourFormat = use24,
            WeatherUnit = unit,
            RemindersEnabled = reminders,
            ClipboardOptIn = clipOptIn
        };
        _preferencesService.Save(updated);

        UpdateClock();
        RenderWeatherSnapshot(_weatherService.CurrentSnapshot);
        _clipboardService.SetOptIn(clipOptIn);
        RenderClipboard(_clipboardService.State, _clipboardService.Snippets);
        _notificationService.UpdatePreferences(new NotificationPreferences { PrivacyMode = privacyMode });
    }

    private void OnResetShortcutsClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _shortcutService.ResetToDefaults();
    }

    private async void OnClearAllAppDataClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        try
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DynamicTopBar");
            if (Directory.Exists(appData))
            {
                foreach (var file in Directory.GetFiles(appData))
                {
                    try { File.Delete(file); } catch { }
                }
            }
        }
        catch { }

        await _todoService.ClearCompletedAsync();
        _clipboardService.ClearHistory();
        _ = _calendarService.InitializeAsync();
        _ = _notesService.InitializeAsync();
    }

    public void ShowOverlay()
    {
        IntPtr previousForeground = Win32Interop.GetForegroundWindow();

        if (_isConcealed)
        {
            RevealBarFromConcealed();
        }

        this.Activate();
        Win32Interop.ShowWindow(_hwnd, Win32Interop.SW_SHOWNOACTIVATE);

        if (previousForeground != IntPtr.Zero && previousForeground != _hwnd)
        {
            Win32Interop.SetForegroundWindow(previousForeground);
        }

        if (_isAutoHideEnabled && _stateMachine.CurrentState == OverlayState.Idle && !_isPointerOver)
        {
            _autoHideTimer?.Stop();
            _autoHideTimer?.Start();
        }
    }

    public void HideOverlay()
    {
        Win32Interop.ShowWindow(_hwnd, Win32Interop.SW_HIDE);
    }

    public void ToggleOverlayVisibility()
    {
        if (IsOverlayVisible)
        {
            HideOverlay();
        }
        else
        {
            ShowOverlay();
        }
    }

    private void ConcealBarToTrigger()
    {
        if (_isDisposed || _isConcealed || !_isAutoHideEnabled) return;
        if (_stateMachine.CurrentState != OverlayState.Idle) return;
        if (_isSettingsOpen || _isCommandPaletteOpen) return;

        if (SlideUpStoryboard != null)
        {
            SlideUpStoryboard.Begin();
        }
        else
        {
            FinishConceal();
        }
    }

    private void OnSlideUpStoryboardCompleted(object? sender, object e)
    {
        FinishConceal();
    }

    private void FinishConceal()
    {
        if (_isDisposed || !_isAutoHideEnabled) return;
        _isConcealed = true;
        MainContainer.Visibility = Visibility.Collapsed;
        TopTriggerHandle.Visibility = Visibility.Visible;
        PositionAndSizeWindow(140, 8, customTopInset: 0, cornerRadiusDip: 4);
    }

    private void RevealBarFromConcealed()
    {
        if (_isDisposed || !_isConcealed) return;
        _isConcealed = false;
        _autoHideTimer?.Stop();

        TopTriggerHandle.Visibility = Visibility.Collapsed;
        MainContainer.Visibility = Visibility.Visible;
        MainContainer.CornerRadius = new CornerRadius(22);
        PositionAndSizeWindow(340, 44, customTopInset: 8, cornerRadiusDip: 22);
        UpdateLiveActivityState();

        SlideDownStoryboard?.Begin();
    }

    private void OnTriggerHandlePointerEntered(object sender, PointerRoutedEventArgs e)
    {
        TopTriggerHandle.Opacity = 1.0;
        RevealBarFromConcealed();
    }

    private void OnTriggerHandlePointerExited(object sender, PointerRoutedEventArgs e)
    {
        TopTriggerHandle.Opacity = 0.85;
    }

    private void OnTriggerHandleTapped(object sender, TappedRoutedEventArgs e)
    {
        RevealBarFromConcealed();
    }

    private void OnPinButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _isAutoHideEnabled = !_isAutoHideEnabled;

        var currentPrefs = _preferencesService.Load();
        var newPrefs = currentPrefs with { AutoHideEnabled = _isAutoHideEnabled };
        _settingsService.SavePreferences(newPrefs);

        UpdatePinButtonUi();

        if (!_isAutoHideEnabled)
        {
            _autoHideTimer?.Stop();
            if (_isConcealed)
            {
                RevealBarFromConcealed();
            }
        }
        else
        {
            if (_stateMachine.CurrentState == OverlayState.Idle && !_isPointerOver)
            {
                _autoHideTimer?.Stop();
                _autoHideTimer?.Start();
            }
        }
    }

    private void UpdatePinButtonUi()
    {
        if (PinIcon == null || PinButton == null) return;

        if (_isAutoHideEnabled)
        {
            PinIcon.Glyph = "\uE718"; // Outline / unpinned pin
            PinIcon.Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"];
            ToolTipService.SetToolTip(PinButton, "Mode: Sembunyi Otomatis (Klik untuk Sematkan di Layar)");
        }
        else
        {
            PinIcon.Glyph = "\uE840"; // Solid / pinned pin
            PinIcon.Foreground = (Brush)Application.Current.Resources["OverlayAccentBrush"];
            ToolTipService.SetToolTip(PinButton, "Mode: Selalu Muncul (Klik untuk Sembunyi Otomatis)");
        }
    }

    private void OnAutoHideTimerTick(object? sender, object e)
    {
        _autoHideTimer?.Stop();
        if (_isAutoHideEnabled && _stateMachine.CurrentState == OverlayState.Idle && !_isPointerOver && !_isInputFocused)
        {
            ConcealBarToTrigger();
        }
    }

    private void OnShortcutTriggered(object? sender, string actionId)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ExecuteAction(actionId);
        });
    }

    public void ExecuteAction(string actionId)
    {
        switch (actionId)
        {
            case "open-command-palette":
                ToggleCommandPalette();
                break;

            case "toggle-overlay-visibility":
                ToggleOverlayVisibility();
                break;

            case "toggle-panel-expansion":
                if (_stateMachine.CurrentState == OverlayState.Expanded)
                {
                    _stateMachine.ForceIdle();
                }
                else
                {
                    _stateMachine.ForceExpand();
                }
                break;

            case "navigate-next-feature":
                _featureNavigator.NavigateNext();
                if (_stateMachine.CurrentState != OverlayState.Expanded)
                {
                    _stateMachine.ForceExpand();
                }
                break;

            case "navigate-prev-feature":
                _featureNavigator.NavigatePrevious();
                if (_stateMachine.CurrentState != OverlayState.Expanded)
                {
                    _stateMachine.ForceExpand();
                }
                break;

            case "media-play-pause":
                _ = _mediaService.TogglePlayPauseAsync();
                break;

            case "focus-start-pause":
                if (_focusTimerService.CurrentSnapshot.State == FocusTimerState.Running)
                {
                    _focusTimerService.Pause();
                }
                else
                {
                    _focusTimerService.Start();
                }
                break;

            case "open-quick-notes":
                _featureNavigator.NavigateTo("notes");
                if (_stateMachine.CurrentState != OverlayState.Expanded)
                {
                    _stateMachine.ForceExpand();
                }
                break;

            case "open-todo-list":
                _featureNavigator.NavigateTo("todo");
                if (_stateMachine.CurrentState != OverlayState.Expanded)
                {
                    _stateMachine.ForceExpand();
                }
                break;

            case "open-spotify":
                _featureNavigator.NavigateTo("spotify");
                if (_stateMachine.CurrentState != OverlayState.Expanded)
                {
                    _stateMachine.ForceExpand();
                }
                break;

            case "open-weather":
                _featureNavigator.NavigateTo("weather");
                if (_stateMachine.CurrentState != OverlayState.Expanded)
                {
                    _stateMachine.ForceExpand();
                }
                break;

            case "open-calendar":
                _featureNavigator.NavigateTo("calendar");
                if (_stateMachine.CurrentState != OverlayState.Expanded)
                {
                    _stateMachine.ForceExpand();
                }
                break;

            case "open-system-glance":
                _featureNavigator.NavigateTo("system-glance");
                if (_stateMachine.CurrentState != OverlayState.Expanded)
                {
                    _stateMachine.ForceExpand();
                }
                break;

            case "open-clipboard":
                _featureNavigator.NavigateTo("clipboard");
                if (_stateMachine.CurrentState != OverlayState.Expanded)
                {
                    _stateMachine.ForceExpand();
                }
                break;

            case "open-productivity-hub":
                _isCommandPaletteOpen = false;
                _isSettingsOpen = false;
                _featureNavigator.NavigateTo("productivity");
                if (_stateMachine.CurrentState != OverlayState.Expanded)
                {
                    _stateMachine.ForceExpand();
                }
                UpdateNavigationUi();
                break;

            case "open-tools":
            case "open-tools-engine":
                _isCommandPaletteOpen = false;
                _isSettingsOpen = false;
                _featureNavigator.NavigateTo("tools");
                if (_stateMachine.CurrentState != OverlayState.Expanded)
                {
                    _stateMachine.ForceExpand();
                }
                UpdateNavigationUi();
                break;
        }
    }

    public void ToggleCommandPalette()
    {
        OpenCommandPaletteWindow();
    }

    private int _selectedCommandIndex = 0;
    private IReadOnlyList<PaletteCommand> _currentCommandResults = Array.Empty<PaletteCommand>();

    private void OnCommandSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        string query = CommandSearchTextBox.Text ?? string.Empty;
        UpdateCommandSearchResults(query);
    }

    private void UpdateCommandSearchResults(string query)
    {
        string? activeModuleId = _featureNavigator.CurrentFeature?.Id;
        _currentCommandResults = _commandPaletteService.Search(query, activeModuleId);
        _selectedCommandIndex = 0;
        RenderCommandResults();
    }

    private void RenderCommandResults()
    {
        CommandResultsContainer.Children.Clear();

        if (_currentCommandResults.Count == 0)
        {
            var emptyNotice = new TextBlock
            {
                Text = "No matching commands found.",
                FontSize = 10,
                Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextTertiaryBrush"],
                Margin = new Thickness(8, 4, 8, 4)
            };
            CommandResultsContainer.Children.Add(emptyNotice);
            return;
        }

        for (int i = 0; i < _currentCommandResults.Count; i++)
        {
            var cmd = _currentCommandResults[i];
            bool isSelected = i == _selectedCommandIndex;

            var itemBtn = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = isSelected 
                    ? (SolidColorBrush)Application.Current.Resources["OverlaySurfaceBrush"] 
                    : new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderBrush = isSelected 
                    ? (SolidColorBrush)Application.Current.Resources["OverlayAccentBrush"] 
                    : (SolidColorBrush)Application.Current.Resources["OverlayBorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 1, 0, 1)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var icon = new FontIcon
            {
                Glyph = cmd.IconGlyph,
                FontSize = 11,
                Foreground = (SolidColorBrush)Application.Current.Resources["OverlayAccentBrush"],
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon, 0);
            grid.Children.Add(icon);

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var titleText = new TextBlock
            {
                Text = cmd.Title,
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextPrimaryBrush"],
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            textStack.Children.Add(titleText);

            if (!string.IsNullOrWhiteSpace(cmd.Description))
            {
                var descText = new TextBlock
                {
                    Text = cmd.Description,
                    FontSize = 8,
                    Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextTertiaryBrush"],
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                textStack.Children.Add(descText);
            }
            Grid.SetColumn(textStack, 1);
            grid.Children.Add(textStack);

            var catBorder = new Border
            {
                Background = (SolidColorBrush)Application.Current.Resources["OverlaySurfaceBrush"],
                BorderBrush = (SolidColorBrush)Application.Current.Resources["OverlayBorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(6, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            var catText = new TextBlock
            {
                Text = cmd.Category,
                FontSize = 8,
                Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextSecondaryBrush"]
            };
            catBorder.Child = catText;
            Grid.SetColumn(catBorder, 2);
            grid.Children.Add(catBorder);

            if (!string.IsNullOrWhiteSpace(cmd.ShortcutHint))
            {
                var shortcutText = new TextBlock
                {
                    Text = cmd.ShortcutHint,
                    FontSize = 8,
                    Foreground = (SolidColorBrush)Application.Current.Resources["OverlayTextTertiaryBrush"],
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(shortcutText, 3);
                grid.Children.Add(shortcutText);
            }

            itemBtn.Content = grid;
            itemBtn.Click += async (s, e) =>
            {
                _stateMachine.UserInteracted();
                await ExecuteCommandResultAsync(cmd);
            };

            CommandResultsContainer.Children.Add(itemBtn);
        }
    }

    private void OnCommandSearchKeyDown(object sender, KeyRoutedEventArgs e)
    {
        _stateMachine.UserInteracted();

        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            _isCommandPaletteOpen = false;
            UpdateNavigationUi();
            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Down)
        {
            if (_currentCommandResults.Count > 0)
            {
                _selectedCommandIndex = (_selectedCommandIndex + 1) % _currentCommandResults.Count;
                RenderCommandResults();
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Up)
        {
            if (_currentCommandResults.Count > 0)
            {
                _selectedCommandIndex = (_selectedCommandIndex - 1 + _currentCommandResults.Count) % _currentCommandResults.Count;
                RenderCommandResults();
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (_currentCommandResults.Count > 0 && _selectedCommandIndex < _currentCommandResults.Count)
            {
                var selectedCmd = _currentCommandResults[_selectedCommandIndex];
                _ = ExecuteCommandResultAsync(selectedCmd);
                e.Handled = true;
            }
        }
    }

    private async Task ExecuteCommandResultAsync(PaletteCommand command)
    {
        _isCommandPaletteOpen = false;
        UpdateNavigationUi();
        await _commandPaletteService.ExecuteCommandAsync(command);
    }

    private Flyout? _activeNotificationFlyout;

    private void OnActiveNotificationChanged(object? sender, AppNotificationItem? notification)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (notification == null)
            {
                _activeNotificationFlyout?.Hide();
            }
            else
            {
                string text = notification.Title;
                if (!string.IsNullOrWhiteSpace(notification.Message))
                {
                    text = $"{notification.Title}: {notification.Message}";
                }
                NotificationCapsuleText.Text = text;
            }
            UpdateLiveActivityState();
        });
    }

    private void OnNotificationCapsulePillPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        var current = _notificationService.CurrentActiveNotification;
        if (current == null) return;

        var flyout = new Flyout();
        var card = new Border
        {
            Width = 260,
            Padding = new Thickness(12),
            Background = (Brush)Application.Current.Resources["OverlaySurfaceBrush"],
            BorderBrush = (Brush)Application.Current.Resources["OverlayBorderBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8)
        };

        var stack = new StackPanel { Spacing = 8 };

        var headerGrid = new Grid();
        var sourceStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
        sourceStack.Children.Add(new FontIcon { Glyph = "\uEA8F", FontSize = 12, Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 234, 179, 8)) });
        sourceStack.Children.Add(new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(current.SourceAppName) ? "System" : current.SourceAppName,
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"]
        });

        var timeText = new TextBlock
        {
            Text = current.Timestamp.ToLocalTime().ToString("t"),
            FontSize = 10,
            Foreground = (Brush)Application.Current.Resources["OverlayTextTertiaryBrush"],
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };

        headerGrid.Children.Add(sourceStack);
        headerGrid.Children.Add(timeText);
        stack.Children.Add(headerGrid);

        var titleText = new TextBlock
        {
            Text = current.Title,
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
            Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"],
            TextWrapping = TextWrapping.Wrap
        };
        stack.Children.Add(titleText);

        var bodyText = new TextBlock
        {
            Text = string.IsNullOrWhiteSpace(current.Message) ? "(No details)" : current.Message,
            FontSize = 12,
            Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"],
            TextWrapping = TextWrapping.Wrap,
            MaxHeight = 120
        };
        stack.Children.Add(bodyText);

        var dismissBtn = new Button
        {
            Content = "Dismiss",
            HorizontalAlignment = HorizontalAlignment.Right,
            Background = (Brush)Application.Current.Resources["OverlayControlHoverBrush"],
            Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"],
            FontSize = 11,
            Height = 26,
            Padding = new Thickness(12, 2, 12, 2),
            CornerRadius = new CornerRadius(6)
        };
        dismissBtn.Click += (s, args) =>
        {
            flyout.Hide();
            _notificationService.DismissActive();
        };
        stack.Children.Add(dismissBtn);

        card.Child = stack;
        flyout.Content = card;
        _activeNotificationFlyout = flyout;
        flyout.ShowAt(NotificationCapsulePill);
        e.Handled = true;
    }

    private void OnNotificationDismissButtonPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        e.Handled = true; // Prevent opening the notification flyout card
    }

    private void OnNotificationDismissButtonClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _activeNotificationFlyout?.Hide();
        _notificationService.DismissActive();
        UpdateLiveActivityState();
    }

    private void OnCommandFailed(object? sender, PaletteCommand command)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _notificationService.Post(new AppNotificationItem
            {
                SourceAppName = "Command Palette",
                Title = "Command Failed",
                Message = $"Could not execute '{command.Title}'",
                Priority = NotificationPriority.High,
                ExpiresAfter = TimeSpan.FromSeconds(5)
            });
        });
    }

    private void OnProductivityGlanceChanged(object? sender, ProductivityGlance glance)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            RenderProductivityGlance(glance);
        });
    }

    private void RenderProductivityGlance(ProductivityGlance glance)
    {
        if (glance.GamificationProgression != null)
        {
            var prog = glance.GamificationProgression;
            ProductivitySummaryText.Text = $"{glance.SummaryText}  •  ⭐ Lv.{prog.CurrentLevel} ({prog.CurrentLevelXp}/{prog.NextLevelRequiredXp} XP)  🔥 {prog.CurrentStreakDays}d (🛡️{prog.StreakShieldCount})";
        }
        else
        {
            ProductivitySummaryText.Text = glance.SummaryText;
        }

        if (glance.ActiveTodos.Count > 0)
        {
            var taskLines = System.Linq.Enumerable.Select(System.Linq.Enumerable.Take(glance.ActiveTodos, 3), t => $"• {t.Title}");
            ProductivityTasksPreviewText.Text = string.Join("\n", taskLines);
        }
        else
        {
            ProductivityTasksPreviewText.Text = "All tasks completed ✓";
        }

        if (glance.UpcomingEvents.Count > 0)
        {
            var eventLines = System.Linq.Enumerable.Select(System.Linq.Enumerable.Take(glance.UpcomingEvents, 2), e => $"• {e.Title} ({e.StartTime:t})");
            ProductivitySchedulePreviewText.Text = string.Join("\n", eventLines);
        }
        else if (glance.ActiveNote != null)
        {
            ProductivitySchedulePreviewText.Text = $"Note: {glance.ActiveNote.Title}";
        }
        else
        {
            ProductivitySchedulePreviewText.Text = "No upcoming events today";
        }
    }

    private void OnLevelUp(object? sender, int level)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _notificationService.Post(
                $"🎉 Level {level} Reached!",
                $"Congratulations! You unlocked new capsule cosmetics at Level {level}.",
                "Gamification",
                NotificationPriority.Normal);
        });
    }

    private void OnBadgeUnlocked(object? sender, BadgeDescriptor badge)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            _notificationService.Post(
                $"🏆 Badge Unlocked: {badge.Title}",
                badge.Description,
                "Gamification",
                NotificationPriority.Normal);
        });
    }

    // =========================================================================
    // Universal Tools Engine UI Handlers
    // =========================================================================

    private void RenderToolsHub()
    {
        UpdateToolCategoryChips();
        RenderToolSelectorChips();
    }

    private void OnToolCategoryDevClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _selectedToolCategory = "Developer";
        UpdateToolCategoryChips();
        RenderToolSelectorChips();
    }

    private void OnToolCategoryFileClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _selectedToolCategory = "File Tools";
        UpdateToolCategoryChips();
        RenderToolSelectorChips();
    }

    private void OnToolCategoryImageClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        _selectedToolCategory = "Image Tools";
        UpdateToolCategoryChips();
        RenderToolSelectorChips();
    }

    private void UpdateToolCategoryChips()
    {
        var accentBrush = (Brush)Application.Current.Resources["OverlayAccentBrush"];
        var surfaceBrush = (Brush)Application.Current.Resources["OverlaySurfaceBrush"];
        var textSecBrush = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"];
        var darkBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 9, 9, 9));

        ToolCategoryDevBtn.Background = _selectedToolCategory == "Developer" ? accentBrush : surfaceBrush;
        ToolCategoryDevBtn.Foreground = _selectedToolCategory == "Developer" ? darkBrush : textSecBrush;

        ToolCategoryFileBtn.Background = _selectedToolCategory == "File Tools" ? accentBrush : surfaceBrush;
        ToolCategoryFileBtn.Foreground = _selectedToolCategory == "File Tools" ? darkBrush : textSecBrush;

        ToolCategoryImageBtn.Background = _selectedToolCategory == "Image Tools" ? accentBrush : surfaceBrush;
        ToolCategoryImageBtn.Foreground = _selectedToolCategory == "Image Tools" ? darkBrush : textSecBrush;
    }

    private void RenderToolSelectorChips()
    {
        ToolSelectorChipsPanel.Children.Clear();
        var categoryTools = _toolRegistry.GetToolsByCategory(_selectedToolCategory);

        if (_selectedTool == null || !categoryTools.Any(t => t.Id == _selectedTool.Id))
        {
            _selectedTool = categoryTools.FirstOrDefault();
        }

        foreach (var tool in categoryTools)
        {
            bool isSelected = _selectedTool != null && _selectedTool.Id == tool.Id;
            var btn = new Button
            {
                Content = tool.DisplayName,
                FontSize = 9,
                FontWeight = isSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
                Height = 22,
                Padding = new Thickness(6, 1, 6, 1),
                CornerRadius = new CornerRadius(4),
                Background = isSelected ? (Brush)Application.Current.Resources["OverlayControlHoverBrush"] : (Brush)Application.Current.Resources["OverlaySurfaceBrush"],
                Foreground = isSelected ? (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"] : (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"],
                Tag = tool
            };

            btn.Click += (s, args) =>
            {
                _stateMachine.UserInteracted();
                _selectedTool = (IDesktopTool)((Button)s).Tag;
                RenderToolSelectorChips();
                UpdateToolUiForSelection();
            };

            ToolSelectorChipsPanel.Children.Add(btn);
        }

        UpdateToolUiForSelection();
    }

    private void UpdateToolUiForSelection()
    {
        if (_selectedTool == null)
        {
            ToolStatusText.Text = "No tool selected in this category.";
            return;
        }

        ToolStatusText.Text = $"{_selectedTool.DisplayName} — {_selectedTool.Description}";
        if (_selectedTool.Id == "tool-json-format")
        {
            ToolInputBox.PlaceholderText = "Paste JSON to format or minify...";
        }
        else if (_selectedTool.Id == "tool-base64")
        {
            ToolInputBox.PlaceholderText = "Enter text to encode/decode in Base64...";
        }
        else if (_selectedTool.Id == "tool-uuid")
        {
            ToolInputBox.PlaceholderText = "Press Execute to generate UUID(s)...";
        }
        else if (_selectedTool.Id == "tool-regex-tester")
        {
            ToolInputBox.PlaceholderText = "Input text to evaluate against regex...";
        }
        else if (_selectedTool.Id == "tool-file-folder-size")
        {
            ToolInputBox.PlaceholderText = "Enter folder path (e.g. C:\\Projects)...";
        }
        else if (_selectedTool.Id.StartsWith("tool-image"))
        {
            ToolInputBox.PlaceholderText = "Enter image file path (e.g. C:\\Images\\photo.jpg)...";
        }
        else
        {
            ToolInputBox.PlaceholderText = "Enter input text or file path...";
        }
    }

    private async void OnToolExecuteClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (_selectedTool == null) return;

        ToolStatusText.Text = $"Executing {_selectedTool.DisplayName}...";
        var context = new ToolContext
        {
            InputText = ToolInputBox.Text
        };

        if (_selectedTool.Id.StartsWith("tool-file") || _selectedTool.Id.StartsWith("tool-image"))
        {
            if (!string.IsNullOrWhiteSpace(ToolInputBox.Text))
            {
                var files = ToolInputBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                context = context with { InputFiles = files };
            }
        }

        var result = await _toolRegistry.ExecuteToolAsync(_selectedTool.Id, context, _windowCts.Token);
        if (result.Success)
        {
            _lastToolResultText = result.OutputText;
            ToolStatusText.Text = "Success.";
            _ = _gamificationService.RecordToolUsedAsync(_selectedTool.DisplayName);
            ToolOutputSnippetText.Text = result.OutputText ?? "(Completed with no text output)";
            if (_selectedTool.Id is "tool-json-format" or "tool-base64" or "tool-text-format" or "tool-hash")
            {
                ToolInputBox.Text = result.OutputText ?? string.Empty;
            }
        }
        else
        {
            ToolStatusText.Text = $"Error: {result.ErrorMessage}";
            ToolOutputSnippetText.Text = result.ErrorMessage ?? "Operation failed.";
        }
    }

    private ToolsWindow? _toolsWindow;

    public void OpenToolsWorkspace(string? category = null)
    {
        _stateMachine.UserInteracted();
        if (_toolsWindow == null)
        {
            _toolsWindow = new ToolsWindow(
                _toolRegistry,
                _devUtilsService,
                _fileToolsService,
                _imageToolsService,
                _clipboardService,
                _pluginManager,
                _workflowEngine,
                _aiAssistantService,
                _personalizationService,
                _diagnosticsService,
                requestShowTopBar: () => ShowOverlay());

            _toolsWindow.Closed += (s, e) => _toolsWindow = null;
        }

        _toolsWindow.Activate();
        if (!string.IsNullOrEmpty(category))
        {
            _toolsWindow.SelectCategory(category);
        }
    }

    private void OnToolOpenWorkspaceClick(object sender, RoutedEventArgs e)
    {
        OpenToolsWorkspace(_selectedToolCategory);
    }

    private void OnToolCopyResultClick(object sender, RoutedEventArgs e)
    {
        _stateMachine.UserInteracted();
        if (!string.IsNullOrEmpty(_lastToolResultText))
        {
            _clipboardService.AddSnippet(_lastToolResultText);
            ToolStatusText.Text = "Result copied to clipboard.";
        }
    }

    private void RegisterBuiltInCommands()
    {
        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-open-tools-workspace",
            Title = "Open Universal Tools Workspace",
            Description = "Open expansive desktop window for image, file, and developer tools",
            Category = "Tools",
            Keywords = new[] { "tools", "workspace", "window", "image", "file", "developer", "utility", "crop", "resize", "compress", "rename" },
            IconGlyph = "\uE74C",
            Action = () => DispatcherQueue.TryEnqueue(() => OpenToolsWorkspace())
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-open-image-tools-workspace",
            Title = "Image Tools: Open Workspace",
            Description = "Open large workspace for image compression, conversion, resize, and crop",
            Category = "Image Tools",
            Keywords = new[] { "image", "photo", "compress", "resize", "crop", "rotate", "flip", "webp", "png", "jpeg" },
            IconGlyph = "\uEB9F",
            Action = () => DispatcherQueue.TryEnqueue(() => OpenToolsWorkspace("image"))
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-open-file-tools-workspace",
            Title = "File Tools: Open Workspace",
            Description = "Open workspace for batch rename, ZIP, folder analyzer, search, and converter",
            Category = "File Tools",
            Keywords = new[] { "file", "rename", "batch", "zip", "extract", "folder", "size", "search", "hash" },
            IconGlyph = "\uE8B7",
            Action = () => DispatcherQueue.TryEnqueue(() => OpenToolsWorkspace("file"))
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-open-dev-tools-workspace",
            Title = "Developer Utilities: Open Workspace",
            Description = "Open workspace for JSON, Regex, Base64, UUID, Color Picker, and QR Code",
            Category = "Developer",
            Keywords = new[] { "developer", "dev", "json", "regex", "base64", "uuid", "color", "qr", "unit" },
            IconGlyph = "\uE943",
            Action = () => DispatcherQueue.TryEnqueue(() => OpenToolsWorkspace("developer"))
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-open-clipboard-workspace",
            Title = "Clipboard History: Open Workspace",
            Description = "Open expanded clipboard manager with search, filters, and export",
            Category = "Clipboard",
            Keywords = new[] { "clipboard", "history", "snippets", "copy", "paste", "backup", "export" },
            IconGlyph = "\uE77F",
            Action = () => DispatcherQueue.TryEnqueue(() => OpenToolsWorkspace("clipboard"))
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-open-workflows-workspace",
            Title = "Workflows: Open Automations Workspace",
            Description = "Manage and execute automated multi-step desktop workflows",
            Category = "Workflows",
            Keywords = new[] { "workflow", "automation", "batch", "macro", "chain", "step" },
            IconGlyph = "\uE8B7",
            Action = () => DispatcherQueue.TryEnqueue(() => OpenToolsWorkspace("workflows"))
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-open-plugins-workspace",
            Title = "Plugins: Manage Extensions",
            Description = "View, activate, and configure modular DynamicTopBar plugins",
            Category = "Plugins",
            Keywords = new[] { "plugin", "extension", "addon", "manager", "modular", "install" },
            IconGlyph = "\uEA86",
            Action = () => DispatcherQueue.TryEnqueue(() => OpenToolsWorkspace("plugins"))
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-open-diagnostics-workspace",
            Title = "Diagnostics: Health & Subsystems Glance",
            Description = "Inspect process memory, thread usage, subsystem status, and export sanitized reports",
            Category = "Diagnostics",
            Keywords = new[] { "health", "diagnostics", "memory", "threads", "logs", "sanitize", "report" },
            IconGlyph = "\uE958",
            Action = () => DispatcherQueue.TryEnqueue(() => OpenToolsWorkspace("diagnostics"))
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-open-ai-workspace",
            Title = "AI Assistant: Natural Language Command",
            Description = "Query local intent planner to execute desktop tools via natural language",
            Category = "AI Assistant",
            Keywords = new[] { "ai", "assistant", "intent", "query", "nlp", "prompt", "smart" },
            IconGlyph = "\uE946",
            Action = () => DispatcherQueue.TryEnqueue(() => OpenToolsWorkspace("ai"))
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-toggle-overlay",
            Title = "Toggle Capsule Visibility",
            Description = "Show or hide the DynamicTopBar capsule",
            Category = "Overlay",
            Keywords = new[] { "toggle", "hide", "show", "capsule", "topbar" },
            IconGlyph = "\uE8D6",
            ShortcutHint = "Ctrl+Shift+Space",
            Action = () => ExecuteAction("toggle-overlay-visibility")
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-expand-collapse",
            Title = "Expand / Collapse TopBar",
            Description = "Expand down to modular panel or collapse to capsule",
            Category = "Overlay",
            Keywords = new[] { "expand", "collapse", "panel", "toggle" },
            IconGlyph = "\uE70E",
            ShortcutHint = "Ctrl+Shift+Down",
            Action = () => ExecuteAction("toggle-panel-expansion")
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-next-feature",
            Title = "Next Feature Module",
            Description = "Navigate forward in active module carousel",
            Category = "Navigation",
            Keywords = new[] { "next", "module", "tab", "cycle" },
            IconGlyph = "\uE72A",
            ShortcutHint = "Ctrl+Shift+Right",
            Action = () => ExecuteAction("navigate-next-feature")
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-prev-feature",
            Title = "Previous Feature Module",
            Description = "Navigate backward in active module carousel",
            Category = "Navigation",
            Keywords = new[] { "previous", "prev", "back", "module" },
            IconGlyph = "\uE72B",
            ShortcutHint = "Ctrl+Shift+Left",
            Action = () => ExecuteAction("navigate-prev-feature")
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-media-play-pause",
            Title = "Media: Play / Pause",
            Description = "Toggle playback for active Windows media session",
            Category = "Media",
            Keywords = new[] { "media", "play", "pause", "music", "audio" },
            IconGlyph = "\uE768",
            ShortcutHint = "Ctrl+Shift+F8",
            ModuleContextId = "media",
            AsyncAction = async () => await _mediaService.TogglePlayPauseAsync()
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-media-next",
            Title = "Media: Next Track",
            Description = "Skip to next audio track",
            Category = "Media",
            Keywords = new[] { "media", "next", "track", "song" },
            IconGlyph = "\uE893",
            ModuleContextId = "media",
            AsyncAction = async () => await _mediaService.SkipNextAsync()
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-media-prev",
            Title = "Media: Previous Track",
            Description = "Skip to previous audio track",
            Category = "Media",
            Keywords = new[] { "media", "prev", "previous", "track" },
            IconGlyph = "\uE892",
            ModuleContextId = "media",
            AsyncAction = async () => await _mediaService.SkipPreviousAsync()
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-spotify-open",
            Title = "Spotify: Open Module",
            Description = "Switch to Spotify Web API Connect controller",
            Category = "Spotify",
            Keywords = new[] { "spotify", "music", "connect" },
            IconGlyph = "\uE8D6",
            Action = () => ExecuteAction("open-spotify")
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-spotify-play-pause",
            Title = "Spotify: Play / Pause",
            Description = "Toggle Spotify Web API playback",
            Category = "Spotify",
            Keywords = new[] { "spotify", "play", "pause" },
            IconGlyph = "\uE768",
            ModuleContextId = "spotify",
            AsyncAction = async () => await _spotifyService.TogglePlayPauseAsync()
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-spotify-next",
            Title = "Spotify: Next Track",
            Description = "Skip forward on Spotify",
            Category = "Spotify",
            Keywords = new[] { "spotify", "next" },
            IconGlyph = "\uE893",
            ModuleContextId = "spotify",
            AsyncAction = async () => await _spotifyService.SkipNextAsync()
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-focus-start-pause",
            Title = "Focus Timer: Start / Pause",
            Description = "Toggle countdown for Pomodoro focus session",
            Category = "Productivity",
            Keywords = new[] { "focus", "timer", "pomodoro", "start", "pause" },
            IconGlyph = "\uE916",
            ShortcutHint = "Ctrl+Shift+F9",
            ModuleContextId = "focus",
            Action = () => ExecuteAction("focus-start-pause")
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-focus-reset",
            Title = "Focus Timer: Reset",
            Description = "Reset timer to preset duration",
            Category = "Productivity",
            Keywords = new[] { "focus", "reset", "timer" },
            IconGlyph = "\uE777",
            ModuleContextId = "focus",
            Action = () => _focusTimerService.Reset()
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-notes-open",
            Title = "Quick Notes: Open Scratchpad",
            Description = "View and edit local scratchpad notes",
            Category = "Productivity",
            Keywords = new[] { "notes", "memo", "scratchpad", "quick" },
            IconGlyph = "\uE70F",
            ShortcutHint = "Ctrl+Shift+N",
            ModuleContextId = "notes",
            Action = () => ExecuteAction("open-quick-notes")
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-todo-open",
            Title = "To-do List: Open Tasks",
            Description = "View checklist and manage pending items",
            Category = "Productivity",
            Keywords = new[] { "todo", "tasks", "checklist" },
            IconGlyph = "\uE73A",
            ModuleContextId = "todo",
            Action = () => ExecuteAction("open-todo-list")
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-calendar-open",
            Title = "Calendar: Open Schedule",
            Description = "View daily events and upcoming agenda",
            Category = "Productivity",
            Keywords = new[] { "calendar", "events", "agenda", "schedule" },
            IconGlyph = "\uE787",
            ModuleContextId = "calendar",
            Action = () => ExecuteAction("open-calendar")
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-clipboard-open",
            Title = "Clipboard: Open History",
            Description = "Browse and paste recent clipboard snippets",
            Category = "Utilities",
            Keywords = new[] { "clipboard", "history", "snippets", "copy" },
            IconGlyph = "\uE77F",
            ModuleContextId = "clipboard",
            Action = () => ExecuteAction("open-clipboard")
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-glance-open",
            Title = "System Glance: Open Stats",
            Description = "View CPU, RAM, and battery telemetry",
            Category = "System",
            Keywords = new[] { "system", "glance", "cpu", "ram", "battery", "stats" },
            IconGlyph = "\uE770",
            ModuleContextId = "system-glance",
            Action = () => ExecuteAction("open-system-glance")
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-weather-open",
            Title = "Weather: Open Forecast",
            Description = "Check local temperature and meteorological forecast",
            Category = "Information",
            Keywords = new[] { "weather", "temperature", "forecast", "rain", "sun" },
            IconGlyph = "\uE9BE",
            ModuleContextId = "weather",
            Action = () => ExecuteAction("open-weather")
        });

        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-productivity-hub",
            Title = "Productivity: Overview Hub",
            Description = "Unified glance across Calendar, Todo, Notes, and Focus",
            Category = "Productivity",
            Keywords = new[] { "productivity", "hub", "summary", "overview" },
            IconGlyph = "\uE9D5",
            Action = () => ExecuteAction("open-productivity-hub")
        });

        // Universal Tools Engine commands
        _commandPaletteService.RegisterCommand(new PaletteCommand
        {
            Id = "cmd-tools-open",
            Title = "Tools Engine: Open Hub",
            Description = "Open Universal Tools Engine utility panel",
            Category = "Utilities",
            Keywords = new[] { "tools", "utility", "image", "file", "developer", "json", "hash" },
            IconGlyph = "\uE74C",
            ModuleContextId = "tools",
            Action = () => ExecuteAction("open-tools")
        });

        foreach (var tool in _toolRegistry.Tools)
        {
            var t = tool;
            _commandPaletteService.RegisterCommand(new PaletteCommand
            {
                Id = $"cmd-{t.Id}",
                Title = $"{t.Category}: {t.DisplayName}",
                Description = t.Description,
                Category = t.Category,
                Keywords = new[] { t.DisplayName.ToLowerInvariant(), t.Category.ToLowerInvariant(), "tool" },
                IconGlyph = t.IconGlyph,
                ModuleContextId = "tools",
                Action = () =>
                {
                    _selectedToolCategory = t.Category;
                    _selectedTool = t;
                    ExecuteAction("open-tools");
                }
            });
        }
    }

    public bool IsOverlayVisible => _appWindow.IsVisible;

    public IntPtr Hwnd => _hwnd;

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        // 1. Flush any pending note edits FIRST while services are alive
        FlushPendingNoteSave();
        if (_notesAutosaveTimer != null)
        {
            _notesAutosaveTimer.Stop();
            _notesAutosaveTimer.Tick -= OnNotesAutosaveTick;
            _notesAutosaveTimer = null;
        }

        if (_toolsWindow != null)
        {
            try { _toolsWindow.Close(); } catch { }
            _toolsWindow = null;
        }

        if (_settingsWindow != null)
        {
            try { _settingsWindow.Close(); } catch { }
            _settingsWindow = null;
        }

        if (_commandPaletteWindow != null)
        {
            try { _commandPaletteWindow.Close(); } catch { }
            _commandPaletteWindow = null;
        }

        _windowCts.Cancel();
        _windowCts.Dispose();

        _toolRegistry.Dispose();
        _fileToolsService.Dispose();

        _shortcutService.ShortcutTriggered -= OnShortcutTriggered;
        _shortcutService.Dispose();

        _notificationService.ActiveNotificationChanged -= OnActiveNotificationChanged;
        _notificationService.Dispose();

        _productivityHubService.GlanceChanged -= OnProductivityGlanceChanged;
        _productivityHubService.Dispose();

        _commandPaletteService.CommandFailed -= OnCommandFailed;
        _commandPaletteService.Dispose();

        _clockTimer.Stop();
        _clockTimer.Tick -= OnClockTimerTick;

        _appWindow.Changed -= OnAppWindowChanged;

        if (CollapseStoryboard != null)
        {
            CollapseStoryboard.Completed -= OnCollapseStoryboardCompleted;
        }
        if (SlideUpStoryboard != null)
        {
            SlideUpStoryboard.Completed -= OnSlideUpStoryboardCompleted;
        }

        _autoHideTimer?.Stop();
        if (_autoHideTimer != null)
        {
            _autoHideTimer.Tick -= OnAutoHideTimerTick;
        }

        _stateMachine.StateChanged -= OnStateMachineStateChanged;
        _stateMachine.Dispose();

        _mediaService.SnapshotChanged -= OnMediaSnapshotChanged;
        _mediaService.Dispose();

        _featureNavigator.CurrentFeatureChanged -= OnCurrentFeatureChanged;

        _focusTimerService.SnapshotChanged -= OnFocusTimerSnapshotChanged;
        _focusTimerService.Dispose();

        _systemGlanceService.SnapshotChanged -= OnSystemGlanceSnapshotChanged;
        _systemGlanceService.Dispose();

        _stopwatchService.SnapshotChanged -= OnStopwatchSnapshotChanged;
        _stopwatchService.Dispose();

        _spotifyService.AuthStateChanged -= OnSpotifyAuthStateChanged;
        _spotifyService.PlaybackChanged -= OnSpotifyPlaybackChanged;
        _spotifyService.Dispose();

        _weatherService.SnapshotChanged -= OnWeatherSnapshotChanged;
        _weatherService.Dispose();

        _todoService.ItemsChanged -= OnTodoItemsChanged;
        _todoService.Dispose();

        _calendarService.GlanceChanged -= OnCalendarGlanceChanged;
        _calendarService.Dispose();

        _notesService.NotesChanged -= OnNotesChanged;
        _notesService.SelectedNoteChanged -= OnSelectedNoteChanged;
        _notesService.Dispose();

        _clipboardService.StateChanged -= OnClipboardStateChanged;
        _clipboardService.SnippetsChanged -= OnClipboardSnippetsChanged;
        _clipboardService.Dispose();

        _reminderService.ReminderFired -= OnReminderFired;
        _reminderService.Dispose();

        _pluginManager.Dispose();
        _workflowEngine.Dispose();
        _aiAssistantService.Dispose();
        _personalizationService.Dispose();
        _gamificationService.LevelUp -= OnLevelUp;
        _gamificationService.BadgeUnlocked -= OnBadgeUnlocked;
        _gamificationService.Dispose();
        RestoreFromFullscreenSuspension();
        _diagnosticsService.Dispose();
    }

    private void FlushPendingNoteSave()
    {
        if (_notesAutosaveTimer != null && _notesAutosaveTimer.IsEnabled)
        {
            _notesAutosaveTimer.Stop();
            var selected = _notesService.SelectedNote;
            string title = NoteTitleBox.Text.Trim();
            string content = NoteContentBox.Text;

            if (selected != null)
            {
                _ = _notesService.UpdateNoteAsync(selected.Id, title, content);
            }
            else if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(content))
            {
                _ = _notesService.CreateNoteAsync(string.IsNullOrWhiteSpace(title) ? "New Note" : title, content);
            }
        }
    }
}
