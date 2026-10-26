using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.App.UI;

/// <summary>
/// Secondary dedicated desktop window providing an expansive workspace for heavy tool operations
/// including image transformation, batch renaming, archive creation, regex debugging, and clipboard inspection.
/// </summary>
public sealed class ToolsWindow : Window
{
    private readonly IToolRegistry _toolRegistry;
    private readonly IDeveloperUtilitiesService _devUtils;
    private readonly IFileToolsService _fileTools;
    private readonly IImageToolsService? _imageTools;
    private readonly IClipboardService _clipboard;
    private readonly IPluginManager? _pluginManager;
    private readonly IWorkflowEngine? _workflowEngine;
    private readonly IAiAssistantService? _aiAssistant;
    private readonly IPersonalizationService? _personalizationService;
    private readonly IDiagnosticsService? _diagnosticsService;
    private readonly Action? _requestShowTopBar;

    // Navigation buttons
    private Button _navImgBtn = null!;
    private Button _navFileBtn = null!;
    private Button _navDevBtn = null!;
    private Button _navClipBtn = null!;
    private Button _navWorkflowBtn = null!;
    private Button _navPluginBtn = null!;
    private Button _navDiagnosticsBtn = null!;
    private Button _navAiBtn = null!;

    // Top-level workspace containers
    private Grid _imageWorkspaceGrid = null!;
    private Grid _fileWorkspaceGrid = null!;
    private Grid _devUtilsWorkspaceGrid = null!;
    private Grid _clipboardWorkspaceGrid = null!;
    private Grid _workflowWorkspaceGrid = null!;
    private Grid _pluginWorkspaceGrid = null!;
    private Grid _diagnosticsWorkspaceGrid = null!;
    private Grid _aiWorkspaceGrid = null!;
    private TextBlock _globalStatusText = null!;

    // Image tools controls
    private TextBox _imgSourcePathBox = null!;
    private TextBlock _imgInspectInfoText = null!;
    private ComboBox _imgFormatCombo = null!;
    private Slider _imgQualitySlider = null!;
    private TextBlock _imgQualityValueText = null!;
    private TextBox _imgTargetWidthBox = null!;
    private TextBox _imgTargetHeightBox = null!;
    private CheckBox _imgMaintainAspectCheck = null!;
    private ComboBox _imgRotateCombo = null!;
    private ComboBox _imgFlipCombo = null!;
    private CheckBox _imgStripMetaCheck = null!;
    private CheckBox _imgOverwriteCheck = null!;
    private TextBox _imgOutputDirBox = null!;
    private TextBlock _imgResultReportText = null!;
    private ProgressBar _imgProgressBar = null!;

    // File tools controls
    private string _activeFileSubTab = "rename";
    private Grid _fileRenameSubPanel = null!;
    private Grid _fileGenericSubPanel = null!;
    private TextBox _fileRenameDirBox = null!;
    private TextBox _fileRenameParamBox = null!;
    private ComboBox _fileRenameRuleCombo = null!;
    private TextBlock _fileRenameOutputText = null!;
    private TextBox _fileGenericInputBox = null!;
    private TextBlock _fileGenericOutputText = null!;
    private readonly List<Button> _fileSubTabButtons = new();

    // Dev utilities controls
    private string _activeDevTool = "json";
    private TextBlock _devToolTitleText = null!;
    private TextBox _devInputTextBox = null!;
    private ComboBox _devOptionCombo = null!;
    private TextBlock _devOutputTextBox = null!;
    private TextBlock _devStatusMessageText = null!;
    private readonly List<Button> _devToolChipButtons = new();

    // Clipboard controls
    private TextBox _clipSearchBox = null!;
    private ComboBox _clipCategoryFilterCombo = null!;
    private StackPanel _clipSnippetsListPanel = null!;

    public ToolsWindow(
        IToolRegistry toolRegistry,
        IDeveloperUtilitiesService devUtils,
        IFileToolsService fileTools,
        IImageToolsService? imageTools,
        IClipboardService clipboard,
        IPluginManager? pluginManager = null,
        IWorkflowEngine? workflowEngine = null,
        IAiAssistantService? aiAssistant = null,
        IPersonalizationService? personalizationService = null,
        IDiagnosticsService? diagnosticsService = null,
        Action? requestShowTopBar = null)
    {
        _toolRegistry = toolRegistry ?? throw new ArgumentNullException(nameof(toolRegistry));
        _devUtils = devUtils ?? throw new ArgumentNullException(nameof(devUtils));
        _fileTools = fileTools ?? throw new ArgumentNullException(nameof(fileTools));
        _imageTools = imageTools;
        _clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
        _pluginManager = pluginManager;
        _workflowEngine = workflowEngine;
        _aiAssistant = aiAssistant;
        _personalizationService = personalizationService;
        _diagnosticsService = diagnosticsService;
        _requestShowTopBar = requestShowTopBar;

        Title = "DynamicTopBar — Universal Tools Workspace";

        this.Content = BuildUi();
        ConfigureWindowDimensions();
    }

    private void ConfigureWindowDimensions()
    {
        try
        {
            IntPtr hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(windowId);
            if (appWindow != null)
            {
                appWindow.Resize(new SizeInt32(960, 650));
            }
        }
        catch { }
    }

    public void SelectCategory(string category)
    {
        switch (category.ToLowerInvariant())
        {
            case "image":
            case "image tools":
                ActivateCategory("image");
                break;
            case "file":
            case "file tools":
                ActivateCategory("file");
                break;
            case "dev":
            case "developer":
                ActivateCategory("dev");
                break;
            case "clipboard":
                ActivateCategory("clipboard");
                break;
            case "workflow":
            case "workflows":
            case "automation":
                ActivateCategory("workflows");
                break;
            case "plugin":
            case "plugins":
                ActivateCategory("plugins");
                break;
            case "diagnostics":
            case "health":
                ActivateCategory("diagnostics");
                break;
            case "ai":
            case "assistant":
                ActivateCategory("ai");
                break;
        }
    }

    private UIElement BuildUi()
    {
        var rootGrid = new Grid { Background = CreateBrush(0x09, 0x09, 0x09) };
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header Bar
        var headerBorder = new Border
        {
            Background = CreateBrush(0x17, 0x17, 0x17),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(16, 10, 16, 10)
        };

        var headerGrid = new Grid();
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Brand
        var brandStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        var brandIconBorder = new Border { Width = 24, Height = 24, CornerRadius = new CornerRadius(12), Background = CreateBrush(0x00, 0xB4, 0xFF) };
        brandIconBorder.Child = new TextBlock
        {
            Text = "\uE74C",
            FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
            FontSize = 12,
            Foreground = CreateBrush(0x09, 0x09, 0x09),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        brandStack.Children.Add(brandIconBorder);
        brandStack.Children.Add(new TextBlock { Text = "Universal Tools Workspace", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF), VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(brandStack, 0);
        headerGrid.Children.Add(brandStack);

        // Segmented Nav
        var navStack = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Spacing = 6 };
        _navImgBtn = CreateNavButton("Image", () => ActivateCategory("image"), true);
        _navFileBtn = CreateNavButton("Files", () => ActivateCategory("file"), false);
        _navDevBtn = CreateNavButton("Developer", () => ActivateCategory("dev"), false);
        _navClipBtn = CreateNavButton("Clipboard", () => ActivateCategory("clipboard"), false);
        _navWorkflowBtn = CreateNavButton("Workflows", () => ActivateCategory("workflows"), false);
        _navPluginBtn = CreateNavButton("Plugins", () => ActivateCategory("plugins"), false);
        _navDiagnosticsBtn = CreateNavButton("Diagnostics", () => ActivateCategory("diagnostics"), false);
        _navAiBtn = CreateNavButton("AI Assistant", () => ActivateCategory("ai"), false);

        navStack.Children.Add(_navImgBtn);
        navStack.Children.Add(_navFileBtn);
        navStack.Children.Add(_navDevBtn);
        navStack.Children.Add(_navClipBtn);
        navStack.Children.Add(_navWorkflowBtn);
        navStack.Children.Add(_navPluginBtn);
        navStack.Children.Add(_navDiagnosticsBtn);
        navStack.Children.Add(_navAiBtn);
        Grid.SetColumn(navStack, 1);
        headerGrid.Children.Add(navStack);

        // Right quick button
        var topBarQuickBtn = new Button
        {
            Content = "Show TopBar",
            FontSize = 10,
            Height = 26,
            Padding = new Thickness(10, 0, 10, 0),
            CornerRadius = new CornerRadius(6),
            Background = CreateBrush(0x22, 0x22, 0x22),
            Foreground = CreateBrush(0xFF, 0xFF, 0xFF)
        };
        topBarQuickBtn.Click += (s, e) => _requestShowTopBar?.Invoke();
        Grid.SetColumn(topBarQuickBtn, 2);
        headerGrid.Children.Add(topBarQuickBtn);

        headerBorder.Child = headerGrid;
        Grid.SetRow(headerBorder, 0);
        rootGrid.Children.Add(headerBorder);

        // Workspaces Container
        var workspaceHost = new Grid { Margin = new Thickness(16, 12, 16, 12) };
        _imageWorkspaceGrid = BuildImageWorkspace();
        _fileWorkspaceGrid = BuildFileWorkspace();
        _devUtilsWorkspaceGrid = BuildDevUtilsWorkspace();
        _clipboardWorkspaceGrid = BuildClipboardWorkspace();
        _workflowWorkspaceGrid = BuildWorkflowWorkspace();
        _pluginWorkspaceGrid = BuildPluginWorkspace();
        _diagnosticsWorkspaceGrid = BuildDiagnosticsWorkspace();
        _aiWorkspaceGrid = BuildAiWorkspace();

        workspaceHost.Children.Add(_imageWorkspaceGrid);
        workspaceHost.Children.Add(_fileWorkspaceGrid);
        workspaceHost.Children.Add(_devUtilsWorkspaceGrid);
        workspaceHost.Children.Add(_clipboardWorkspaceGrid);
        workspaceHost.Children.Add(_workflowWorkspaceGrid);
        workspaceHost.Children.Add(_pluginWorkspaceGrid);
        workspaceHost.Children.Add(_diagnosticsWorkspaceGrid);
        workspaceHost.Children.Add(_aiWorkspaceGrid);

        Grid.SetRow(workspaceHost, 1);
        rootGrid.Children.Add(workspaceHost);

        // Footer Bar
        var footerBorder = new Border
        {
            Background = CreateBrush(0x12, 0x12, 0x12),
            BorderBrush = CreateBrush(0x22, 0x22, 0x22),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Padding = new Thickness(16, 6, 16, 6)
        };
        var footerGrid = new Grid();
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _globalStatusText = new TextBlock { Text = "Ready. Universal Tools Engine v1.0.0", FontSize = 10, Foreground = CreateBrush(0x88, 0x88, 0x88), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(_globalStatusText, 0);
        footerGrid.Children.Add(_globalStatusText);

        var engineTag = new TextBlock { Text = "DynamicTopBar Desktop Tools", FontSize = 10, Foreground = CreateBrush(0x55, 0x55, 0x55), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(engineTag, 1);
        footerGrid.Children.Add(engineTag);

        footerBorder.Child = footerGrid;
        Grid.SetRow(footerBorder, 2);
        rootGrid.Children.Add(footerBorder);

        ActivateCategory("image");
        return rootGrid;
    }

    private Button CreateNavButton(string title, Action action, bool isActive)
    {
        var btn = new Button
        {
            Content = title,
            FontSize = 11,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Height = 28,
            Padding = new Thickness(10, 2, 10, 2),
            CornerRadius = new CornerRadius(14),
            Background = isActive ? CreateBrush(0x00, 0xB4, 0xFF) : CreateBrush(0x22, 0x22, 0x22),
            Foreground = isActive ? CreateBrush(0x09, 0x09, 0x09) : CreateBrush(0xA0, 0xA0, 0xA0)
        };
        btn.Click += (s, e) => action();
        return btn;
    }

    private void ActivateCategory(string category)
    {
        var activeBrush = CreateBrush(0x00, 0xB4, 0xFF);
        var inactiveBrush = CreateBrush(0x22, 0x22, 0x22);
        var activeText = CreateBrush(0x09, 0x09, 0x09);
        var inactiveText = CreateBrush(0xA0, 0xA0, 0xA0);

        _navImgBtn.Background = category == "image" ? activeBrush : inactiveBrush;
        _navImgBtn.Foreground = category == "image" ? activeText : inactiveText;
        _navFileBtn.Background = category == "file" ? activeBrush : inactiveBrush;
        _navFileBtn.Foreground = category == "file" ? activeText : inactiveText;
        _navDevBtn.Background = category == "dev" ? activeBrush : inactiveBrush;
        _navDevBtn.Foreground = category == "dev" ? activeText : inactiveText;
        _navClipBtn.Background = category == "clipboard" ? activeBrush : inactiveBrush;
        _navClipBtn.Foreground = category == "clipboard" ? activeText : inactiveText;
        _navWorkflowBtn.Background = category == "workflows" ? activeBrush : inactiveBrush;
        _navWorkflowBtn.Foreground = category == "workflows" ? activeText : inactiveText;
        _navPluginBtn.Background = category == "plugins" ? activeBrush : inactiveBrush;
        _navPluginBtn.Foreground = category == "plugins" ? activeText : inactiveText;
        _navDiagnosticsBtn.Background = category == "diagnostics" ? activeBrush : inactiveBrush;
        _navDiagnosticsBtn.Foreground = category == "diagnostics" ? activeText : inactiveText;
        _navAiBtn.Background = category == "ai" ? activeBrush : inactiveBrush;
        _navAiBtn.Foreground = category == "ai" ? activeText : inactiveText;

        _imageWorkspaceGrid.Visibility = category == "image" ? Visibility.Visible : Visibility.Collapsed;
        _fileWorkspaceGrid.Visibility = category == "file" ? Visibility.Visible : Visibility.Collapsed;
        _devUtilsWorkspaceGrid.Visibility = category == "dev" ? Visibility.Visible : Visibility.Collapsed;
        _clipboardWorkspaceGrid.Visibility = category == "clipboard" ? Visibility.Visible : Visibility.Collapsed;
        _workflowWorkspaceGrid.Visibility = category == "workflows" ? Visibility.Visible : Visibility.Collapsed;
        _pluginWorkspaceGrid.Visibility = category == "plugins" ? Visibility.Visible : Visibility.Collapsed;
        _diagnosticsWorkspaceGrid.Visibility = category == "diagnostics" ? Visibility.Visible : Visibility.Collapsed;
        _aiWorkspaceGrid.Visibility = category == "ai" ? Visibility.Visible : Visibility.Collapsed;

        _globalStatusText.Text = $"{category.ToUpperInvariant()} Tools Workspace Active.";
        if (category == "clipboard") RefreshClipboardList();
        if (category == "workflows") RefreshWorkflowsList();
        if (category == "plugins") RefreshPluginsList();
        if (category == "diagnostics") RefreshDiagnosticsView();
    }

    #region Image Workspace Implementation

    private Grid BuildImageWorkspace()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Source Box Row
        var topBorder = new Border
        {
            Background = CreateBrush(0x17, 0x17, 0x17),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var topGrid = new Grid();
        topGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        topGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _imgSourcePathBox = new TextBox
        {
            PlaceholderText = "Enter full source image path (e.g. C:\\Photos\\sample.jpg)...",
            FontSize = 11,
            Background = CreateBrush(0x0D, 0x0D, 0x0D),
            Foreground = CreateBrush(0xFF, 0xFF, 0xFF),
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetRow(_imgSourcePathBox, 0);
        Grid.SetColumn(_imgSourcePathBox, 0);
        topGrid.Children.Add(_imgSourcePathBox);

        var topBtnStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var inspectBtn = new Button { Content = "Inspect", FontSize = 10, Height = 32, Padding = new Thickness(10, 0, 10, 0), Background = CreateBrush(0x22, 0x22, 0x22), Foreground = CreateBrush(0xFF, 0xFF, 0xFF) };
        inspectBtn.Click += async (s, e) => await OnImgInspectClickAsync();
        var processBtn = new Button { Content = "Process Single", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Height = 32, Padding = new Thickness(10, 0, 10, 0), Background = CreateBrush(0x00, 0xB4, 0xFF), Foreground = CreateBrush(0x09, 0x09, 0x09) };
        processBtn.Click += async (s, e) => await OnImgProcessClickAsync();
        topBtnStack.Children.Add(inspectBtn);
        topBtnStack.Children.Add(processBtn);
        Grid.SetRow(topBtnStack, 0);
        Grid.SetColumn(topBtnStack, 1);
        topGrid.Children.Add(topBtnStack);

        _imgInspectInfoText = new TextBlock { Text = "No image inspected. Enter path and click Inspect or Process Single.", FontSize = 10, Foreground = CreateBrush(0xA0, 0xA0, 0xA0), Margin = new Thickness(0, 6, 0, 0) };
        Grid.SetRow(_imgInspectInfoText, 1);
        Grid.SetColumnSpan(_imgInspectInfoText, 2);
        topGrid.Children.Add(_imgInspectInfoText);
        topBorder.Child = topGrid;
        Grid.SetRow(topBorder, 0);
        grid.Children.Add(topBorder);

        // Parameters & Results Row
        var bodyGrid = new Grid();
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left Controls
        var leftBorder = new Border
        {
            Background = CreateBrush(0x17, 0x17, 0x17),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 10, 0)
        };
        var leftStack = new StackPanel { Spacing = 10 };
        leftStack.Children.Add(new TextBlock { Text = "Transform & Compression Settings", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF) });

        // Format
        var fmtStack = new StackPanel { Spacing = 4 };
        fmtStack.Children.Add(new TextBlock { Text = "Target Format", FontSize = 10, Foreground = CreateBrush(0xA0, 0xA0, 0xA0) });
        _imgFormatCombo = new ComboBox { SelectedIndex = 0, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Stretch };
        _imgFormatCombo.Items.Add(new ComboBoxItem { Content = "JPEG" });
        _imgFormatCombo.Items.Add(new ComboBoxItem { Content = "PNG (Lossless)" });
        _imgFormatCombo.Items.Add(new ComboBoxItem { Content = "WebP" });
        fmtStack.Children.Add(_imgFormatCombo);
        leftStack.Children.Add(fmtStack);

        // Quality
        var qStack = new StackPanel { Spacing = 4 };
        var qHeaderGrid = new Grid();
        qHeaderGrid.Children.Add(new TextBlock { Text = "Quality", FontSize = 10, Foreground = CreateBrush(0xA0, 0xA0, 0xA0), HorizontalAlignment = HorizontalAlignment.Left });
        _imgQualityValueText = new TextBlock { Text = "85%", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0x00, 0xB4, 0xFF), HorizontalAlignment = HorizontalAlignment.Right };
        qHeaderGrid.Children.Add(_imgQualityValueText);
        qStack.Children.Add(qHeaderGrid);
        _imgQualitySlider = new Slider { Minimum = 1, Maximum = 100, Value = 85 };
        _imgQualitySlider.ValueChanged += (s, e) => _imgQualityValueText.Text = $"{(int)e.NewValue}%";
        qStack.Children.Add(_imgQualitySlider);
        leftStack.Children.Add(qStack);

        // Resize
        var resStack = new StackPanel { Spacing = 4 };
        resStack.Children.Add(new TextBlock { Text = "Resize Dimensions (px)", FontSize = 10, Foreground = CreateBrush(0xA0, 0xA0, 0xA0) });
        var dimGrid = new Grid();
        dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        dimGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _imgTargetWidthBox = new TextBox { PlaceholderText = "Width", FontSize = 10, Background = CreateBrush(0x0D, 0x0D, 0x0D), Foreground = CreateBrush(0xFF, 0xFF, 0xFF) };
        var multText = new TextBlock { Text = "×", FontSize = 11, Foreground = CreateBrush(0xA0, 0xA0, 0xA0), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0, 6, 0) };
        _imgTargetHeightBox = new TextBox { PlaceholderText = "Height", FontSize = 10, Background = CreateBrush(0x0D, 0x0D, 0x0D), Foreground = CreateBrush(0xFF, 0xFF, 0xFF) };
        Grid.SetColumn(_imgTargetWidthBox, 0);
        Grid.SetColumn(multText, 1);
        Grid.SetColumn(_imgTargetHeightBox, 2);
        dimGrid.Children.Add(_imgTargetWidthBox);
        dimGrid.Children.Add(multText);
        dimGrid.Children.Add(_imgTargetHeightBox);
        resStack.Children.Add(dimGrid);
        _imgMaintainAspectCheck = new CheckBox { Content = "Maintain Aspect Ratio", IsChecked = true, FontSize = 10, Foreground = CreateBrush(0xA0, 0xA0, 0xA0) };
        resStack.Children.Add(_imgMaintainAspectCheck);
        leftStack.Children.Add(resStack);

        // Rotate & Flip
        var rotFlipGrid = new Grid();
        rotFlipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rotFlipGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _imgRotateCombo = new ComboBox { SelectedIndex = 0, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 0, 4, 0) };
        _imgRotateCombo.Items.Add(new ComboBoxItem { Content = "Rotate: None" });
        _imgRotateCombo.Items.Add(new ComboBoxItem { Content = "Rotate 90° CW" });
        _imgRotateCombo.Items.Add(new ComboBoxItem { Content = "Rotate 180°" });
        _imgRotateCombo.Items.Add(new ComboBoxItem { Content = "Rotate 270° CW" });
        _imgFlipCombo = new ComboBox { SelectedIndex = 0, FontSize = 10, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(4, 0, 0, 0) };
        _imgFlipCombo.Items.Add(new ComboBoxItem { Content = "Flip: None" });
        _imgFlipCombo.Items.Add(new ComboBoxItem { Content = "Flip Horizontal" });
        _imgFlipCombo.Items.Add(new ComboBoxItem { Content = "Flip Vertical" });
        Grid.SetColumn(_imgRotateCombo, 0);
        Grid.SetColumn(_imgFlipCombo, 1);
        rotFlipGrid.Children.Add(_imgRotateCombo);
        rotFlipGrid.Children.Add(_imgFlipCombo);
        leftStack.Children.Add(rotFlipGrid);

        // Metadata & Overwrite
        _imgStripMetaCheck = new CheckBox { Content = "Strip EXIF Metadata", IsChecked = true, FontSize = 10, Foreground = CreateBrush(0xA0, 0xA0, 0xA0) };
        _imgOverwriteCheck = new CheckBox { Content = "Overwrite Destination", IsChecked = false, FontSize = 10, Foreground = CreateBrush(0xA0, 0xA0, 0xA0) };
        _imgOutputDirBox = new TextBox { PlaceholderText = "Output directory (optional)", FontSize = 10, Background = CreateBrush(0x0D, 0x0D, 0x0D), Foreground = CreateBrush(0xFF, 0xFF, 0xFF) };
        leftStack.Children.Add(_imgStripMetaCheck);
        leftStack.Children.Add(_imgOverwriteCheck);
        leftStack.Children.Add(_imgOutputDirBox);

        leftBorder.Child = new ScrollViewer { Content = leftStack, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetColumn(leftBorder, 0);
        bodyGrid.Children.Add(leftBorder);

        // Right Output Area
        var rightBorder = new Border
        {
            Background = CreateBrush(0x17, 0x17, 0x17),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14)
        };
        var rightGrid = new Grid();
        rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        rightGrid.Children.Add(new TextBlock { Text = "Processing Statistics & Output", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF), Margin = new Thickness(0, 0, 0, 8) });

        var reportBorder = new Border { Background = CreateBrush(0x0D, 0x0D, 0x0D), BorderBrush = CreateBrush(0x22, 0x22, 0x22), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(10) };
        _imgResultReportText = new TextBlock { Text = "Ready. Set parameters on the left and click 'Process Single'.", FontSize = 11, FontFamily = new FontFamily("Consolas"), Foreground = CreateBrush(0xE0, 0xE0, 0xE0), TextWrapping = TextWrapping.Wrap };
        reportBorder.Child = new ScrollViewer { Content = _imgResultReportText, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(reportBorder, 1);
        rightGrid.Children.Add(reportBorder);

        var rightBottomGrid = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        rightBottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rightBottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _imgProgressBar = new ProgressBar { Value = 0, Maximum = 100, Height = 4, VerticalAlignment = VerticalAlignment.Center };
        var copyReportBtn = new Button { Content = "Copy Report", FontSize = 10, Height = 26, Margin = new Thickness(8, 0, 0, 0) };
        copyReportBtn.Click += (s, e) => CopyToClipboard(_imgResultReportText.Text);
        Grid.SetColumn(_imgProgressBar, 0);
        Grid.SetColumn(copyReportBtn, 1);
        rightBottomGrid.Children.Add(_imgProgressBar);
        rightBottomGrid.Children.Add(copyReportBtn);
        Grid.SetRow(rightBottomGrid, 2);
        rightGrid.Children.Add(rightBottomGrid);

        rightBorder.Child = rightGrid;
        Grid.SetColumn(rightBorder, 1);
        bodyGrid.Children.Add(rightBorder);

        Grid.SetRow(bodyGrid, 1);
        grid.Children.Add(bodyGrid);
        return grid;
    }

    private async Task OnImgInspectClickAsync()
    {
        string path = _imgSourcePathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _imgInspectInfoText.Text = "Source file does not exist or invalid path.";
            return;
        }

        if (_imageTools == null)
        {
            _imgInspectInfoText.Text = "Image tools service not available on this platform.";
            return;
        }

        try
        {
            var info = await _imageTools.InspectImageAsync(path);
            double kb = info.FileSizeBytes / 1024.0;
            _imgInspectInfoText.Text = $"Format: {info.Format} | Resolution: {info.Width} × {info.Height} px | Size: {kb:F1} KB | Alpha: {(info.HasAlphaChannel ? "Yes" : "No")}";
        }
        catch (Exception ex)
        {
            _imgInspectInfoText.Text = $"Inspection error: {ex.Message}";
        }
    }

    private async Task OnImgProcessClickAsync()
    {
        string path = _imgSourcePathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            _imgResultReportText.Text = "Please enter a valid existing image file path.";
            return;
        }

        if (_imageTools == null)
        {
            _imgResultReportText.Text = "Hardware-accelerated image service is unavailable.";
            return;
        }

        _imgResultReportText.Text = "Processing image...";
        _imgProgressBar.Value = 30;

        try
        {
            var format = _imgFormatCombo.SelectedIndex switch
            {
                1 => ImageFormatTarget.Png,
                2 => ImageFormatTarget.WebP,
                _ => ImageFormatTarget.Jpeg
            };

            uint? targetW = uint.TryParse(_imgTargetWidthBox.Text.Trim(), out uint w) ? w : null;
            uint? targetH = uint.TryParse(_imgTargetHeightBox.Text.Trim(), out uint h) ? h : null;

            var rotate = _imgRotateCombo.SelectedIndex switch
            {
                1 => ImageRotationAngle.Rotate90,
                2 => ImageRotationAngle.Rotate180,
                3 => ImageRotationAngle.Rotate270,
                _ => ImageRotationAngle.None
            };

            var flip = _imgFlipCombo.SelectedIndex switch
            {
                1 => ImageFlipMode.Horizontal,
                2 => ImageFlipMode.Vertical,
                _ => ImageFlipMode.None
            };

            var options = new ImageProcessOptions
            {
                TargetFormat = format,
                QualityPercent = (int)_imgQualitySlider.Value,
                TargetWidth = targetW,
                TargetHeight = targetH,
                MaintainAspectRatio = _imgMaintainAspectCheck.IsChecked ?? true,
                Rotation = rotate,
                Flip = flip,
                StripMetadata = _imgStripMetaCheck.IsChecked ?? true,
                OverwriteExisting = _imgOverwriteCheck.IsChecked ?? false,
                OutputDirectory = string.IsNullOrWhiteSpace(_imgOutputDirBox.Text) ? null : _imgOutputDirBox.Text.Trim()
            };

            var res = await _imageTools.ProcessImageAsync(path, options);
            _imgProgressBar.Value = 100;

            if (res.Success)
            {
                double origKb = res.OriginalSizeBytes / 1024.0;
                double procKb = res.ProcessedSizeBytes / 1024.0;
                double savedKb = res.SavedBytes / 1024.0;

                _imgResultReportText.Text =
                    $"SUCCESS\n" +
                    $"Output File : {res.OutputFilePath}\n" +
                    $"Dimensions  : {res.OriginalWidth}×{res.OriginalHeight} -> {res.ProcessedWidth}×{res.ProcessedHeight} px\n" +
                    $"Original    : {origKb:F1} KB ({res.OriginalSizeBytes:N0} bytes)\n" +
                    $"Result      : {procKb:F1} KB ({res.ProcessedSizeBytes:N0} bytes)\n" +
                    $"Savings     : {res.SavingsPercentage:F1}% ({savedKb:F1} KB saved)";
            }
            else
            {
                _imgResultReportText.Text = $"FAILED: {res.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            _imgProgressBar.Value = 0;
            _imgResultReportText.Text = $"Error during processing: {ex.Message}";
        }
    }

    #endregion

    #region File Workspace Implementation

    private Grid BuildFileWorkspace()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var subTabsStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 0, 0, 10) };
        AddFileSubTabButton(subTabsStack, "Batch Rename", "rename", true);
        AddFileSubTabButton(subTabsStack, "ZIP & Unzip", "zip", false);
        AddFileSubTabButton(subTabsStack, "Folder Analyzer", "size", false);
        AddFileSubTabButton(subTabsStack, "File Search", "search", false);
        AddFileSubTabButton(subTabsStack, "File Converter", "convert", false);
        AddFileSubTabButton(subTabsStack, "Hash Checksum", "hash", false);
        Grid.SetRow(subTabsStack, 0);
        grid.Children.Add(subTabsStack);

        var contentBorder = new Border
        {
            Background = CreateBrush(0x17, 0x17, 0x17),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14)
        };
        var innerGrid = new Grid();
        innerGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        innerGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // SubPanel 1: Rename
        _fileRenameSubPanel = new Grid();
        _fileRenameSubPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _fileRenameSubPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var renameControls = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        renameControls.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        renameControls.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        renameControls.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        renameControls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        renameControls.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _fileRenameDirBox = new TextBox { PlaceholderText = "Enter directory containing files to rename...", FontSize = 10, Background = CreateBrush(0x0D, 0x0D, 0x0D), Foreground = CreateBrush(0xFF, 0xFF, 0xFF), Margin = new Thickness(0, 0, 8, 6) };
        _fileRenameRuleCombo = new ComboBox { SelectedIndex = 0, FontSize = 10, Margin = new Thickness(0, 0, 8, 6) };
        _fileRenameRuleCombo.Items.Add(new ComboBoxItem { Content = "Replace Text" });
        _fileRenameRuleCombo.Items.Add(new ComboBoxItem { Content = "Prefix & Suffix" });
        _fileRenameRuleCombo.Items.Add(new ComboBoxItem { Content = "Padded Numbering" });
        _fileRenameRuleCombo.Items.Add(new ComboBoxItem { Content = "Change to Lowercase" });
        _fileRenameRuleCombo.Items.Add(new ComboBoxItem { Content = "Change to Uppercase" });
        _fileRenameRuleCombo.Items.Add(new ComboBoxItem { Content = "Change Extension" });

        var renameBtns = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var prevBtn = new Button { Content = "Preview", FontSize = 10, Height = 30, Background = CreateBrush(0x22, 0x22, 0x22), Foreground = CreateBrush(0xFF, 0xFF, 0xFF) };
        prevBtn.Click += async (s, e) => await OnFileRenamePreviewClickAsync();
        var execBtn = new Button { Content = "Execute", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Height = 30, Background = CreateBrush(0x00, 0xB4, 0xFF), Foreground = CreateBrush(0x09, 0x09, 0x09) };
        execBtn.Click += async (s, e) => await OnFileRenameExecuteClickAsync();
        renameBtns.Children.Add(prevBtn);
        renameBtns.Children.Add(execBtn);

        Grid.SetColumn(_fileRenameDirBox, 0);
        Grid.SetColumn(_fileRenameRuleCombo, 1);
        Grid.SetColumn(renameBtns, 2);
        renameControls.Children.Add(_fileRenameDirBox);
        renameControls.Children.Add(_fileRenameRuleCombo);
        renameControls.Children.Add(renameBtns);

        _fileRenameParamBox = new TextBox { PlaceholderText = "Parameters: FindText|ReplaceText OR Prefix|Suffix OR .newExt", FontSize = 10, Background = CreateBrush(0x0D, 0x0D, 0x0D), Foreground = CreateBrush(0xFF, 0xFF, 0xFF) };
        Grid.SetRow(_fileRenameParamBox, 1);
        Grid.SetColumnSpan(_fileRenameParamBox, 3);
        renameControls.Children.Add(_fileRenameParamBox);

        Grid.SetRow(renameControls, 0);
        _fileRenameSubPanel.Children.Add(renameControls);

        var renameOutputBorder = new Border { Background = CreateBrush(0x0D, 0x0D, 0x0D), BorderBrush = CreateBrush(0x22, 0x22, 0x22), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(10) };
        _fileRenameOutputText = new TextBlock { Text = "Specify directory and rule above, then click 'Preview'.", FontSize = 11, FontFamily = new FontFamily("Consolas"), Foreground = CreateBrush(0xE0, 0xE0, 0xE0), TextWrapping = TextWrapping.Wrap };
        renameOutputBorder.Child = new ScrollViewer { Content = _fileRenameOutputText, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(renameOutputBorder, 1);
        _fileRenameSubPanel.Children.Add(renameOutputBorder);
        innerGrid.Children.Add(_fileRenameSubPanel);

        // SubPanel 2: Generic
        _fileGenericSubPanel = new Grid { Visibility = Visibility.Collapsed };
        _fileGenericSubPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _fileGenericSubPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var genericInputGrid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        genericInputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        genericInputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _fileGenericInputBox = new TextBox { PlaceholderText = "Enter parameters...", FontSize = 10, Background = CreateBrush(0x0D, 0x0D, 0x0D), Foreground = CreateBrush(0xFF, 0xFF, 0xFF), Margin = new Thickness(0, 0, 8, 0) };
        var genericRunBtn = new Button { Content = "Execute", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Height = 32, Padding = new Thickness(12, 0, 12, 0), Background = CreateBrush(0x00, 0xB4, 0xFF), Foreground = CreateBrush(0x09, 0x09, 0x09) };
        genericRunBtn.Click += async (s, e) => await OnFileGenericActionClickAsync();
        Grid.SetColumn(_fileGenericInputBox, 0);
        Grid.SetColumn(genericRunBtn, 1);
        genericInputGrid.Children.Add(_fileGenericInputBox);
        genericInputGrid.Children.Add(genericRunBtn);
        Grid.SetRow(genericInputGrid, 0);
        _fileGenericSubPanel.Children.Add(genericInputGrid);

        var genericOutputBorder = new Border { Background = CreateBrush(0x0D, 0x0D, 0x0D), BorderBrush = CreateBrush(0x22, 0x22, 0x22), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(10) };
        _fileGenericOutputText = new TextBlock { Text = "Ready.", FontSize = 11, FontFamily = new FontFamily("Consolas"), Foreground = CreateBrush(0xE0, 0xE0, 0xE0), TextWrapping = TextWrapping.Wrap };
        genericOutputBorder.Child = new ScrollViewer { Content = _fileGenericOutputText, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(genericOutputBorder, 1);
        _fileGenericSubPanel.Children.Add(genericOutputBorder);
        innerGrid.Children.Add(_fileGenericSubPanel);

        // Copy button
        var copyBtn = new Button { Content = "Copy Output", FontSize = 10, Height = 24, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
        copyBtn.Click += (s, e) => CopyToClipboard(_activeFileSubTab == "rename" ? _fileRenameOutputText.Text : _fileGenericOutputText.Text);
        Grid.SetRow(copyBtn, 1);
        innerGrid.Children.Add(copyBtn);

        contentBorder.Child = innerGrid;
        Grid.SetRow(contentBorder, 1);
        grid.Children.Add(contentBorder);
        return grid;
    }

    private void AddFileSubTabButton(StackPanel panel, string title, string subTab, bool isInitial)
    {
        var btn = new Button
        {
            Content = title,
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Height = 24,
            Padding = new Thickness(10, 0, 10, 0),
            CornerRadius = new CornerRadius(12),
            Background = isInitial ? CreateBrush(0x00, 0xB4, 0xFF) : CreateBrush(0x22, 0x22, 0x22),
            Foreground = isInitial ? CreateBrush(0x09, 0x09, 0x09) : CreateBrush(0xA0, 0xA0, 0xA0)
        };
        btn.Click += (s, e) => SelectFileSubTab(btn, subTab);
        _fileSubTabButtons.Add(btn);
        panel.Children.Add(btn);
    }

    private void SelectFileSubTab(Button activeBtn, string subTab)
    {
        _activeFileSubTab = subTab;
        var activeBrush = CreateBrush(0x00, 0xB4, 0xFF);
        var inactiveBrush = CreateBrush(0x22, 0x22, 0x22);
        var activeText = CreateBrush(0x09, 0x09, 0x09);
        var inactiveText = CreateBrush(0xA0, 0xA0, 0xA0);

        foreach (var b in _fileSubTabButtons)
        {
            b.Background = inactiveBrush;
            b.Foreground = inactiveText;
        }
        activeBtn.Background = activeBrush;
        activeBtn.Foreground = activeText;

        if (subTab == "rename")
        {
            _fileRenameSubPanel.Visibility = Visibility.Visible;
            _fileGenericSubPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            _fileRenameSubPanel.Visibility = Visibility.Collapsed;
            _fileGenericSubPanel.Visibility = Visibility.Visible;
            _fileGenericInputBox.PlaceholderText = subTab switch
            {
                "zip" => "Create: sourceDir|dest.zip  OR Extract: zipPath|extractDir",
                "size" => "Enter directory path to analyze...",
                "search" => "Enter: rootDir|pattern (e.g. C:\\Projects|*.cs)",
                "convert" => "Enter: sourcePath|type (CsvToJson, JsonToCsv, LineEndings, TextEncoding)",
                "hash" => "Enter: filePath|algorithm (SHA256, MD5, SHA1, SHA512)",
                _ => "Parameters..."
            };
        }
    }

    private BatchRenameRule BuildCurrentRenameRule()
    {
        string param = _fileRenameParamBox.Text.Trim();
        return _fileRenameRuleCombo.SelectedIndex switch
        {
            0 => new BatchRenameRule
            {
                Type = BatchRenameRuleType.ReplaceText,
                FindText = param.Split('|').FirstOrDefault() ?? "",
                ReplaceWithText = param.Split('|').ElementAtOrDefault(1) ?? ""
            },
            1 => new BatchRenameRule
            {
                Type = BatchRenameRuleType.PrefixSuffix,
                Prefix = param.Split('|').FirstOrDefault() ?? "",
                Suffix = param.Split('|').ElementAtOrDefault(1) ?? ""
            },
            2 => new BatchRenameRule
            {
                Type = BatchRenameRuleType.Numbering,
                StartIndex = 1,
                PaddingDigits = 3
            },
            3 => new BatchRenameRule
            {
                Type = BatchRenameRuleType.ChangeCase,
                CaseMode = TextCaseMode.Lowercase
            },
            4 => new BatchRenameRule
            {
                Type = BatchRenameRuleType.ChangeCase,
                CaseMode = TextCaseMode.Uppercase
            },
            5 => new BatchRenameRule
            {
                Type = BatchRenameRuleType.ChangeExtension,
                NewExtension = param.StartsWith('.') ? param : $".{param}"
            },
            _ => new BatchRenameRule()
        };
    }

    private async Task OnFileRenamePreviewClickAsync()
    {
        string dir = _fileRenameDirBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
        {
            _fileRenameOutputText.Text = "Please specify a valid existing directory.";
            return;
        }

        var files = Directory.GetFiles(dir);
        if (files.Length == 0)
        {
            _fileRenameOutputText.Text = "No files found in directory.";
            return;
        }

        var rule = BuildCurrentRenameRule();
        var previews = await _fileTools.PreviewBatchRenameAsync(files, rule);
        var lines = previews.Select(p => $"{(p.IsValid ? "OK" : "ERR")} : {p.OriginalFileName} -> {p.NewFileName} {(p.ErrorMessage != null ? $"({p.ErrorMessage})" : "")}");
        _fileRenameOutputText.Text = $"SIMULATION PREVIEW ({previews.Count} files):\n" + string.Join("\n", lines);
    }

    private async Task OnFileRenameExecuteClickAsync()
    {
        string dir = _fileRenameDirBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
        {
            _fileRenameOutputText.Text = "Please specify a valid existing directory.";
            return;
        }

        var files = Directory.GetFiles(dir);
        var rule = BuildCurrentRenameRule();
        var res = await _fileTools.ExecuteBatchRenameAsync(files, rule);

        _fileRenameOutputText.Text = $"BATCH RENAME COMPLETE\nTotal: {res.TotalRequested} | Renamed: {res.RenamedCount} | Failed: {res.FailedCount}\n" +
            string.Join("\n", res.Items.Select(i => $"{(i.IsValid ? "RENAMED" : "FAILED")} : {i.OriginalFileName} -> {i.NewFileName}"));
    }

    private async Task OnFileGenericActionClickAsync()
    {
        string input = _fileGenericInputBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            _fileGenericOutputText.Text = "Please enter parameters.";
            return;
        }

        _fileGenericOutputText.Text = "Executing...";

        try
        {
            switch (_activeFileSubTab)
            {
                case "size":
                    {
                        var res = await _fileTools.AnalyzeFolderSizeAsync(input);
                        double mb = res.TotalSizeBytes / (1024.0 * 1024.0);
                        var extSummary = string.Join("\n  ", res.ExtensionSizesBytes.OrderByDescending(kv => kv.Value).Take(10).Select(kv => $"{kv.Key}: {kv.Value / 1024.0:F1} KB"));
                        _fileGenericOutputText.Text = $"FOLDER SIZE ANALYSIS\nPath: {res.FolderPath}\nTotal Size: {mb:F2} MB ({res.TotalSizeBytes:N0} bytes)\nFiles: {res.FileCount:N0}\nSubdirectories: {res.SubdirectoryCount:N0}\nTop Extensions:\n  {extSummary}";
                        break;
                    }

                case "search":
                    {
                        string[] parts = input.Split('|');
                        string root = parts[0].Trim();
                        string pattern = parts.Length > 1 ? parts[1].Trim() : "*";
                        var hits = await _fileTools.SearchFilesAsync(root, pattern);
                        var lines = hits.Take(50).Select(h => $"{h.FileName} ({h.SizeBytes / 1024.0:F1} KB) - {h.FilePath}");
                        _fileGenericOutputText.Text = $"SEARCH RESULTS ({hits.Count} matches in {root}):\n" + string.Join("\n", lines);
                        break;
                    }

                case "hash":
                    {
                        string[] parts = input.Split('|');
                        string filePath = parts[0].Trim();
                        string algo = parts.Length > 1 ? parts[1].Trim() : "SHA256";
                        var hashRes = await _fileTools.ComputeFileHashAsync(filePath, algo);
                        _fileGenericOutputText.Text = $"FILE HASH REPORT\nFile     : {hashRes.FilePath}\nAlgorithm: {hashRes.Algorithm}\nHash     : {hashRes.HashHex}\nSize     : {hashRes.FileSizeBytes:N0} bytes\nElapsed  : {hashRes.Elapsed.TotalMilliseconds:F1} ms";
                        break;
                    }

                case "convert":
                    {
                        string[] parts = input.Split('|');
                        string src = parts[0].Trim();
                        string typeStr = parts.Length > 1 ? parts[1].Trim() : "CsvToJson";
                        var convType = Enum.TryParse<FileConversionType>(typeStr, true, out var ct) ? ct : FileConversionType.CsvToJson;

                        var res = await _fileTools.ConvertFileAsync(src, new FileConvertOptions { ConversionType = convType });
                        _fileGenericOutputText.Text = res.Success
                            ? $"FILE CONVERSION SUCCESS\nSource : {res.SourceFilePath}\nOutput : {res.OutputFilePath}\nSize   : {res.OriginalSizeBytes:N0} -> {res.ConvertedSizeBytes:N0} bytes"
                            : $"CONVERSION FAILED: {res.ErrorMessage}";
                        break;
                    }

                case "zip":
                    {
                        string[] parts = input.Split('|');
                        if (parts.Length < 2)
                        {
                            _fileGenericOutputText.Text = "Please format as: sourceDir|destZipPath  OR zipPath|extractDir";
                            return;
                        }

                        if (parts[1].EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            bool ok = await _fileTools.CreateZipArchiveAsync(new[] { parts[0].Trim() }, parts[1].Trim(), overwriteExisting: true);
                            _fileGenericOutputText.Text = ok ? $"ZIP created successfully at: {parts[1].Trim()}" : "ZIP creation failed.";
                        }
                        else
                        {
                            bool ok = await _fileTools.ExtractZipArchiveAsync(parts[0].Trim(), parts[1].Trim(), overwriteExisting: true);
                            _fileGenericOutputText.Text = ok ? $"ZIP extracted successfully to: {parts[1].Trim()}" : "ZIP extraction failed.";
                        }
                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            _fileGenericOutputText.Text = $"Error: {ex.Message}";
        }
    }

    #endregion

    #region Developer Utilities Workspace Implementation

    private Grid BuildDevUtilsWorkspace()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var chipsStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        AddDevChipButton(chipsStack, "JSON Formatter", "json", "JSON Formatter & Validator", true);
        AddDevChipButton(chipsStack, "Regex Tester", "regex", "Regex Tester (ReDoS Guard)", false);
        AddDevChipButton(chipsStack, "Base64", "base64", "Base64 Encoder/Decoder", false);
        AddDevChipButton(chipsStack, "URL Codec", "url", "URL Encoder/Decoder", false);
        AddDevChipButton(chipsStack, "UUID", "uuid", "UUID / GUID Generator", false);
        AddDevChipButton(chipsStack, "Hash", "hash", "Text Hash Calculator", false);
        AddDevChipButton(chipsStack, "Text", "text", "Text Formatter & Case Converter", false);
        AddDevChipButton(chipsStack, "Timestamp", "time", "Timestamp & Date Converter", false);
        AddDevChipButton(chipsStack, "Color", "color", "Color Spaces Converter", false);
        AddDevChipButton(chipsStack, "QR Code", "qr", "QR Code Generator", false);
        AddDevChipButton(chipsStack, "Units", "units", "Universal Unit Converter", false);

        var chipsScroll = new ScrollViewer { Content = chipsStack, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, HorizontalScrollMode = ScrollMode.Enabled, Margin = new Thickness(0, 0, 0, 10) };
        Grid.SetRow(chipsScroll, 0);
        grid.Children.Add(chipsScroll);

        var splitGrid = new Grid();
        splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        splitGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left Pane: Editor & Options
        var leftBorder = new Border { Background = CreateBrush(0x17, 0x17, 0x17), BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 0, 6, 0) };
        var leftGrid = new Grid();
        leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        _devToolTitleText = new TextBlock { Text = "JSON Formatter & Validator", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF), Margin = new Thickness(0, 0, 0, 8) };
        Grid.SetRow(_devToolTitleText, 0);
        leftGrid.Children.Add(_devToolTitleText);

        _devInputTextBox = new TextBox { PlaceholderText = "Paste JSON here...", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, FontSize = 11, FontFamily = new FontFamily("Consolas"), Background = CreateBrush(0x0D, 0x0D, 0x0D), BorderBrush = CreateBrush(0x22, 0x22, 0x22), Foreground = CreateBrush(0xFF, 0xFF, 0xFF) };
        Grid.SetRow(_devInputTextBox, 1);
        leftGrid.Children.Add(_devInputTextBox);

        var bottomActions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        _devOptionCombo = new ComboBox { SelectedIndex = 0, FontSize = 10, Width = 140 };
        _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Prettify (2 sp)" });
        _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Prettify (4 sp)" });
        _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Minify" });

        var runBtn = new Button { Content = "Run Operation", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Height = 28, Padding = new Thickness(10, 0, 10, 0), Background = CreateBrush(0x00, 0xB4, 0xFF), Foreground = CreateBrush(0x09, 0x09, 0x09) };
        runBtn.Click += (s, e) => OnDevRunActionClick();
        bottomActions.Children.Add(_devOptionCombo);
        bottomActions.Children.Add(runBtn);
        Grid.SetRow(bottomActions, 2);
        leftGrid.Children.Add(bottomActions);
        leftBorder.Child = leftGrid;
        Grid.SetColumn(leftBorder, 0);
        splitGrid.Children.Add(leftBorder);

        // Right Pane: Output
        var rightBorder = new Border { Background = CreateBrush(0x17, 0x17, 0x17), BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(6, 0, 0, 0) };
        var rightGrid = new Grid();
        rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        rightGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        rightGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        rightGrid.Children.Add(new TextBlock { Text = "Result Output", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF), Margin = new Thickness(0, 0, 0, 8) });

        var outputScrollBorder = new Border { Background = CreateBrush(0x0D, 0x0D, 0x0D), BorderBrush = CreateBrush(0x22, 0x22, 0x22), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(6), Padding = new Thickness(10) };
        _devOutputTextBox = new TextBlock { Text = "Output will appear here...", FontSize = 11, FontFamily = new FontFamily("Consolas"), Foreground = CreateBrush(0xE0, 0xE0, 0xE0), TextWrapping = TextWrapping.Wrap };
        outputScrollBorder.Child = new ScrollViewer { Content = _devOutputTextBox, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(outputScrollBorder, 1);
        rightGrid.Children.Add(outputScrollBorder);

        var rightFooter = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Margin = new Thickness(0, 8, 0, 0) };
        _devStatusMessageText = new TextBlock { Text = "Ready", FontSize = 10, Foreground = CreateBrush(0xA0, 0xA0, 0xA0), VerticalAlignment = VerticalAlignment.Center };
        var copyDevResultBtn = new Button { Content = "Copy Result", FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Height = 26, Padding = new Thickness(10, 0, 10, 0), Background = CreateBrush(0x22, 0x22, 0x22), Foreground = CreateBrush(0xFF, 0xFF, 0xFF) };
        copyDevResultBtn.Click += (s, e) => CopyToClipboard(_devOutputTextBox.Text);
        rightFooter.Children.Add(_devStatusMessageText);
        rightFooter.Children.Add(copyDevResultBtn);
        Grid.SetRow(rightFooter, 2);
        rightGrid.Children.Add(rightFooter);

        rightBorder.Child = rightGrid;
        Grid.SetColumn(rightBorder, 1);
        splitGrid.Children.Add(rightBorder);

        Grid.SetRow(splitGrid, 1);
        grid.Children.Add(splitGrid);
        return grid;
    }

    private void AddDevChipButton(StackPanel panel, string title, string key, string toolName, bool isInitial)
    {
        var btn = new Button
        {
            Content = title,
            FontSize = 10,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Height = 24,
            Padding = new Thickness(8, 0, 8, 0),
            CornerRadius = new CornerRadius(12),
            Background = isInitial ? CreateBrush(0x00, 0xB4, 0xFF) : CreateBrush(0x22, 0x22, 0x22),
            Foreground = isInitial ? CreateBrush(0x09, 0x09, 0x09) : CreateBrush(0xA0, 0xA0, 0xA0)
        };
        btn.Click += (s, e) => SelectDevTool(btn, key, toolName);
        _devToolChipButtons.Add(btn);
        panel.Children.Add(btn);
    }

    private void SelectDevTool(Button activeBtn, string toolKey, string toolName)
    {
        _activeDevTool = toolKey;
        _devToolTitleText.Text = toolName;

        var activeBrush = CreateBrush(0x00, 0xB4, 0xFF);
        var inactiveBrush = CreateBrush(0x22, 0x22, 0x22);
        var activeText = CreateBrush(0x09, 0x09, 0x09);
        var inactiveText = CreateBrush(0xA0, 0xA0, 0xA0);

        foreach (var b in _devToolChipButtons)
        {
            b.Background = inactiveBrush;
            b.Foreground = inactiveText;
        }
        activeBtn.Background = activeBrush;
        activeBtn.Foreground = activeText;

        _devOptionCombo.Items.Clear();
        switch (toolKey)
        {
            case "json":
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Prettify (2 sp)" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Prettify (4 sp)" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Minify" });
                _devInputTextBox.PlaceholderText = "Paste JSON here...";
                break;
            case "regex":
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Find Matches" });
                _devInputTextBox.PlaceholderText = "Format: pattern|targetText";
                break;
            case "base64":
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Encode" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Encode (URL-Safe)" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Decode" });
                _devInputTextBox.PlaceholderText = "Enter text to encode or Base64 string to decode...";
                break;
            case "url":
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Encode" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Decode" });
                _devInputTextBox.PlaceholderText = "Enter URL or text...";
                break;
            case "uuid":
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Standard (D)" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Digits (N)" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Braces (B)" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Batch (5x)" });
                _devInputTextBox.PlaceholderText = "No input needed. Press Run Operation.";
                break;
            case "hash":
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "SHA256" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "MD5" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "SHA1" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "SHA512" });
                _devInputTextBox.PlaceholderText = "Enter text to hash...";
                break;
            case "text":
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Uppercase" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Lowercase" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Title Case" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Sort Lines" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Deduplicate" });
                _devInputTextBox.PlaceholderText = "Enter text to transform...";
                break;
            case "time":
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Current Time" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Parse Date/Epoch" });
                _devInputTextBox.PlaceholderText = "Enter epoch seconds/millis or ISO date...";
                break;
            case "color":
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Convert Format" });
                _devInputTextBox.PlaceholderText = "Enter color: #00B4FF, rgb(0, 180, 255), or hsl(198, 100%, 50%)...";
                break;
            case "qr":
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "SVG Vector" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "ASCII Art" });
                _devInputTextBox.PlaceholderText = "Enter text or URL to generate QR code...";
                break;
            case "units":
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Bytes to MB/GB" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "KM to Miles" });
                _devOptionCombo.Items.Add(new ComboBoxItem { Content = "Celsius to Fahrenheit" });
                _devInputTextBox.PlaceholderText = "Enter numeric value...";
                break;
        }

        if (_devOptionCombo.Items.Count > 0)
        {
            _devOptionCombo.SelectedIndex = 0;
        }
    }

    private void OnDevRunActionClick()
    {
        string input = _devInputTextBox.Text.Trim();
        int opt = _devOptionCombo.SelectedIndex;

        try
        {
            switch (_activeDevTool)
            {
                case "json":
                    {
                        bool minify = opt == 2;
                        int indent = opt == 1 ? 4 : 2;
                        var res = _devUtils.FormatJson(input, !minify, indent);
                        _devOutputTextBox.Text = res.IsValid ? res.FormattedJson : $"JSON ERROR: {res.ErrorMessage}";
                        _devStatusMessageText.Text = res.IsValid ? "Valid JSON" : "Invalid JSON";
                        break;
                    }
                case "regex":
                    {
                        string[] parts = input.Split('|');
                        string pat = parts[0].Trim();
                        string target = parts.Length > 1 ? parts[1].Trim() : "";
                        var res = _devUtils.TestRegex(pat, target);
                        _devOutputTextBox.Text = res.Success
                            ? $"REGEX RESULT ({res.MatchCount} match(es)):\n" + string.Join("\n", res.Matches.Select((m, i) => $"[{i + 1}] Index {m.Index}: {m.Value}"))
                            : $"REGEX ERROR: {res.ErrorMessage}";
                        break;
                    }
                case "base64":
                    {
                        if (opt == 2) _devOutputTextBox.Text = _devUtils.DecodeBase64(input);
                        else _devOutputTextBox.Text = _devUtils.EncodeBase64(input, urlSafe: opt == 1);
                        _devStatusMessageText.Text = "Completed";
                        break;
                    }
                case "url":
                    {
                        _devOutputTextBox.Text = opt == 1 ? _devUtils.DecodeUrl(input) : _devUtils.EncodeUrl(input);
                        _devStatusMessageText.Text = "Completed";
                        break;
                    }
                case "uuid":
                    {
                        string fmt = opt switch { 1 => "N", 2 => "B", _ => "D" };
                        int count = opt == 3 ? 5 : 1;
                        var list = _devUtils.GenerateUuids(count, fmt);
                        _devOutputTextBox.Text = string.Join("\n", list);
                        _devStatusMessageText.Text = $"Generated {list.Count} UUID(s)";
                        break;
                    }
                case "hash":
                    {
                        string algo = opt switch { 1 => "MD5", 2 => "SHA1", 3 => "SHA512", _ => "SHA256" };
                        _devOutputTextBox.Text = $"{algo} : {_devUtils.ComputeHash(input, algo)}";
                        _devStatusMessageText.Text = "Hash computed";
                        break;
                    }
                case "text":
                    {
                        var mode = opt switch
                        {
                            1 => TextTransformMode.Lowercase,
                            2 => TextTransformMode.TitleCase,
                            3 => TextTransformMode.SortLinesAscending,
                            4 => TextTransformMode.DeduplicateLines,
                            _ => TextTransformMode.Uppercase
                        };
                        _devOutputTextBox.Text = _devUtils.TransformText(input, mode);
                        var stats = _devUtils.AnalyzeText(input);
                        _devStatusMessageText.Text = $"{stats.WordCount} words | {stats.CharacterCount} chars | {stats.LineCount} lines";
                        break;
                    }
                case "time":
                    {
                        if (opt == 0 || string.IsNullOrWhiteSpace(input))
                        {
                            var now = DateTimeOffset.UtcNow;
                            _devOutputTextBox.Text = $"CURRENT TIME\nEpoch Sec  : {now.ToUnixTimeSeconds()}\nEpoch Ms   : {now.ToUnixTimeMilliseconds()}\nUTC ISO    : {now:O}\nLocal      : {now.ToLocalTime():F}";
                        }
                        else
                        {
                            var res = long.TryParse(input, out long ep)
                                ? _devUtils.ConvertFromEpoch(ep, input.Length > 11)
                                : _devUtils.ParseDateToTimestamp(input);

                            _devOutputTextBox.Text = $"TIMESTAMP CONVERSION\nEpoch Sec : {res.EpochSeconds}\nEpoch Ms  : {res.EpochMilliseconds}\nUTC ISO   : {res.UtcIso8601}\nLocal     : {res.LocalDateTimeFormatted}";
                        }
                        break;
                    }
                case "color":
                    {
                        var res = _devUtils.ConvertColor(input);
                        _devOutputTextBox.Text = res.IsValid
                            ? $"COLOR CONVERSION\nHEX : {res.Hex}\nRGB : rgb({res.Red}, {res.Green}, {res.Blue})\nHSL : hsl({res.Hue:F0}, {res.Saturation * 100:F0}%, {res.Lightness * 100:F0}%)\nAlpha: {res.Alpha} / 255"
                            : "Invalid color format. Please enter #RRGGBB, rgb(r, g, b), or hsl(h, s, l).";
                        break;
                    }
                case "qr":
                    {
                        var qrRes = _devUtils.GenerateQr(input);
                        _devOutputTextBox.Text = opt == 1 ? qrRes.AsciiPreview : qrRes.SvgContent;
                        _devStatusMessageText.Text = "QR Code Rendered";
                        break;
                    }
                case "units":
                    {
                        double val = double.TryParse(input, out double v) ? v : 1.0;
                        if (opt == 0)
                        {
                            var mb = _devUtils.ConvertUnit(val, "Storage", "bytes", "megabytes");
                            var gb = _devUtils.ConvertUnit(val, "Storage", "bytes", "gigabytes");
                            _devOutputTextBox.Text = $"{val:N0} Bytes = {mb.OutputValue:F2} MB = {gb.OutputValue:F4} GB";
                        }
                        else if (opt == 1)
                        {
                            var mi = _devUtils.ConvertUnit(val, "Length", "kilometers", "miles");
                            _devOutputTextBox.Text = $"{val} Kilometers = {mi.OutputValue:F3} Miles";
                        }
                        else
                        {
                            var f = _devUtils.ConvertUnit(val, "Temperature", "celsius", "fahrenheit");
                            _devOutputTextBox.Text = $"{val}°C = {f.OutputValue:F1}°F";
                        }
                        break;
                    }
            }
        }
        catch (Exception ex)
        {
            _devOutputTextBox.Text = $"Error: {ex.Message}";
        }
    }

    #endregion

    #region Clipboard Workspace Implementation

    private Grid BuildClipboardWorkspace()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Filter Bar
        var filterBorder = new Border { Background = CreateBrush(0x17, 0x17, 0x17), BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 10) };
        var filterGrid = new Grid();
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        filterGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _clipSearchBox = new TextBox { PlaceholderText = "Search clipboard history...", FontSize = 11, Background = CreateBrush(0x0D, 0x0D, 0x0D), BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A), Foreground = CreateBrush(0xFF, 0xFF, 0xFF), Margin = new Thickness(0, 0, 8, 0) };
        _clipSearchBox.TextChanged += (s, e) => RefreshClipboardList(_clipSearchBox.Text.Trim());
        Grid.SetColumn(_clipSearchBox, 0);
        filterGrid.Children.Add(_clipSearchBox);

        _clipCategoryFilterCombo = new ComboBox { SelectedIndex = 0, FontSize = 10, Margin = new Thickness(0, 0, 8, 0) };
        _clipCategoryFilterCombo.Items.Add(new ComboBoxItem { Content = "All Types" });
        _clipCategoryFilterCombo.Items.Add(new ComboBoxItem { Content = "Text" });
        _clipCategoryFilterCombo.Items.Add(new ComboBoxItem { Content = "URL" });
        _clipCategoryFilterCombo.Items.Add(new ComboBoxItem { Content = "Code" });
        _clipCategoryFilterCombo.Items.Add(new ComboBoxItem { Content = "JSON" });
        _clipCategoryFilterCombo.Items.Add(new ComboBoxItem { Content = "Email" });
        _clipCategoryFilterCombo.Items.Add(new ComboBoxItem { Content = "Number" });
        _clipCategoryFilterCombo.SelectionChanged += (s, e) =>
        {
            ClipboardContentType? ct = _clipCategoryFilterCombo.SelectedIndex switch
            {
                1 => ClipboardContentType.Text,
                2 => ClipboardContentType.Url,
                3 => ClipboardContentType.Code,
                4 => ClipboardContentType.Json,
                5 => ClipboardContentType.Email,
                6 => ClipboardContentType.Number,
                _ => null
            };
            RefreshClipboardList(null, ct);
        };
        Grid.SetColumn(_clipCategoryFilterCombo, 1);
        filterGrid.Children.Add(_clipCategoryFilterCombo);

        var clipBtnStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        var clearBtn = new Button { Content = "Clear History", FontSize = 10, Height = 30, Background = CreateBrush(0x22, 0x22, 0x22), Foreground = CreateBrush(0xFF, 0x6B, 0x6B) };
        clearBtn.Click += (s, e) =>
        {
            _clipboard.ClearHistory();
            RefreshClipboardList();
        };
        clipBtnStack.Children.Add(clearBtn);
        Grid.SetColumn(clipBtnStack, 2);
        filterGrid.Children.Add(clipBtnStack);
        filterBorder.Child = filterGrid;
        Grid.SetRow(filterBorder, 0);
        grid.Children.Add(filterBorder);

        // List
        var listBorder = new Border { Background = CreateBrush(0x17, 0x17, 0x17), BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12) };
        _clipSnippetsListPanel = new StackPanel { Spacing = 6 };
        listBorder.Child = new ScrollViewer { Content = _clipSnippetsListPanel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        Grid.SetRow(listBorder, 1);
        grid.Children.Add(listBorder);

        // Privacy Gate Footer
        var privText = new TextBlock { Text = "Privacy Gate: Monitoring is local-only. Sensitive patterns (JWT, Credit Cards, Keys) are automatically masked and protected.", FontSize = 9, Foreground = CreateBrush(0x66, 0x66, 0x66), Margin = new Thickness(0, 8, 0, 0) };
        Grid.SetRow(privText, 2);
        grid.Children.Add(privText);

        return grid;
    }

    private void RefreshClipboardList(string? query = null, ClipboardContentType? category = null)
    {
        _clipSnippetsListPanel.Children.Clear();
        var items = _clipboard.Search(query, category);

        if (items.Count == 0)
        {
            _clipSnippetsListPanel.Children.Add(new TextBlock
            {
                Text = "No clipboard snippets found.",
                FontSize = 11,
                Foreground = CreateBrush(0x88, 0x88, 0x88),
                Margin = new Thickness(0, 10, 0, 0)
            });
            return;
        }

        foreach (var item in items)
        {
            var card = new Border
            {
                Background = CreateBrush(0x14, 0x14, 0x14),
                BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var rowGrid = new Grid();
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var contentStack = new StackPanel { Spacing = 4 };
            var metaStack = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };

            metaStack.Children.Add(new TextBlock
            {
                Text = item.ContentType.ToString().ToUpperInvariant(),
                FontSize = 9,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Foreground = CreateBrush(0x00, 0xB4, 0xFF)
            });
            metaStack.Children.Add(new TextBlock
            {
                Text = $"• {item.Timestamp.ToLocalTime():HH:mm:ss}",
                FontSize = 9,
                Foreground = CreateBrush(0x88, 0x88, 0x88)
            });

            if (item.IsPinned)
            {
                metaStack.Children.Add(new TextBlock
                {
                    Text = "PINNED",
                    FontSize = 9,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = CreateBrush(0xFF, 0xD7, 0x00)
                });
            }

            contentStack.Children.Add(metaStack);
            contentStack.Children.Add(new TextBlock
            {
                Text = item.Text,
                FontSize = 10,
                FontFamily = new FontFamily("Consolas"),
                Foreground = CreateBrush(0xE6, 0xE6, 0xE6),
                MaxLines = 3,
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.Wrap
            });
            Grid.SetColumn(contentStack, 0);
            rowGrid.Children.Add(contentStack);

            var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, VerticalAlignment = VerticalAlignment.Center };
            var copyBtn = new Button { Content = "Copy", FontSize = 9, Height = 24, Padding = new Thickness(8, 0, 8, 0) };
            copyBtn.Click += async (s, e) =>
            {
                await _clipboard.CopyToClipboardAsync(item.Id);
                _globalStatusText.Text = "Snippet copied to clipboard.";
            };

            var pinBtn = new Button { Content = item.IsPinned ? "Unpin" : "Pin", FontSize = 9, Height = 24, Padding = new Thickness(8, 0, 8, 0) };
            pinBtn.Click += (s, e) =>
            {
                _clipboard.TogglePin(item.Id);
                RefreshClipboardList();
            };

            var delBtn = new Button { Content = "✕", FontSize = 9, Height = 24, Padding = new Thickness(6, 0, 6, 0), Foreground = CreateBrush(0xFF, 0x6B, 0x6B) };
            delBtn.Click += (s, e) =>
            {
                _clipboard.DeleteSnippet(item.Id);
                RefreshClipboardList();
            };

            actions.Children.Add(copyBtn);
            actions.Children.Add(pinBtn);
            actions.Children.Add(delBtn);
            Grid.SetColumn(actions, 1);
            rowGrid.Children.Add(actions);

            card.Child = rowGrid;
            _clipSnippetsListPanel.Children.Add(card);
        }
    }

    #endregion

    #region Workflow Workspace Implementation

    private StackPanel _wfListPanel = null!;
    private TextBlock _wfTitleText = null!;
    private TextBlock _wfDescText = null!;
    private StackPanel _wfStepsPanel = null!;
    private Button _wfRunBtn = null!;
    private TextBlock _wfResultText = null!;
    private WorkflowDefinition? _selectedWorkflow;

    private Grid BuildWorkflowWorkspace()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left Pane: Workflows List
        var leftBorder = new Border
        {
            Background = CreateBrush(0x14, 0x14, 0x14),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 10, 0)
        };
        var leftStack = new StackPanel { Spacing = 10 };
        leftStack.Children.Add(new TextBlock { Text = "Automated Workflows", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF) });

        var scroller = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Height = 480 };
        _wfListPanel = new StackPanel { Spacing = 6 };
        scroller.Content = _wfListPanel;
        leftStack.Children.Add(scroller);

        leftBorder.Child = leftStack;
        Grid.SetColumn(leftBorder, 0);
        grid.Children.Add(leftBorder);

        // Right Pane: Details & Runner
        var rightBorder = new Border
        {
            Background = CreateBrush(0x17, 0x17, 0x17),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16)
        };
        var rightStack = new StackPanel { Spacing = 14 };

        _wfTitleText = new TextBlock { Text = "Select a Workflow", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF) };
        _wfDescText = new TextBlock { Text = "Automated multi-step workflow details", FontSize = 11, Foreground = CreateBrush(0x88, 0x88, 0x88) };
        rightStack.Children.Add(_wfTitleText);
        rightStack.Children.Add(_wfDescText);

        rightStack.Children.Add(new TextBlock { Text = "Workflow Steps:", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0x00, 0xB4, 0xFF) });
        var stepsScroller = new ScrollViewer { MaxHeight = 160 };
        _wfStepsPanel = new StackPanel { Spacing = 4 };
        stepsScroller.Content = _wfStepsPanel;
        rightStack.Children.Add(stepsScroller);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        _wfRunBtn = new Button
        {
            Content = "Execute Workflow",
            Background = CreateBrush(0x00, 0xB4, 0xFF),
            Foreground = CreateBrush(0x09, 0x09, 0x09),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Height = 32,
            Padding = new Thickness(16, 0, 16, 0),
            CornerRadius = new CornerRadius(6)
        };
        _wfRunBtn.Click += async (s, e) =>
        {
            if (_selectedWorkflow == null || _workflowEngine == null) return;
            _wfRunBtn.IsEnabled = false;
            _globalStatusText.Text = $"Executing workflow '{_selectedWorkflow.Name}'...";

            var res = await _workflowEngine.ExecuteWorkflowAsync(_selectedWorkflow.Id);
            _wfRunBtn.IsEnabled = true;

            if (res.Success)
            {
                _wfResultText.Text = $"Workflow executed successfully in {res.TotalDuration.TotalMilliseconds:F0} ms.\n" +
                    string.Join("\n", res.StepResults.Select((st, idx) => $"[Step {idx + 1}] {st.StepName}: OK -> {st.OutputText}"));
                _globalStatusText.Text = $"Workflow '{_selectedWorkflow.Name}' completed successfully.";
            }
            else
            {
                _wfResultText.Text = $"Workflow failed: {res.ErrorMessage}\n" +
                    string.Join("\n", res.StepResults.Select((st, idx) => $"[Step {idx + 1}] {st.StepName}: {(st.Success ? "OK" : "FAILED - " + st.ErrorMessage)}"));
                _globalStatusText.Text = $"Workflow execution failed.";
            }
        };
        btnRow.Children.Add(_wfRunBtn);

        var copyResBtn = new Button { Content = "Copy Result", Height = 32, Padding = new Thickness(12, 0, 12, 0), CornerRadius = new CornerRadius(6) };
        copyResBtn.Click += (s, e) => CopyToClipboard(_wfResultText.Text);
        btnRow.Children.Add(copyResBtn);
        rightStack.Children.Add(btnRow);

        var resBorder = new Border
        {
            Background = CreateBrush(0x0D, 0x0D, 0x0D),
            BorderBrush = CreateBrush(0x22, 0x22, 0x22),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Height = 180
        };
        var resScroller = new ScrollViewer();
        _wfResultText = new TextBlock
        {
            Text = "Execution output will appear here...",
            FontSize = 10,
            FontFamily = new FontFamily("Consolas"),
            Foreground = CreateBrush(0xA0, 0xA0, 0xA0),
            TextWrapping = TextWrapping.Wrap
        };
        resScroller.Content = _wfResultText;
        resBorder.Child = resScroller;
        rightStack.Children.Add(resBorder);

        rightBorder.Child = rightStack;
        Grid.SetColumn(rightBorder, 1);
        grid.Children.Add(rightBorder);

        return grid;
    }

    private void RefreshWorkflowsList()
    {
        if (_wfListPanel == null || _workflowEngine == null) return;
        _wfListPanel.Children.Clear();

        var workflows = _workflowEngine.Workflows;
        foreach (var wf in workflows)
        {
            var card = new Border
            {
                Background = wf.Id == _selectedWorkflow?.Id ? CreateBrush(0x22, 0x22, 0x22) : CreateBrush(0x19, 0x19, 0x19),
                BorderBrush = CreateBrush(0x2C, 0x2C, 0x2C),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8)
            };
            var stack = new StackPanel { Spacing = 3 };
            stack.Children.Add(new TextBlock { Text = wf.Name, FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF) });
            stack.Children.Add(new TextBlock { Text = $"{wf.Steps.Count} step(s)", FontSize = 9, Foreground = CreateBrush(0x88, 0x88, 0x88) });
            card.Child = stack;

            card.PointerPressed += (s, e) =>
            {
                _selectedWorkflow = wf;
                _wfTitleText.Text = wf.Name;
                _wfDescText.Text = wf.Description;
                _wfStepsPanel.Children.Clear();
                for (int i = 0; i < wf.Steps.Count; i++)
                {
                    var st = wf.Steps[i];
                    _wfStepsPanel.Children.Add(new TextBlock
                    {
                        Text = $"{i + 1}. {st.Name} (Tool: {st.ToolId})",
                        FontSize = 10,
                        Foreground = CreateBrush(0xCC, 0xCC, 0xCC)
                    });
                }
                RefreshWorkflowsList();
            };

            _wfListPanel.Children.Add(card);
        }

        if (_selectedWorkflow == null && workflows.Count > 0)
        {
            _selectedWorkflow = workflows[0];
            _wfTitleText.Text = _selectedWorkflow.Name;
            _wfDescText.Text = _selectedWorkflow.Description;
            _wfStepsPanel.Children.Clear();
            for (int i = 0; i < _selectedWorkflow.Steps.Count; i++)
            {
                var st = _selectedWorkflow.Steps[i];
                _wfStepsPanel.Children.Add(new TextBlock
                {
                    Text = $"{i + 1}. {st.Name} (Tool: {st.ToolId})",
                    FontSize = 10,
                    Foreground = CreateBrush(0xCC, 0xCC, 0xCC)
                });
            }
        }
    }

    #endregion

    #region Plugin Workspace Implementation

    private StackPanel _pluginListPanel = null!;
    private TextBlock _pluginTitleText = null!;
    private TextBlock _pluginMetaText = null!;
    private Border _pluginStatusPill = null!;
    private TextBlock _pluginStatusPillText = null!;
    private Button _pluginToggleBtn = null!;
    private TextBlock _pluginDeclaredText = null!;
    private IPlugin? _selectedPlugin;

    private Grid BuildPluginWorkspace()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        // Left Pane: Plugin List
        var leftBorder = new Border
        {
            Background = CreateBrush(0x14, 0x14, 0x14),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 10, 0)
        };
        var leftStack = new StackPanel { Spacing = 10 };
        leftStack.Children.Add(new TextBlock { Text = "Installed Extensions", FontSize = 12, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF) });

        var scroller = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Height = 480 };
        _pluginListPanel = new StackPanel { Spacing = 6 };
        scroller.Content = _pluginListPanel;
        leftStack.Children.Add(scroller);

        leftBorder.Child = leftStack;
        Grid.SetColumn(leftBorder, 0);
        grid.Children.Add(leftBorder);

        // Right Pane: Plugin Inspector
        var rightBorder = new Border
        {
            Background = CreateBrush(0x17, 0x17, 0x17),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(16)
        };
        var rightStack = new StackPanel { Spacing = 14 };

        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, VerticalAlignment = VerticalAlignment.Center };
        _pluginTitleText = new TextBlock { Text = "Select a Plugin", FontSize = 16, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF) };
        titleRow.Children.Add(_pluginTitleText);

        _pluginStatusPill = new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 2, 8, 2),
            Background = CreateBrush(0x2A, 0x2A, 0x2A)
        };
        _pluginStatusPillText = new TextBlock { Text = "Unknown", FontSize = 9, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0xCC, 0xCC, 0xCC) };
        _pluginStatusPill.Child = _pluginStatusPillText;
        titleRow.Children.Add(_pluginStatusPill);
        rightStack.Children.Add(titleRow);

        _pluginMetaText = new TextBlock { Text = "Plugin metadata", FontSize = 11, Foreground = CreateBrush(0x88, 0x88, 0x88) };
        rightStack.Children.Add(_pluginMetaText);

        _pluginToggleBtn = new Button
        {
            Content = "Toggle Plugin",
            Background = CreateBrush(0x00, 0xB4, 0xFF),
            Foreground = CreateBrush(0x09, 0x09, 0x09),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Height = 32,
            Padding = new Thickness(16, 0, 16, 0),
            CornerRadius = new CornerRadius(6)
        };
        _pluginToggleBtn.Click += async (s, e) =>
        {
            if (_selectedPlugin == null || _pluginManager == null) return;
            string id = _selectedPlugin.Manifest.Id;
            bool isEnabled = _pluginManager.IsPluginEnabled(id);

            if (isEnabled)
            {
                await _pluginManager.DisablePluginAsync(id);
                _globalStatusText.Text = $"Plugin '{_selectedPlugin.Manifest.Name}' disabled.";
            }
            else
            {
                await _pluginManager.EnablePluginAsync(id);
                _globalStatusText.Text = $"Plugin '{_selectedPlugin.Manifest.Name}' activated.";
            }

            UpdateSelectedPluginView();
            RefreshPluginsList();
        };
        rightStack.Children.Add(_pluginToggleBtn);

        rightStack.Children.Add(new TextBlock { Text = "Exposed Tools & Commands:", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0x00, 0xB4, 0xFF) });
        var declBorder = new Border
        {
            Background = CreateBrush(0x0D, 0x0D, 0x0D),
            BorderBrush = CreateBrush(0x22, 0x22, 0x22),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Height = 220
        };
        var declScroller = new ScrollViewer();
        _pluginDeclaredText = new TextBlock
        {
            Text = "No plugin selected.",
            FontSize = 10,
            FontFamily = new FontFamily("Consolas"),
            Foreground = CreateBrush(0xA0, 0xA0, 0xA0),
            TextWrapping = TextWrapping.Wrap
        };
        declScroller.Content = _pluginDeclaredText;
        declBorder.Child = declScroller;
        rightStack.Children.Add(declBorder);

        rightBorder.Child = rightStack;
        Grid.SetColumn(rightBorder, 1);
        grid.Children.Add(rightBorder);

        return grid;
    }

    private void RefreshPluginsList()
    {
        if (_pluginListPanel == null || _pluginManager == null) return;
        _pluginListPanel.Children.Clear();

        var plugins = _pluginManager.Plugins;
        foreach (var p in plugins)
        {
            bool isEnabled = _pluginManager.IsPluginEnabled(p.Manifest.Id);
            var card = new Border
            {
                Background = p.Manifest.Id == _selectedPlugin?.Manifest.Id ? CreateBrush(0x22, 0x22, 0x22) : CreateBrush(0x19, 0x19, 0x19),
                BorderBrush = CreateBrush(0x2C, 0x2C, 0x2C),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 8, 10, 8)
            };
            var stack = new StackPanel { Spacing = 3 };
            var hRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            hRow.Children.Add(new TextBlock { Text = p.Manifest.Name, FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF) });
            hRow.Children.Add(new TextBlock { Text = $"v{p.Manifest.Version}", FontSize = 9, Foreground = CreateBrush(0x88, 0x88, 0x88), VerticalAlignment = VerticalAlignment.Center });
            stack.Children.Add(hRow);

            var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            statusRow.Children.Add(new TextBlock
            {
                Text = isEnabled ? "Active" : "Disabled",
                FontSize = 9,
                Foreground = isEnabled ? CreateBrush(0x10, 0x7C, 0x41) : CreateBrush(0x88, 0x88, 0x88)
            });
            stack.Children.Add(statusRow);
            card.Child = stack;

            card.PointerPressed += (s, e) =>
            {
                _selectedPlugin = p;
                UpdateSelectedPluginView();
                RefreshPluginsList();
            };

            _pluginListPanel.Children.Add(card);
        }

        if (_selectedPlugin == null && plugins.Count > 0)
        {
            _selectedPlugin = plugins[0];
            UpdateSelectedPluginView();
        }
    }

    private void UpdateSelectedPluginView()
    {
        if (_selectedPlugin == null || _pluginManager == null) return;
        var m = _selectedPlugin.Manifest;
        bool isEnabled = _pluginManager.IsPluginEnabled(m.Id);

        _pluginTitleText.Text = m.Name;
        _pluginMetaText.Text = $"ID: {m.Id} | Author: {m.Author} | MinAppVersion: {m.MinAppVersion}\nPermissions: {m.Permissions}\n{m.Description}";

        _pluginStatusPill.Background = isEnabled ? CreateBrush(0x10, 0x7C, 0x41) : CreateBrush(0x3A, 0x3A, 0x3A);
        _pluginStatusPillText.Text = isEnabled ? "Active" : "Disabled";

        _pluginToggleBtn.Content = isEnabled ? "Disable Plugin" : "Enable Plugin";
        _pluginToggleBtn.Background = isEnabled ? CreateBrush(0x3A, 0x3A, 0x3A) : CreateBrush(0x00, 0xB4, 0xFF);
        _pluginToggleBtn.Foreground = isEnabled ? CreateBrush(0xFF, 0xFF, 0xFF) : CreateBrush(0x09, 0x09, 0x09);

        var tools = _selectedPlugin.GetTools().ToList();
        var cmds = _selectedPlugin.GetCommands().ToList();
        _pluginDeclaredText.Text = $"Declared Tools ({tools.Count}):\n" +
            string.Join("\n", tools.Select(t => $"  • {t.DisplayName} ({t.Id}) - {t.Description}")) +
            $"\n\nDeclared Palette Commands ({cmds.Count}):\n" +
            string.Join("\n", cmds.Select(c => $"  • {c.Title} ({c.Id})"));
    }

    #endregion

    #region Diagnostics Workspace Implementation

    private TextBlock _diagStatusBannerText = null!;
    private TextBlock _diagResourceText = null!;
    private StackPanel _diagHealthChecksPanel = null!;
    private TextBlock _diagReportPreviewText = null!;

    private Grid BuildDiagnosticsWorkspace()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Header Status Banner
        var bannerBorder = new Border
        {
            Background = CreateBrush(0x14, 0x14, 0x14),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var bannerStack = new StackPanel { Spacing = 6 };
        _diagStatusBannerText = new TextBlock { Text = "System Status: Healthy", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = CreateBrush(0x10, 0x7C, 0x41) };
        _diagResourceText = new TextBlock { Text = "Memory: -- MB | Threads: -- | Uptime: --", FontSize = 11, Foreground = CreateBrush(0xA0, 0xA0, 0xA0) };
        bannerStack.Children.Add(_diagStatusBannerText);
        bannerStack.Children.Add(_diagResourceText);
        bannerBorder.Child = bannerStack;
        Grid.SetRow(bannerBorder, 0);
        grid.Children.Add(bannerBorder);

        // Actions & Health Checks Row
        var checksBorder = new Border
        {
            Background = CreateBrush(0x17, 0x17, 0x17),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var checksStack = new StackPanel { Spacing = 8 };
        var actRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        var refreshBtn = new Button { Content = "Refresh Health", Height = 28, Padding = new Thickness(10, 0, 10, 0), CornerRadius = new CornerRadius(6) };
        refreshBtn.Click += (s, e) => RefreshDiagnosticsView();
        actRow.Children.Add(refreshBtn);

        var exportSanitizedBtn = new Button
        {
            Content = "Export Sanitized Report (JSON)",
            Background = CreateBrush(0x00, 0xB4, 0xFF),
            Foreground = CreateBrush(0x09, 0x09, 0x09),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Height = 28,
            Padding = new Thickness(12, 0, 12, 0),
            CornerRadius = new CornerRadius(6)
        };
        exportSanitizedBtn.Click += async (s, e) =>
        {
            if (_diagnosticsService == null) return;
            var report = await _diagnosticsService.GenerateReportAsync();
            string json = _diagnosticsService.ExportSanitizedReportJson(report);
            CopyToClipboard(json);
            _diagReportPreviewText.Text = json;
            _globalStatusText.Text = "Sanitized diagnostic report exported & copied to clipboard (Secrets Redacted).";
        };
        actRow.Children.Add(exportSanitizedBtn);
        checksStack.Children.Add(actRow);

        _diagHealthChecksPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        checksStack.Children.Add(_diagHealthChecksPanel);
        checksBorder.Child = checksStack;
        Grid.SetRow(checksBorder, 1);
        grid.Children.Add(checksBorder);

        // Report Preview Box
        var reportBorder = new Border
        {
            Background = CreateBrush(0x0D, 0x0D, 0x0D),
            BorderBrush = CreateBrush(0x22, 0x22, 0x22),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12)
        };
        var reportScroller = new ScrollViewer();
        _diagReportPreviewText = new TextBlock
        {
            Text = "Click 'Refresh Health' or 'Export Sanitized Report' to inspect system diagnostics.",
            FontSize = 10,
            FontFamily = new FontFamily("Consolas"),
            Foreground = CreateBrush(0xA0, 0xA0, 0xA0),
            TextWrapping = TextWrapping.Wrap
        };
        reportScroller.Content = _diagReportPreviewText;
        reportBorder.Child = reportScroller;
        Grid.SetRow(reportBorder, 2);
        grid.Children.Add(reportBorder);

        return grid;
    }

    private async void RefreshDiagnosticsView()
    {
        if (_diagnosticsService == null || _diagHealthChecksPanel == null) return;

        var metrics = _diagnosticsService.GetResourceMetrics();
        double memMb = metrics.ManagedMemoryBytes / (1024.0 * 1024.0);
        _diagResourceText.Text = $"Managed Memory: {memMb:F1} MB | Threads: {metrics.ThreadCount} | Process Uptime: {metrics.ProcessUptime:hh\\:mm\\:ss}";

        var checks = await _diagnosticsService.RunHealthChecksAsync();
        _diagHealthChecksPanel.Children.Clear();

        bool hasDegraded = false;
        bool hasUnhealthy = false;

        foreach (var c in checks)
        {
            if (c.Status == HealthStatus.Unhealthy) hasUnhealthy = true;
            if (c.Status == HealthStatus.Degraded) hasDegraded = true;

            var pill = new Border
            {
                Background = c.Status switch
                {
                    HealthStatus.Healthy => CreateBrush(0x10, 0x7C, 0x41),
                    HealthStatus.Degraded => CreateBrush(0x9E, 0x7C, 0x00),
                    _ => CreateBrush(0xA8, 0x00, 0x00)
                },
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 4, 8, 4)
            };
            var st = new StackPanel { Spacing = 2 };
            st.Children.Add(new TextBlock { Text = c.ComponentName, FontSize = 10, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF) });
            st.Children.Add(new TextBlock { Text = $"{c.Status} ({c.LatencyMs:F0} ms)", FontSize = 8, Foreground = CreateBrush(0xEE, 0xEE, 0xEE) });
            pill.Child = st;
            _diagHealthChecksPanel.Children.Add(pill);
        }

        if (hasUnhealthy)
        {
            _diagStatusBannerText.Text = "System Status: Attention Required";
            _diagStatusBannerText.Foreground = CreateBrush(0xFF, 0x55, 0x55);
        }
        else if (hasDegraded)
        {
            _diagStatusBannerText.Text = "System Status: Degraded Subsystems";
            _diagStatusBannerText.Foreground = CreateBrush(0xFF, 0xBA, 0x00);
        }
        else
        {
            _diagStatusBannerText.Text = "System Status: All Subsystems Healthy";
            _diagStatusBannerText.Foreground = CreateBrush(0x10, 0x7C, 0x41);
        }
    }

    #endregion

    #region AI Workspace Implementation

    private TextBox _aiPromptInput = null!;
    private TextBlock _aiStatusBadgeText = null!;
    private Button _aiOptInToggleBtn = null!;
    private TextBlock _aiPlanPreviewText = null!;
    private TextBlock _aiResponseOutputText = null!;
    private Button _aiExecuteBtn = null!;

    private Grid BuildAiWorkspace()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // Privacy Banner
        var privacyBorder = new Border
        {
            Background = CreateBrush(0x14, 0x14, 0x14),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var bannerGrid = new Grid();
        bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bannerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var bannerStack = new StackPanel { Spacing = 4 };
        bannerStack.Children.Add(new TextBlock { Text = "AI Assistant (Local NLP & Intent Planner)", FontSize = 14, FontWeight = Microsoft.UI.Text.FontWeights.Bold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF) });
        _aiStatusBadgeText = new TextBlock
        {
            Text = _aiAssistant?.IsEnabled == true ? "Status: Enabled (Strict Local Execution, Zero Telemetry)" : "Status: Disabled (Opt-in required for privacy)",
            FontSize = 10,
            Foreground = _aiAssistant?.IsEnabled == true ? CreateBrush(0x10, 0x7C, 0x41) : CreateBrush(0xA0, 0xA0, 0xA0)
        };
        bannerStack.Children.Add(_aiStatusBadgeText);
        Grid.SetColumn(bannerStack, 0);
        bannerGrid.Children.Add(bannerStack);

        _aiOptInToggleBtn = new Button
        {
            Content = _aiAssistant?.IsEnabled == true ? "Disable AI" : "Opt-In & Enable",
            Height = 28,
            Padding = new Thickness(12, 0, 12, 0),
            CornerRadius = new CornerRadius(6)
        };
        _aiOptInToggleBtn.Click += (s, e) =>
        {
            if (_aiAssistant == null) return;
            _aiAssistant.IsEnabled = !_aiAssistant.IsEnabled;
            _aiStatusBadgeText.Text = _aiAssistant.IsEnabled ? "Status: Enabled (Strict Local Execution, Zero Telemetry)" : "Status: Disabled (Opt-in required for privacy)";
            _aiStatusBadgeText.Foreground = _aiAssistant.IsEnabled ? CreateBrush(0x10, 0x7C, 0x41) : CreateBrush(0xA0, 0xA0, 0xA0);
            _aiOptInToggleBtn.Content = _aiAssistant.IsEnabled ? "Disable AI" : "Opt-In & Enable";
            _globalStatusText.Text = _aiAssistant.IsEnabled ? "AI Assistant enabled (Local processing only)." : "AI Assistant disabled.";
        };
        Grid.SetColumn(_aiOptInToggleBtn, 1);
        bannerGrid.Children.Add(_aiOptInToggleBtn);
        privacyBorder.Child = bannerGrid;
        Grid.SetRow(privacyBorder, 0);
        grid.Children.Add(privacyBorder);

        // Prompt Input & Suggestions
        var promptBorder = new Border
        {
            Background = CreateBrush(0x17, 0x17, 0x17),
            BorderBrush = CreateBrush(0x2A, 0x2A, 0x2A),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 10)
        };
        var promptStack = new StackPanel { Spacing = 10 };

        var inputRow = new Grid();
        inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inputRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _aiPromptInput = new TextBox
        {
            PlaceholderText = "Type natural language command: e.g. 'prettify json {\"a\":1}', 'create guid', 'base64 encode hello'",
            Height = 32,
            CornerRadius = new CornerRadius(6)
        };
        Grid.SetColumn(_aiPromptInput, 0);
        inputRow.Children.Add(_aiPromptInput);

        _aiExecuteBtn = new Button
        {
            Content = "Run Command",
            Background = CreateBrush(0x00, 0xB4, 0xFF),
            Foreground = CreateBrush(0x09, 0x09, 0x09),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Height = 32,
            Margin = new Thickness(10, 0, 0, 0),
            Padding = new Thickness(14, 0, 14, 0),
            CornerRadius = new CornerRadius(6)
        };
        _aiExecuteBtn.Click += async (s, e) =>
        {
            if (_aiAssistant == null) return;
            string q = _aiPromptInput.Text.Trim();
            if (string.IsNullOrEmpty(q)) return;

            _aiExecuteBtn.IsEnabled = false;
            _globalStatusText.Text = "AI Assistant planning intent...";

            var plan = await _aiAssistant.PlanIntentAsync(q);
            if (plan != null)
            {
                _aiPlanPreviewText.Text = $"Target Tool: {plan.TargetToolId} | Confidence: {plan.Confidence:P0}\nDescription: {plan.IntentDescription}\nDestructive Gate: {(plan.RequiresConfirmation ? "REQUIRES CONFIRMATION" : "Safe")}";
            }
            else
            {
                _aiPlanPreviewText.Text = "No direct tool mapping recognized. Assistant will provide general desktop help.";
            }

            var res = await _aiAssistant.ProcessQueryAsync(q);
            _aiExecuteBtn.IsEnabled = true;

            _aiResponseOutputText.Text = res.Success
                ? res.AssistantResponse
                : $"Failed or Cancelled: {res.ErrorMessage ?? res.AssistantResponse}";
            _globalStatusText.Text = res.Success ? "AI Assistant execution succeeded." : "AI Assistant execution cancelled/failed.";
        };
        Grid.SetColumn(_aiExecuteBtn, 1);
        inputRow.Children.Add(_aiExecuteBtn);
        promptStack.Children.Add(inputRow);

        // Suggestions
        var chipsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        chipsRow.Children.Add(new TextBlock { Text = "Suggestions:", FontSize = 9, Foreground = CreateBrush(0x88, 0x88, 0x88), VerticalAlignment = VerticalAlignment.Center });
        chipsRow.Children.Add(CreateAiChip("prettify json {\"mode\":\"fast\"}"));
        chipsRow.Children.Add(CreateAiChip("create new guid"));
        chipsRow.Children.Add(CreateAiChip("base64 encode TopBar"));
        chipsRow.Children.Add(CreateAiChip("calculate sha256 of test"));
        promptStack.Children.Add(chipsRow);

        promptBorder.Child = promptStack;
        Grid.SetRow(promptBorder, 1);
        grid.Children.Add(promptBorder);

        // Plan & Response Dual Columns
        var outputGrid = new Grid();
        outputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(320) });
        outputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var planBorder = new Border
        {
            Background = CreateBrush(0x11, 0x11, 0x11),
            BorderBrush = CreateBrush(0x22, 0x22, 0x22),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 10, 0)
        };
        var planStack = new StackPanel { Spacing = 6 };
        planStack.Children.Add(new TextBlock { Text = "Intent Plan Preview", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0x00, 0xB4, 0xFF) });
        _aiPlanPreviewText = new TextBlock
        {
            Text = "Enter a command above to view the synthesized execution plan.",
            FontSize = 10,
            FontFamily = new FontFamily("Consolas"),
            Foreground = CreateBrush(0xA0, 0xA0, 0xA0),
            TextWrapping = TextWrapping.Wrap
        };
        planStack.Children.Add(_aiPlanPreviewText);
        planBorder.Child = planStack;
        Grid.SetColumn(planBorder, 0);
        outputGrid.Children.Add(planBorder);

        var respBorder = new Border
        {
            Background = CreateBrush(0x0D, 0x0D, 0x0D),
            BorderBrush = CreateBrush(0x22, 0x22, 0x22),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12)
        };
        var respStack = new StackPanel { Spacing = 8 };
        var topRow = new Grid();
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topRow.Children.Add(new TextBlock { Text = "Execution Output", FontSize = 11, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold, Foreground = CreateBrush(0xFF, 0xFF, 0xFF) });
        var copyBtn = new Button { Content = "Copy", Height = 22, Padding = new Thickness(8, 0, 8, 0), FontSize = 9, CornerRadius = new CornerRadius(4) };
        copyBtn.Click += (s, e) => CopyToClipboard(_aiResponseOutputText.Text);
        Grid.SetColumn(copyBtn, 1);
        topRow.Children.Add(copyBtn);
        respStack.Children.Add(topRow);

        var respScroller = new ScrollViewer { Height = 200 };
        _aiResponseOutputText = new TextBlock
        {
            Text = "Output from the executed tool will appear here...",
            FontSize = 10,
            FontFamily = new FontFamily("Consolas"),
            Foreground = CreateBrush(0xE6, 0xE6, 0xE6),
            TextWrapping = TextWrapping.Wrap
        };
        respScroller.Content = _aiResponseOutputText;
        respStack.Children.Add(respScroller);
        respBorder.Child = respStack;
        Grid.SetColumn(respBorder, 1);
        outputGrid.Children.Add(respBorder);

        Grid.SetRow(outputGrid, 2);
        grid.Children.Add(outputGrid);

        return grid;
    }

    private Button CreateAiChip(string query)
    {
        var btn = new Button
        {
            Content = query,
            FontSize = 9,
            Height = 22,
            Padding = new Thickness(8, 0, 8, 0),
            CornerRadius = new CornerRadius(11),
            Background = CreateBrush(0x22, 0x22, 0x22),
            Foreground = CreateBrush(0xBB, 0xBB, 0xBB)
        };
        btn.Click += (s, e) =>
        {
            _aiPromptInput.Text = query;
        };
        return btn;
    }

    #endregion

    private void CopyToClipboard(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var dp = new DataPackage();
        dp.SetText(text);
        Clipboard.SetContent(dp);
        _globalStatusText.Text = "Result copied to clipboard.";
    }

    private static SolidColorBrush CreateBrush(byte r, byte g, byte b)
    {
        return new SolidColorBrush(Windows.UI.Color.FromArgb(255, r, g, b));
    }
}
