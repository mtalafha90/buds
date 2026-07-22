using BudsControl.Core.Models;

namespace BudsControl.Core.Devices;

/// <summary>Protocol-agnostic control surface the UI programs against, regardless of whether the earbuds are Samsung or Apple.</summary>
public interface IEarbudsDevice : IAsyncDisposable
{
    string Name { get; }
    string Address { get; }
    EarbudsKind Kind { get; }
    bool IsConnected { get; }
    BatteryStatus Battery { get; }
    NoiseControlMode NoiseControlMode { get; }
    IReadOnlyList<NoiseControlMode> SupportedNoiseControlModes { get; }

    event EventHandler<BatteryStatus>? BatteryChanged;
    event EventHandler<NoiseControlMode>? NoiseControlModeChanged;
    event EventHandler? Disconnected;

    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync();
    Task SetNoiseControlModeAsync(NoiseControlMode mode, CancellationToken ct = default);
    Task RefreshStatusAsync(CancellationToken ct = default);
}
