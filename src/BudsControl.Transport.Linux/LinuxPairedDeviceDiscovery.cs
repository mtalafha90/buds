using System.Diagnostics;
using BudsControl.Core.Models;
using BudsControl.Core.Transport;

namespace BudsControl.Transport.Linux;

/// <summary>Lists paired devices by shelling out to `bluetoothctl`, which ships with BlueZ on every mainstream distro - avoids taking a direct D-Bus client dependency for a simple read-only listing.</summary>
public sealed class LinuxPairedDeviceDiscovery : IPairedDeviceDiscovery
{
    public async Task<IReadOnlyList<PairedDeviceInfo>> GetPairedDevicesAsync(CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo("bluetoothctl", "devices Paired")
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start bluetoothctl. Is BlueZ installed?");
        string output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        var devices = new List<PairedDeviceInfo>();
        foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            // Format: "Device AA:BB:CC:DD:EE:FF Some Device Name"
            string[] parts = line.Split(' ', 3);
            if (parts.Length < 3 || parts[0] != "Device")
            {
                continue;
            }

            string address = parts[1];
            string name = parts[2].Trim();
            devices.Add(new PairedDeviceInfo(name, address, EarbudsKindClassifier.Classify(name)));
        }

        return devices;
    }
}
