using System;
using System.Threading;
using Windows.System.Power;
using DynamicTopBar.Core.Interfaces;
using DynamicTopBar.Core.Models;

namespace DynamicTopBar.App.Platform;

/// <summary>
/// Concrete Windows platform adapter implementing ISystemGlanceService
/// using event-driven PowerManager and local system metrics.
/// </summary>
public sealed class WindowsSystemGlanceService : ISystemGlanceService
{
    private readonly object _lock = new();
    private readonly Timer? _refreshTimer;
    private volatile SystemGlanceSnapshot _currentSnapshot;
    private bool _isDisposed;

    public SystemGlanceSnapshot CurrentSnapshot => _currentSnapshot;

    public event EventHandler<SystemGlanceSnapshot>? SnapshotChanged;

    public WindowsSystemGlanceService()
    {
        _currentSnapshot = ReadCurrentMetrics();

        try
        {
            PowerManager.BatteryStatusChanged += OnPowerStatusChanged;
            PowerManager.RemainingChargePercentChanged += OnPowerStatusChanged;
            PowerManager.PowerSupplyStatusChanged += OnPowerStatusChanged;
            Windows.Networking.Connectivity.NetworkInformation.NetworkStatusChanged += OnNetworkStatusChanged;
        }
        catch (Exception)
        {
            // Fallback gracefully on environments where platform events cannot be registered
        }

        // Conservative 5-second interval for clock and uptime refresh without aggressive polling
        _refreshTimer = new Timer(_ => Refresh(), null, 5000, 5000);
    }

    private void OnPowerStatusChanged(object? sender, object e)
    {
        Refresh();
    }

    private void OnNetworkStatusChanged(object sender)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (_isDisposed) return;

        SystemGlanceSnapshot snapshot;
        lock (_lock)
        {
            if (_isDisposed) return;
            snapshot = ReadCurrentMetrics();
            _currentSnapshot = snapshot;
        }

        SnapshotChanged?.Invoke(this, snapshot);
    }

    private static SystemGlanceSnapshot ReadCurrentMetrics()
    {
        BatteryInfo battery;
        try
        {
            var batteryStatus = PowerManager.BatteryStatus;
            if (batteryStatus == BatteryStatus.NotPresent)
            {
                battery = BatteryInfo.DesktopAcPower;
            }
            else
            {
                int percent = PowerManager.RemainingChargePercent;
                var (statusKind, statusText, glyph) = ResolveBatteryDetails(batteryStatus, percent);
                battery = new BatteryInfo
                {
                    HasBattery = true,
                    RemainingPercentage = percent,
                    Status = statusKind,
                    StatusText = statusText,
                    IconGlyph = glyph
                };
            }
        }
        catch (Exception)
        {
            battery = BatteryInfo.DesktopAcPower;
        }

        var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
        string osVersion = $"Windows {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Build}";
        var network = ReadNetworkMetrics();

        return SystemGlanceSnapshot.Create(battery, uptime, Environment.MachineName, osVersion, network);
    }

    private static NetworkInfo ReadNetworkMetrics()
    {
        try
        {
            var profile = Windows.Networking.Connectivity.NetworkInformation.GetInternetConnectionProfile();
            if (profile == null)
            {
                return NetworkInfo.Disconnected;
            }

            var level = profile.GetNetworkConnectivityLevel();
            bool isInternet = level == Windows.Networking.Connectivity.NetworkConnectivityLevel.InternetAccess;
            string name = profile.ProfileName ?? "Connected";

            string type = "Ethernet";
            string glyph = "\uE839"; // Ethernet
            if (profile.IsWlanConnectionProfile)
            {
                type = "Wi-Fi";
                glyph = "\uE701"; // Wi-Fi
            }
            else if (profile.IsWwanConnectionProfile)
            {
                type = "Cellular";
                glyph = "\uE870"; // Cellular
            }

            return new NetworkInfo
            {
                IsConnected = isInternet || level == Windows.Networking.Connectivity.NetworkConnectivityLevel.ConstrainedInternetAccess,
                Connectivity = isInternet ? NetworkConnectivityKind.InternetAccess : NetworkConnectivityKind.LocalAccess,
                NetworkName = name,
                ConnectionType = type,
                IconGlyph = isInternet ? glyph : "\uEB55"
            };
        }
        catch
        {
            return NetworkInfo.Disconnected;
        }
    }

    private static (PowerStatusKind Kind, string Text, string Glyph) ResolveBatteryDetails(BatteryStatus status, int percent)
    {
        switch (status)
        {
            case BatteryStatus.Charging:
                return (PowerStatusKind.Charging, $"{percent}% · Charging", "\uEBAA"); // BatteryCharging
            case BatteryStatus.Discharging:
                return (PowerStatusKind.Discharging, $"{percent}% · On Battery", "\uE83F"); // Battery
            case BatteryStatus.Idle:
                return (PowerStatusKind.Full, $"{percent}% · AC Connected", "\uE703"); // Plug
            default:
                return (PowerStatusKind.Unknown, $"{percent}% · Unknown", "\uE83F");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                PowerManager.BatteryStatusChanged -= OnPowerStatusChanged;
                PowerManager.RemainingChargePercentChanged -= OnPowerStatusChanged;
                PowerManager.PowerSupplyStatusChanged -= OnPowerStatusChanged;
                Windows.Networking.Connectivity.NetworkInformation.NetworkStatusChanged -= OnNetworkStatusChanged;
            }
            catch { }

            _refreshTimer?.Dispose();
        }
    }
}
