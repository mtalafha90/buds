using BudsControl.Core.Models;
using BudsControl.Core.Transport;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace BudsControl.Transport.Windows;

public sealed class WindowsPairedDeviceDiscovery : IPairedDeviceDiscovery
{
    public async Task<IReadOnlyList<PairedDeviceInfo>> GetPairedDevicesAsync(CancellationToken ct = default)
    {
        string selector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        DeviceInformationCollection infos = await DeviceInformation.FindAllAsync(selector).AsTask(ct);

        var devices = new List<PairedDeviceInfo>();
        foreach (DeviceInformation info in infos)
        {
            using BluetoothDevice? device = await BluetoothDevice.FromIdAsync(info.Id).AsTask(ct);
            if (device is null)
            {
                continue;
            }

            string address = FormatAddress(device.BluetoothAddress);
            devices.Add(new PairedDeviceInfo(info.Name, address, EarbudsKindClassifier.Classify(info.Name)));
        }

        return devices;
    }

    private static string FormatAddress(ulong bluetoothAddress)
    {
        byte[] bytes = new byte[6];
        for (int i = 5; i >= 0; i--)
        {
            bytes[i] = (byte)(bluetoothAddress & 0xFF);
            bluetoothAddress >>= 8;
        }

        return string.Join(':', bytes.Select(b => b.ToString("X2")));
    }
}
