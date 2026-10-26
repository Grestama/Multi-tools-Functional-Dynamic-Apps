using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;
using DynamicTopBar.App.Platform;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;
using WinRT.Interop;
using Win32 = DynamicTopBar.App.Platform.Win32Interop;

namespace DynamicTopBar.App.UI;

/// <summary>
/// Spotlight Command Palette floating overlay window with responsive search,
/// full keyboard navigation (Up, Down, Enter, Escape), module actions, and execution feedback.
/// </summary>
public sealed class CommandPaletteWindow : Window, IDisposable
{
    private readonly ICommandPaletteService _commandPaletteService;
    private readonly IFeatureNavigator _featureNavigator;
    private readonly Action<string>? _onModuleNavigated;

    private readonly IntPtr _hwnd;
    private readonly AppWindow _appWindow;
    private readonly TextBox _searchBox;
    private readonly StackPanel _resultsPanel;
    private readonly TextBlock _statusCountText;

    private IReadOnlyList<PaletteCommand> _currentResults = Array.Empty<PaletteCommand>();
    private int _selectedIndex = 0;
    private bool _isDisposed;

    public bool IsDisposed => _isDisposed;

    public CommandPaletteWindow(
        ICommandPaletteService commandPaletteService,
        IFeatureNavigator featureNavigator,
        Action<string>? onModuleNavigated = null)
    {
        _commandPaletteService = commandPaletteService;
        _featureNavigator = featureNavigator;
        _onModuleNavigated = onModuleNavigated;

        this.Title = "DynamicTopBar — Command Palette";
        _hwnd = WindowNative.GetWindowHandle(this);
        _appWindow = this.AppWindow;

        // Position: 540x360 spotlight overlay centered horizontally, 15% from top
        int widthDip = 540;
        int heightDip = 360;
        uint dpi = Win32.GetDpiForWindow(_hwnd);
        double scaling = dpi > 0 ? dpi / 96.0 : 1.0;
        int pxWidth = (int)(widthDip * scaling);
        int pxHeight = (int)(heightDip * scaling);

        var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
        int posX = displayArea.WorkArea.X + (displayArea.WorkArea.Width - pxWidth) / 2;
        int posY = displayArea.WorkArea.Y + (int)(displayArea.WorkArea.Height * 0.15);
        _appWindow.MoveAndResize(new RectInt32(posX, posY, pxWidth, pxHeight));

        // Set DWM dark mode attribute
        uint darkMode = 1;
        Win32.DwmSetWindowAttribute(_hwnd, 20 /* DWMWA_USE_IMMERSIVE_DARK_MODE */, ref darkMode, sizeof(uint));

        // Root container
        var root = new Grid
        {
            Background = new SolidColorBrush(Windows.UI.Color.FromArgb(250, 18, 18, 22)),
            Padding = new Thickness(16, 14, 16, 12)
        };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Search Input Header
        _searchBox = new TextBox
        {
            PlaceholderText = "Type a command or module name (e.g. todo, notes, play, timer)...",
            FontSize = 13,
            Height = 36,
            Padding = new Thickness(10, 6, 10, 6),
            CornerRadius = new CornerRadius(6),
            Background = (Brush)Application.Current.Resources["OverlaySurfaceBrush"],
            Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"],
            BorderBrush = (Brush)Application.Current.Resources["OverlayBorderBrush"],
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 0, 10)
        };
        _searchBox.TextChanged += OnSearchBoxTextChanged;
        _searchBox.KeyDown += OnSearchBoxKeyDown;
        Grid.SetRow(_searchBox, 0);
        root.Children.Add(_searchBox);

        // Results Scroll Area
        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 0, 0, 8)
        };
        _resultsPanel = new StackPanel { Spacing = 3 };
        scrollViewer.Content = _resultsPanel;
        Grid.SetRow(scrollViewer, 1);
        root.Children.Add(scrollViewer);

        // Footer status bar
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var hintText = new TextBlock
        {
            Text = "↑↓ to navigate • Enter to execute • Esc to dismiss",
            FontSize = 10,
            Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"],
            Opacity = 0.75
        };
        Grid.SetColumn(hintText, 0);
        footerGrid.Children.Add(hintText);

        _statusCountText = new TextBlock
        {
            Text = "0 commands",
            FontSize = 10,
            Foreground = (Brush)Application.Current.Resources["OverlayAccentBrush"]
        };
        Grid.SetColumn(_statusCountText, 1);
        footerGrid.Children.Add(_statusCountText);

        Grid.SetRow(footerGrid, 2);
        root.Children.Add(footerGrid);

        this.Content = root;
        this.Closed += (s, e) => _isDisposed = true;

        // Initial search populates all available commands
        PerformSearch(string.Empty);
    }

    public void FocusSearchBox()
    {
        _searchBox.Text = string.Empty;
        _searchBox.Focus(FocusState.Programmatic);
    }

    private void OnSearchBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        PerformSearch(_searchBox.Text ?? string.Empty);
    }

    private void PerformSearch(string query)
    {
        string? activeModuleId = _featureNavigator.CurrentFeature?.Id;
        var serviceResults = _commandPaletteService.Search(query, activeModuleId);

        var list = new List<PaletteCommand>(serviceResults);

        // Add direct module jump commands if query is empty or matches module name
        var enabledFeatures = _featureNavigator.EnabledFeatures;
        foreach (var feature in enabledFeatures)
        {
            if (string.IsNullOrWhiteSpace(query) ||
                feature.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                feature.Id.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                if (!list.Any(c => c.Id == $"nav-{feature.Id}"))
                {
                    list.Add(new PaletteCommand
                    {
                        Id = $"nav-{feature.Id}",
                        Title = $"Switch to {feature.DisplayName}",
                        Category = "Navigation",
                        Description = $"Switch active TopBar module to {feature.DisplayName}",
                        IconGlyph = feature.IconKey,
                        SyncAction = () =>
                        {
                            _featureNavigator.NavigateTo(feature.Id);
                            _onModuleNavigated?.Invoke(feature.Id);
                        }
                    });
                }
            }
        }

        _currentResults = list;
        _selectedIndex = 0;
        RenderResults();
    }

    private void RenderResults()
    {
        _resultsPanel.Children.Clear();
        _statusCountText.Text = $"{_currentResults.Count} commands";

        if (_currentResults.Count == 0)
        {
            var emptyPanel = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 30, 0, 0),
                Spacing = 6
            };
            emptyPanel.Children.Add(new FontIcon
            {
                Glyph = "\uE721",
                FontSize = 24,
                Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"],
                Opacity = 0.5
            });
            emptyPanel.Children.Add(new TextBlock
            {
                Text = "No matching commands found",
                FontSize = 11,
                Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"]
            });
            _resultsPanel.Children.Add(emptyPanel);
            return;
        }

        for (int i = 0; i < _currentResults.Count; i++)
        {
            var cmd = _currentResults[i];
            bool isSelected = (i == _selectedIndex);

            var itemBorder = new Border
            {
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 10, 6),
                Background = isSelected
                    ? (Brush)Application.Current.Resources["OverlaySurfaceBrush"]
                    : new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderBrush = isSelected
                    ? (Brush)Application.Current.Resources["OverlayBorderBrush"]
                    : new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                BorderThickness = new Thickness(1)
            };

            var itemGrid = new Grid();
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Command Icon
            var icon = new FontIcon
            {
                Glyph = cmd.IconGlyph,
                FontSize = 14,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = isSelected
                    ? (Brush)Application.Current.Resources["OverlayAccentBrush"]
                    : (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"]
            };
            Grid.SetColumn(icon, 0);
            itemGrid.Children.Add(icon);

            // Title and Description
            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var title = new TextBlock
            {
                Text = cmd.Title,
                FontSize = 12,
                FontWeight = isSelected ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
                Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"]
            };
            var desc = new TextBlock
            {
                Text = $"{cmd.Category}{(string.IsNullOrEmpty(cmd.Description) ? "" : " • " + cmd.Description)}",
                FontSize = 10,
                Foreground = (Brush)Application.Current.Resources["OverlayTextSecondaryBrush"]
            };
            textStack.Children.Add(title);
            textStack.Children.Add(desc);
            Grid.SetColumn(textStack, 1);
            itemGrid.Children.Add(textStack);

            // Action / Shortcut Badge
            if (!string.IsNullOrEmpty(cmd.ShortcutHint))
            {
                var badge = new Border
                {
                    Background = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 255, 255)),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    VerticalAlignment = VerticalAlignment.Center
                };
                badge.Child = new TextBlock
                {
                    Text = cmd.ShortcutHint,
                    FontSize = 9,
                    Foreground = (Brush)Application.Current.Resources["OverlayAccentBrush"]
                };
                Grid.SetColumn(badge, 2);
                itemGrid.Children.Add(badge);
            }
            else if (isSelected)
            {
                var runBadge = new Border
                {
                    Background = (Brush)Application.Current.Resources["OverlayAccentBrush"],
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 2, 6, 2),
                    VerticalAlignment = VerticalAlignment.Center
                };
                runBadge.Child = new TextBlock
                {
                    Text = "↵ Enter",
                    FontSize = 9,
                    Foreground = (Brush)Application.Current.Resources["OverlayTextPrimaryBrush"]
                };
                Grid.SetColumn(runBadge, 2);
                itemGrid.Children.Add(runBadge);
            }

            itemBorder.Child = itemGrid;

            // Click execution
            int clickedIndex = i;
            itemBorder.PointerPressed += async (s, e) =>
            {
                _selectedIndex = clickedIndex;
                await ExecuteCurrentSelectionAsync();
            };

            _resultsPanel.Children.Add(itemBorder);
        }
    }

    private async void OnSearchBoxKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            e.Handled = true;
            this.Close();
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Down)
        {
            if (_currentResults.Count > 0)
            {
                _selectedIndex = (_selectedIndex + 1) % _currentResults.Count;
                RenderResults();
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Up)
        {
            if (_currentResults.Count > 0)
            {
                _selectedIndex = (_selectedIndex - 1 + _currentResults.Count) % _currentResults.Count;
                RenderResults();
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            await ExecuteCurrentSelectionAsync();
            return;
        }
    }

    private async Task ExecuteCurrentSelectionAsync()
    {
        if (_currentResults.Count == 0 || _selectedIndex < 0 || _selectedIndex >= _currentResults.Count)
        {
            return;
        }

        var cmd = _currentResults[_selectedIndex];
        this.Close();

        await _commandPaletteService.ExecuteCommandAsync(cmd);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        this.Close();
    }
}
