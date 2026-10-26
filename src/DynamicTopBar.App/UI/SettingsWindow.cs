using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using DynamicTopBar.App.Platform;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using WinRT.Interop;
using Win32 = DynamicTopBar.App.Platform.Win32Interop;

namespace DynamicTopBar.App.UI;

/// <summary>
/// Dedicated multi-tab Settings and Personalization dialog window for DynamicTopBar.
/// Categories: General, Modules, Shortcuts, Privacy, Diagnostics.
/// </summary>
public sealed class SettingsWindow : Window, IDisposable
{
    private readonly ISettingsService _settingsService;
    private readonly IFeatureNavigator _featureNavigator;
    private readonly IFeatureRegistry _featureRegistry;
    private readonly IShortcutService _shortcutService;
    private readonly IClipboardService _clipboardService;
    private readonly IDiagnosticsService _diagnosticsService;
    private readonly INotificationService? _notificationService;
    private readonly Action? _onSettingsChanged;

    private readonly IntPtr _hwnd;
    private readonly AppWindow _appWindow;

    private readonly Grid _contentContainer;
    private readonly Button[] _categoryNavButtons = new Button[5];
    private int _selectedTabIndex = 0;
    private bool _isDisposed;

    public bool IsDisposed => _isDisposed;

    public SettingsWindow(
        ISettingsService settingsService,
        IFeatureNavigator featureNavigator,
        IFeatureRegistry featureRegistry,
        IShortcutService shortcutService,
        IClipboardService clipboardService,
        IDiagnosticsService diagnosticsService,
        INotificationService? notificationService = null,
        Action? onSettingsChanged = null)
    {
        _settingsService = settingsService;
        _featureNavigator = featureNavigator;
        _featureRegistry = featureRegistry;
        _shortcutService = shortcutService;
        _clipboardService = clipboardService;
        _diagnosticsService = diagnosticsService;
        _notificationService = notificationService;
        _onSettingsChanged = onSettingsChanged;

        this.Title = "DynamicTopBar — Settings & Preferences";
        _hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = this.AppWindow;

        // Position window: 680x520 centered on primary screen
        int widthDip = 680;
        int heightDip = 520;
        uint dpi = Win32.GetDpiForWindow(_hwnd);
        double scaling = dpi > 0 ? dpi / 96.0 : 1.0;
        int pxWidth = (int)(widthDip * scaling);
        int pxHeight = (int)(heightDip * scaling);

        var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
        int posX = displayArea.WorkArea.X + (displayArea.WorkArea.Width - pxWidth) / 2;
        int posY = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - pxHeight) / 2;
        _appWindow.MoveAndResize(new RectInt32(posX, posY, pxWidth, pxHeight));

        // Enable Windows 11 dark mode for window frame
        uint darkMode = 1;
        Win32.DwmSetWindowAttribute(_hwnd, 20 /* DWMWA_USE_IMMERSIVE_DARK_MODE */, ref darkMode, sizeof(uint));

        // Main layout root
        var rootGrid = new Grid
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 18, 18, 22))
        };
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left Navigation Bar
        var sidebar = CreateSidebar();
        Grid.SetColumn(sidebar, 0);
        rootGrid.Children.Add(sidebar);

        // Right Content Area
        _contentContainer = new Grid
        {
            Padding = new Thickness(24, 20, 24, 20),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 24, 24, 28))
        };
        Grid.SetColumn(_contentContainer, 1);
        rootGrid.Children.Add(_contentContainer);

        this.Content = rootGrid;
        this.Closed += OnWindowClosed;

        SelectTab(0);
    }

    private FrameworkElement CreateSidebar()
    {
        var panel = new Grid
        {
            Padding = new Thickness(14, 18, 14, 18),
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 16, 16, 20))
        };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header Branding
        var header = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 16) };
        var icon = new FontIcon
        {
            Glyph = "\uE74C",
            FontSize = 18,
            Foreground = (Brush)Application.Current.Resources["OverlayAccentBrush"]
        };
        var titleStack = new StackPanel();
        var appTitle = new TextBlock
        {
            Text = "DynamicTopBar",
            FontSize = 13,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"]
        };
        var subTitle = new TextBlock
        {
            Text = "Preferences",
            FontSize = 10,
            Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"]
        };
        titleStack.Children.Add(appTitle);
        titleStack.Children.Add(subTitle);
        header.Children.Add(icon);
        header.Children.Add(titleStack);
        Grid.SetRow(header, 0);
        panel.Children.Add(header);

        // Navigation Menu
        var navList = new StackPanel { Spacing = 4 };
        string[] categories = { "General", "Modules", "Shortcuts", "Privacy", "Diagnostics" };
        string[] icons = { "\uE713", "\uE74C", "\uE765", "\uE72E", "\uE9F9" };

        for (int i = 0; i < categories.Length; i++)
        {
            int tabIndex = i;
            var btn = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(10, 8, 10, 8),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(0)
            };

            var btnContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            btnContent.Children.Add(new FontIcon { Glyph = icons[i], FontSize = 14, Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"] });
            btnContent.Children.Add(new TextBlock { Text = categories[i], FontSize = 12, Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"] });
            btn.Content = btnContent;

            btn.Click += (s, e) => SelectTab(tabIndex);
            _categoryNavButtons[i] = btn;
            navList.Children.Add(btn);
        }

        Grid.SetRow(navList, 1);
        panel.Children.Add(navList);

        // Footer version info
        var footer = new TextBlock
        {
            Text = "v1.0.0 • Tahap 1 Production",
            FontSize = 9,
            Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"],
            Opacity = 0.7,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        Grid.SetRow(footer, 2);
        panel.Children.Add(footer);

        return panel;
    }

    private void SelectTab(int tabIndex)
    {
        _selectedTabIndex = tabIndex;

        // Highlight active sidebar item
        for (int i = 0; i < _categoryNavButtons.Length; i++)
        {
            var btn = _categoryNavButtons[i];
            if (i == tabIndex)
            {
                btn.Background = (Brush)Application.Current.Resources["OverlaySurfaceBrush"];
                btn.BorderThickness = new Thickness(1);
                btn.BorderBrush = (Brush)Application.Current.Resources["OverlayBorderBrush"];
            }
            else
            {
                btn.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                btn.BorderThickness = new Thickness(0);
            }
        }

        _contentContainer.Children.Clear();
        var tabContent = tabIndex switch
        {
            0 => CreateGeneralTab(),
            1 => CreateModulesTab(),
            2 => CreateShortcutsTab(),
            3 => CreatePrivacyTab(),
            4 => CreateDiagnosticsTab(),
            _ => CreateGeneralTab()
        };

        _contentContainer.Children.Add(tabContent);
    }

    private FrameworkElement CreateGeneralTab()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel { Spacing = 14 };

        stack.Children.Add(CreateSectionHeader("General Settings", "Basic top bar behaviors, timers, and display preferences."));

        var prefs = _settingsService.LoadPreferences();

        // Auto-Hide Switch
        var autoHideSwitch = new ToggleSwitch
        {
            Header = "Auto-hide capsule to notch when inactive",
            IsOn = prefs.AutoHideEnabled,
            OffContent = "Disabled (Capsule stays always visible)",
            OnContent = "Enabled (Hides to top notch handle after 2.5s)"
        };
        autoHideSwitch.Toggled += (s, e) =>
        {
            var current = _settingsService.LoadPreferences();
            _settingsService.SavePreferences(current with { AutoHideEnabled = autoHideSwitch.IsOn });
            _onSettingsChanged?.Invoke();
        };
        stack.Children.Add(autoHideSwitch);

        // Clock Format
        var clockSwitch = new ToggleSwitch
        {
            Header = "Time Display Format",
            IsOn = prefs.ClockUse24HourFormat,
            OffContent = "12-Hour (AM / PM)",
            OnContent = "24-Hour (Military time)"
        };
        clockSwitch.Toggled += (s, e) =>
        {
            var current = _settingsService.LoadPreferences();
            _settingsService.SavePreferences(current with { ClockUse24HourFormat = clockSwitch.IsOn });
            _onSettingsChanged?.Invoke();
        };
        stack.Children.Add(clockSwitch);

        // Temperature Unit
        var tempSwitch = new ToggleSwitch
        {
            Header = "Weather Temperature Unit",
            IsOn = prefs.WeatherUnit == WeatherUnit.Fahrenheit,
            OffContent = "Celsius (°C)",
            OnContent = "Fahrenheit (°F)"
        };
        tempSwitch.Toggled += (s, e) =>
        {
            var current = _settingsService.LoadPreferences();
            _settingsService.SavePreferences(current with { WeatherUnit = tempSwitch.IsOn ? WeatherUnit.Fahrenheit : WeatherUnit.Celsius });
            _onSettingsChanged?.Invoke();
        };
        stack.Children.Add(tempSwitch);

        // Reminders
        var reminderSwitch = new ToggleSwitch
        {
            Header = "Task & Calendar Event Reminders",
            IsOn = prefs.RemindersEnabled,
            OffContent = "Disabled",
            OnContent = "Enabled (Notify when tasks/events are due)"
        };
        reminderSwitch.Toggled += (s, e) =>
        {
            var current = _settingsService.LoadPreferences();
            _settingsService.SavePreferences(current with { RemindersEnabled = reminderSwitch.IsOn });
            _onSettingsChanged?.Invoke();
        };
        stack.Children.Add(reminderSwitch);

        scroll.Content = stack;
        return scroll;
    }

    private FrameworkElement CreateModulesTab()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel { Spacing = 12 };

        stack.Children.Add(CreateSectionHeader("Active Modules", "Enable or disable modules visible in DynamicTopBar. Changes apply immediately."));

        var prefs = _settingsService.LoadPreferences();
        var allFeatures = _featureRegistry.GetAllDescriptors().OrderBy(f => f.DisplayOrder);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        int row = 0;
        int col = 0;

        foreach (var feat in allFeatures)
        {
            if (col == 0)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            bool isEnabled = _featureNavigator.EnabledFeatures.Any(f => f.Id == feat.Id);

            var cb = new CheckBox
            {
                Content = $"{feat.DisplayName}",
                IsChecked = isEnabled,
                Margin = new Thickness(0, 4, 12, 4),
                Tag = feat.Id,
                Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"],
                FontSize = 12
            };

            cb.Click += (s, e) =>
            {
                if (s is CheckBox box && box.Tag is string featId)
                {
                    bool check = box.IsChecked == true;
                    var current = _settingsService.LoadPreferences();
                    var list = new List<string>(current.EnabledFeatureIds);
                    if (check && !list.Contains(featId))
                    {
                        list.Add(featId);
                    }
                    else if (!check && list.Contains(featId))
                    {
                        list.Remove(featId);
                    }
                    var updated = current with { EnabledFeatureIds = list };
                    _settingsService.SavePreferences(updated);
                    _featureNavigator.ApplyPreferences(updated);
                    _onSettingsChanged?.Invoke();
                }
            };

            Grid.SetRow(cb, row);
            Grid.SetColumn(cb, col);
            grid.Children.Add(cb);

            col++;
            if (col >= 2)
            {
                col = 0;
                row++;
            }
        }

        stack.Children.Add(grid);
        scroll.Content = stack;
        return scroll;
    }

    private FrameworkElement CreateShortcutsTab()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel { Spacing = 12 };

        stack.Children.Add(CreateSectionHeader("Global Shortcuts", "System-wide hotkeys available from any application without stealing focus."));

        var shortcuts = new (string Key, string Description)[]
        {
            ("Ctrl + Alt + Space", "Open Spotlight Command Palette"),
            ("Ctrl + Alt + T", "Toggle TopBar Visibility (Show / Hide)"),
            ("Ctrl + Alt + E", "Toggle Panel Expansion (Expand / Collapse)"),
            ("Ctrl + Alt + M", "Navigate to Next Module"),
            ("Ctrl + Alt + P", "Play / Pause Active Media Session"),
            ("Ctrl + Alt + N", "Quick Notes Module"),
            ("Ctrl + Alt + L", "Focus Timer / Lock Mode")
        };

        foreach (var sc in shortcuts)
        {
            var card = new Border
            {
                Background = (Brush)Application.Current.Resources["OverlaySurfaceBrush"],
                BorderBrush = (Brush)Application.Current.Resources["OverlayBorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8)
            };

            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var descText = new TextBlock
            {
                Text = sc.Description,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"]
            };
            Grid.SetColumn(descText, 0);
            row.Children.Add(descText);

            var keyBadge = new Border
            {
                Background = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 255, 255, 255)),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 2, 8, 2)
            };
            var keyText = new TextBlock
            {
                Text = sc.Key,
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = (Brush)Application.Current.Resources["OverlayAccentBrush"]
            };
            keyBadge.Child = keyText;
            Grid.SetColumn(keyBadge, 1);
            row.Children.Add(keyBadge);

            card.Child = row;
            stack.Children.Add(card);
        }

        var resetBtn = new Button
        {
            Content = "Reset Shortcuts to Defaults",
            FontSize = 11,
            Margin = new Thickness(0, 8, 0, 0),
            Padding = new Thickness(12, 6, 12, 6),
            Background = (Brush)Application.Current.Resources["OverlaySurfaceBrush"],
            Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"],
            BorderBrush = (Brush)Application.Current.Resources["OverlayBorderBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6)
        };
        resetBtn.Click += (s, e) =>
        {
            _shortcutService.ResetToDefaults();
            var flyout = new Flyout
            {
                Content = new TextBlock { Text = "Global shortcuts reset to defaults.", FontSize = 11, Margin = new Thickness(8) }
            };
            flyout.ShowAt(resetBtn);
        };
        stack.Children.Add(resetBtn);

        scroll.Content = stack;
        return scroll;
    }

    private FrameworkElement CreatePrivacyTab()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel { Spacing = 14 };

        stack.Children.Add(CreateSectionHeader("Privacy & Local Data", "All processing and storage is 100% local on your machine."));

        var prefs = _settingsService.LoadPreferences();

        // Clipboard opt-in
        var clipSwitch = new ToggleSwitch
        {
            Header = "Clipboard History (Opt-In)",
            IsOn = prefs.ClipboardOptIn,
            OffContent = "Disabled (No clipboard data captured)",
            OnContent = "Enabled (Stores recent text snippets locally)"
        };
        clipSwitch.Toggled += (s, e) =>
        {
            var current = _settingsService.LoadPreferences();
            _settingsService.SavePreferences(current with { ClipboardOptIn = clipSwitch.IsOn });
            _clipboardService.SetOptIn(clipSwitch.IsOn);
            _onSettingsChanged?.Invoke();
        };
        stack.Children.Add(clipSwitch);

        // Notification privacy
        var notifSwitch = new ToggleSwitch
        {
            Header = "Notification Privacy Masking",
            IsOn = _notificationService?.Preferences.PrivacyMode ?? false,
            OffContent = "Full preview visible in ticker",
            OnContent = "Masked (Hides message preview in ticker for privacy)"
        };
        notifSwitch.Toggled += (s, e) =>
        {
            _notificationService?.UpdatePreferences(new NotificationPreferences { PrivacyMode = notifSwitch.IsOn });
            _onSettingsChanged?.Invoke();
        };
        stack.Children.Add(notifSwitch);

        // Local Storage Path Info
        string localAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DynamicTopBar");
        var pathBox = new TextBox
        {
            Header = "Local Storage Directory:",
            Text = localAppData,
            IsReadOnly = true,
            FontSize = 11
        };
        stack.Children.Add(pathBox);

        // Clear data button
        var clearBtn = new Button
        {
            Content = "Clear All Local Data & Logs",
            FontSize = 11,
            Padding = new Thickness(12, 6, 12, 6),
            Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 239, 68, 68)),
            Background = (Brush)Application.Current.Resources["OverlaySurfaceBrush"],
            BorderBrush = (Brush)Application.Current.Resources["OverlayBorderBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6)
        };
        clearBtn.Click += (s, e) =>
        {
            try
            {
                if (Directory.Exists(localAppData))
                {
                    Directory.Delete(localAppData, true);
                }
                var flyout = new Flyout
                {
                    Content = new TextBlock { Text = "Local data cleared successfully.", FontSize = 11, Margin = new Thickness(8) }
                };
                flyout.ShowAt(clearBtn);
            }
            catch (Exception ex)
            {
                var flyout = new Flyout
                {
                    Content = new TextBlock { Text = $"Error clearing data: {ex.Message}", FontSize = 11, Margin = new Thickness(8) }
                };
                flyout.ShowAt(clearBtn);
            }
        };
        stack.Children.Add(clearBtn);

        scroll.Content = stack;
        return scroll;
    }

    private FrameworkElement CreateDiagnosticsTab()
    {
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel { Spacing = 12 };

        stack.Children.Add(CreateSectionHeader("System Diagnostics", "Application health, memory metrics, and runtime verification."));

        long memoryMb = GC.GetTotalMemory(false) / (1024 * 1024);
        int moduleCount = _featureRegistry.Count;

        var cards = new (string Title, string Value)[]
        {
            ("System Status", "Healthy & Operational"),
            ("Runtime Platform", ".NET 9.0 • WinUI 3 • Windows App SDK"),
            ("Target Architecture", "win-x64 Native Desktop Application"),
            ("Managed Memory", $"{memoryMb} MB"),
            ("Registered Modules", $"{moduleCount} active modules")
        };

        foreach (var c in cards)
        {
            var card = new Border
            {
                Background = (Brush)Application.Current.Resources["OverlaySurfaceBrush"],
                BorderBrush = (Brush)Application.Current.Resources["OverlayBorderBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 8, 12, 8)
            };
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock { Text = c.Title, FontSize = 11, Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"] };
            Grid.SetColumn(titleText, 0);
            row.Children.Add(titleText);

            var valText = new TextBlock { Text = c.Value, FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"] };
            Grid.SetColumn(valText, 1);
            row.Children.Add(valText);

            card.Child = row;
            stack.Children.Add(card);
        }

        var testNotifBtn = new Button
        {
            Content = "Send Diagnostic Notification",
            FontSize = 11,
            Margin = new Thickness(0, 8, 0, 0),
            Padding = new Thickness(12, 6, 12, 6),
            Background = (Brush)Application.Current.Resources["OverlaySurfaceBrush"],
            Foreground = (Brush)Application.Current.Resources["OverlayAccentBrush"],
            BorderBrush = (Brush)Application.Current.Resources["OverlayBorderBrush"],
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6)
        };
        testNotifBtn.Click += (s, e) =>
        {
            _notificationService?.PostNotification(
                "Diagnostic Check",
                "DynamicTopBar notification pipeline is operating normally.",
                "System Diagnostics",
                NotificationPriority.Normal,
                "\uE9F9");
        };
        stack.Children.Add(testNotifBtn);

        scroll.Content = stack;
        return scroll;
    }

    private FrameworkElement CreateSectionHeader(string title, string subtitle)
    {
        var panel = new StackPanel { Spacing = 2, Margin = new Thickness(0, 0, 0, 8) };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 16,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"]
        });
        panel.Children.Add(new TextBlock
        {
            Text = subtitle,
            FontSize = 11,
            Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"]
        });
        return panel;
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _isDisposed = true;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        this.Close();
    }
}
