using System.Runtime.InteropServices;
using BudsControl.Core.Transport;

namespace BudsControl.Transport.Linux;

/// <summary>
/// Connects an RFCOMM channel by brute-force scanning channels 1-30 rather than doing a proper
/// SDP service search for the Serial Port Profile UUID. This is a deliberate scope trade-off, not
/// an oversight: implementing a full SDP client was out of scope here, and channel-scanning is a
/// well-established fallback technique for exactly this situation. Most single-profile SPP
/// accessories (Galaxy Buds included, per public reports) sit on a low channel number, so this
/// is usually fast. If it's ever too slow or fails, replace this with a real SDP search (BlueZ
/// historically shipped `sdptool search SP &lt;addr&gt;` for this - it may or may not still be
/// installed on modern distros) and connect directly to the discovered channel instead.
/// </summary>
public sealed class LinuxRfcommTransportFactory : IRfcommTransportFactory
{
    private const int MaxChannel = 30;

    public Task<IByteStreamTransport> ConnectAsync(string deviceAddress, CancellationToken ct = default)
    {
        return Task.Run<IByteStreamTransport>(() =>
        {
            for (byte channel = 1; channel <= MaxChannel; channel++)
            {
                ct.ThrowIfCancellationRequested();

                int fd = BlueZNative.socket(BlueZNative.AF_BLUETOOTH, BlueZNative.SOCK_STREAM, BlueZNative.BTPROTO_RFCOMM);
                if (fd < 0)
                {
                    throw new IOException($"Failed to create an RFCOMM socket (errno {Marshal.GetLastPInvokeError()}). Is the Bluetooth kernel module loaded?");
                }

                byte[] addr = BlueZNative.BuildSockAddrRc(deviceAddress, channel);
                if (BlueZNative.connect(fd, addr, addr.Length) == 0)
                {
                    return new NativeSocketTransport(fd);
                }

                BlueZNative.close(fd);
            }

            throw new IOException($"Could not open an RFCOMM connection to {deviceAddress} on any channel 1-{MaxChannel}. Confirm the device is paired and not already connected to another app (e.g. the phone's Buds app).");
        }, ct);
    }
}
