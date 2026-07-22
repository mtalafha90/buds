using System.Runtime.InteropServices;
using BudsControl.Core.Transport;

namespace BudsControl.Transport.Linux;

public sealed class LinuxL2CapTransportFactory : IL2CapTransportFactory
{
    public Task<IByteStreamTransport> ConnectAsync(string deviceAddress, int psm, CancellationToken ct = default)
    {
        return Task.Run<IByteStreamTransport>(() =>
        {
            int fd = BlueZNative.socket(BlueZNative.AF_BLUETOOTH, BlueZNative.SOCK_SEQPACKET, BlueZNative.BTPROTO_L2CAP);
            if (fd < 0)
            {
                throw new IOException($"Failed to create an L2CAP socket (errno {Marshal.GetLastPInvokeError()}). Is the Bluetooth kernel module loaded?");
            }

            byte[] addr = BlueZNative.BuildSockAddrL2(deviceAddress, psm);
            if (BlueZNative.connect(fd, addr, addr.Length) != 0)
            {
                int errno = Marshal.GetLastPInvokeError();
                BlueZNative.close(fd);
                throw new IOException($"connect() to {deviceAddress} on L2CAP PSM 0x{psm:X} failed (errno {errno}).");
            }

            return new NativeSocketTransport(fd);
        }, ct);
    }
}
