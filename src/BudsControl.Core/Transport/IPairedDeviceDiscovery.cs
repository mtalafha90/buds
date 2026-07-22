using BudsControl.Core.Models;

namespace BudsControl.Core.Transport;

/// <summary>Lists devices already paired at the OS Bluetooth level. Pairing itself is left to the OS's own Bluetooth settings UI - this app never performs pairing.</summary>
public interface IPairedDeviceDiscovery
{
    Task<IReadOnlyList<PairedDeviceInfo>> GetPairedDevicesAsync(CancellationToken ct = default);
}
