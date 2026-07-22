using BudsControl.Core.Transport;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;

namespace BudsControl.Transport.Windows;

/// <summary>Connects RFCOMM for Serial Port Profile using WinRT's documented Rfcomm APIs - the standard, well-supported path (unlike Windows L2CAP, see WindowsL2capTransportFactory).</summary>
public sealed class WindowsRfcommTransportFactory : IRfcommTransportFactory
{
    private static readonly Guid SerialPortServiceUuid = RfcommServiceId.SerialPort.Uuid;

    public async Task<IByteStreamTransport> ConnectAsync(string deviceAddress, CancellationToken ct = default)
    {
        ulong bluetoothAddress = ParseBluetoothAddress(deviceAddress);
        using BluetoothDevice device = await BluetoothDevice.FromBluetoothAddressAsync(bluetoothAddress).AsTask(ct);
        if (device is null)
        {
            throw new IOException($"Windows could not resolve a BluetoothDevice for {deviceAddress}. Is it paired?");
        }

        RfcommDeviceServicesResult servicesResult = await device.GetRfcommServicesForIdAsync(RfcommServiceId.SerialPort).AsTask(ct);
        if (servicesResult.Services.Count == 0)
        {
            throw new IOException($"{deviceAddress} does not advertise a Serial Port Profile service.");
        }

        RfcommDeviceService service = servicesResult.Services[0];
        var socket = new StreamSocket();
        await socket.ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName).AsTask(ct);
        return new StreamSocketTransport(socket);
    }

    internal static ulong ParseBluetoothAddress(string address)
    {
        byte[] bytes = address.Split(':').Select(s => Convert.ToByte(s, 16)).ToArray();
        ulong value = 0;
        foreach (byte b in bytes)
        {
            value = (value << 8) | b;
        }

        return value;
    }
}
