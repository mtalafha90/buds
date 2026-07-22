using System.Runtime.InteropServices;
using BudsControl.Core.Transport;

namespace BudsControl.Transport.Windows;

/// <summary>Connects L2CAP by PSM using raw Winsock (AF_BTH) - see WinsockBluetoothNative for why this isn't done through WinRT.</summary>
public sealed class WindowsL2capTransportFactory : IL2CapTransportFactory
{
    public Task<IByteStreamTransport> ConnectAsync(string deviceAddress, int psm, CancellationToken ct = default)
    {
        return Task.Run<IByteStreamTransport>(() =>
        {
            WinsockBluetoothNative.EnsureInitialized();

            nint handle = WinsockBluetoothNative.socket(WinsockBluetoothNative.AF_BTH, WinsockBluetoothNative.SOCK_STREAM, WinsockBluetoothNative.BTHPROTO_L2CAP);
            if (handle == nint.Zero || handle == -1)
            {
                throw new IOException($"Failed to create an L2CAP socket (WSAGetLastError {Marshal.GetLastPInvokeError()}).");
            }

            byte[] addr = WinsockBluetoothNative.BuildSockAddrBth(deviceAddress, (uint)psm);
            if (WinsockBluetoothNative.connect(handle, addr, addr.Length) != 0)
            {
                int error = Marshal.GetLastPInvokeError();
                WinsockBluetoothNative.closesocket(handle);
                throw new IOException($"connect() to {deviceAddress} on L2CAP PSM 0x{psm:X} failed (WSAGetLastError {error}).");
            }

            return new WinsockSocketTransport(handle);
        }, ct);
    }
}
