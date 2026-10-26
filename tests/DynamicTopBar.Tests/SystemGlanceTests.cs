using System;
using DynamicTopBar.Core.Models;
using Xunit;

namespace DynamicTopBar.Tests;

public class SystemGlanceTests
{
    [Fact]
    public void DesktopAcPower_HasExpectedDefaults()
    {
        var info = BatteryInfo.DesktopAcPower;

        Assert.False(info.HasBattery);
        Assert.Null(info.RemainingPercentage);
        Assert.Equal(PowerStatusKind.NotPresent, info.Status);
        Assert.Equal("AC Power (No battery)", info.StatusText);
        Assert.Equal("\uE703", info.IconGlyph);
    }

    [Fact]
    public void SystemGlanceSnapshot_Create_FormatsUptimeAndMetadataCorrectly()
    {
        var battery = new BatteryInfo
        {
            HasBattery = true,
            RemainingPercentage = 85,
            Status = PowerStatusKind.Discharging,
            StatusText = "85% · On Battery",
            IconGlyph = "\uE83F"
        };

        var uptime = TimeSpan.FromHours(4) + TimeSpan.FromMinutes(25);
        var snapshot = SystemGlanceSnapshot.Create(battery, uptime, "TEST-RIG", "Windows 11.26100");

        Assert.Equal("TEST-RIG", snapshot.MachineName);
        Assert.Equal("Windows 11.26100", snapshot.OsVersionDescription);
        Assert.Equal("4h 25m", snapshot.UptimeText);
        Assert.True(snapshot.Battery.HasBattery);
        Assert.Equal(85, snapshot.Battery.RemainingPercentage);
        Assert.Equal(PowerStatusKind.Discharging, snapshot.Battery.Status);
        Assert.False(string.IsNullOrWhiteSpace(snapshot.FormattedTime));
        Assert.False(string.IsNullOrWhiteSpace(snapshot.FormattedDate));
    }

    [Fact]
    public void SystemGlanceSnapshot_DesktopWithoutBattery_HandlesGracefully()
    {
        var uptime = TimeSpan.FromHours(2);
        var snapshot = SystemGlanceSnapshot.Create(BatteryInfo.DesktopAcPower, uptime);

        Assert.False(snapshot.Battery.HasBattery);
        Assert.Null(snapshot.Battery.RemainingPercentage);
        Assert.Equal(PowerStatusKind.NotPresent, snapshot.Battery.Status);
        Assert.Equal("AC Power (No battery)", snapshot.Battery.StatusText);
        Assert.Equal("2h 0m", snapshot.UptimeText);
    }

    [Fact]
    public void SystemGlanceSnapshot_WithNetworkInfo_IncludesConnectedStatus()
    {
        var network = new NetworkInfo
        {
            IsConnected = true,
            Connectivity = NetworkConnectivityKind.InternetAccess,
            NetworkName = "Office-WiFi",
            ConnectionType = "Wi-Fi",
            IconGlyph = "\uE701"
        };

        var snapshot = SystemGlanceSnapshot.Create(
            BatteryInfo.DesktopAcPower,
            TimeSpan.FromHours(1),
            network: network);

        Assert.NotNull(snapshot.Network);
        Assert.True(snapshot.Network.IsConnected);
        Assert.Equal("Office-WiFi", snapshot.Network.NetworkName);
        Assert.Equal("Wi-Fi", snapshot.Network.ConnectionType);
        Assert.Equal(NetworkConnectivityKind.InternetAccess, snapshot.Network.Connectivity);
    }

    [Fact]
    public void NetworkInfo_Disconnected_HasExpectedDefaults()
    {
        var disconnected = NetworkInfo.Disconnected;

        Assert.False(disconnected.IsConnected);
        Assert.Equal(NetworkConnectivityKind.Disconnected, disconnected.Connectivity);
        Assert.Equal("No Internet", disconnected.NetworkName);
        Assert.Equal("None", disconnected.ConnectionType);
    }
}
