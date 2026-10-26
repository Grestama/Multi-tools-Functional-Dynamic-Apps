using System;

namespace DynamicTopBar.Core.Models;

/// <summary>
/// Classification of the system power / battery status.
/// </summary>
public enum PowerStatusKind
{
    NotPresent,
    Discharging,
    Charging,
    ACPower,
    Full,
    Unknown
}

/// <summary>
/// Status information regarding the host system's power supply and battery.
/// </summary>
public record BatteryInfo
{
    public bool HasBattery { get; init; }
    public int? RemainingPercentage { get; init; }
    public PowerStatusKind Status { get; init; } = PowerStatusKind.NotPresent;
    public string StatusText { get; init; } = "AC Power (Desktop)";
    public string IconGlyph { get; init; } = "\uE83F"; // Battery status icon

    public static BatteryInfo DesktopAcPower => new()
    {
        HasBattery = false,
        RemainingPercentage = null,
        Status = PowerStatusKind.NotPresent,
        StatusText = "AC Power (No battery)",
        IconGlyph = "\uE703" // Plug / Power
    };
}

/// <summary>
/// Status of host system network connectivity.
/// </summary>
public enum NetworkConnectivityKind
{
    Disconnected,
    LocalAccess,
    InternetAccess,
    Unknown
}

/// <summary>
/// Network connectivity details.
/// </summary>
public record NetworkInfo
{
    public bool IsConnected { get; init; }
    public NetworkConnectivityKind Connectivity { get; init; } = NetworkConnectivityKind.Unknown;
    public string NetworkName { get; init; } = "Not connected";
    public string ConnectionType { get; init; } = "None"; // Wi-Fi, Ethernet, Cellular
    public string IconGlyph { get; init; } = "\uEB55"; // Disconnected network icon

    public static NetworkInfo Disconnected => new()
    {
        IsConnected = false,
        Connectivity = NetworkConnectivityKind.Disconnected,
        NetworkName = "No Internet",
        ConnectionType = "None",
        IconGlyph = "\uEB55"
    };
}

/// <summary>
/// Snapshot of system glance metrics (realtime clock, battery, network, machine name, OS description, uptime).
/// </summary>
public record SystemGlanceSnapshot
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string FormattedTime => Timestamp.ToString("h:mm tt");
    public string FormattedDate => Timestamp.ToString("dddd, MMM d, yyyy");
    public BatteryInfo Battery { get; init; } = BatteryInfo.DesktopAcPower;
    public NetworkInfo Network { get; init; } = NetworkInfo.Disconnected;
    public string MachineName { get; init; } = Environment.MachineName;
    public string OsVersionDescription { get; init; } = "Windows";
    public string UptimeText { get; init; } = "0h 0m";

    public static SystemGlanceSnapshot Create(BatteryInfo battery, TimeSpan uptime, string? machineName = null, string? osVersion = null, NetworkInfo? network = null)
    {
        int hours = (int)uptime.TotalHours;
        int minutes = uptime.Minutes;

        return new SystemGlanceSnapshot
        {
            Timestamp = DateTime.Now,
            Battery = battery,
            Network = network ?? NetworkInfo.Disconnected,
            MachineName = machineName ?? Environment.MachineName,
            OsVersionDescription = osVersion ?? "Windows",
            UptimeText = $"{hours}h {minutes}m"
        };
    }
}
