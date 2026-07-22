using System.Runtime.InteropServices;

namespace BudsControl.Transport.Linux;

/// <summary>
/// P/Invoke bindings for Linux's native Bluetooth socket family (AF_BLUETOOTH), as defined in
/// the kernel/glibc headers bluetooth/bluetooth.h, bluetooth/l2cap.h and bluetooth/rfcomm.h.
/// Unlike the Samsung/Apple protocol constants elsewhere in this repo, this is a long-stable,
/// widely-documented kernel ABI (used by BlueZ itself and countless open-source Bluetooth tools),
/// so confidence here is much higher - but it has NOT been exercised against a real socket in
/// this project's build sandbox, which has no Bluetooth adapter/kernel module at all. If connect()
/// ever fails with EINVAL, the most likely culprit is the sockaddr struct sizes below
/// (SizeOfSockAddrL2 / SizeOfSockAddrRc), which can differ 1 byte for struct padding reasons
/// depending on libc/kernel version.
/// </summary>
internal static class BlueZNative
{
    public const int AF_BLUETOOTH = 31;
    public const int BTPROTO_L2CAP = 0;
    public const int BTPROTO_RFCOMM = 3;
    public const int SOCK_STREAM = 1;
    public const int SOCK_SEQPACKET = 5;

    public const int SizeOfSockAddrL2 = 14;
    public const int SizeOfSockAddrRc = 10;

    [DllImport("libc", SetLastError = true)]
    public static extern int socket(int domain, int type, int protocol);

    [DllImport("libc", SetLastError = true)]
    public static extern int connect(int sockfd, byte[] addr, int addrlen);

    [DllImport("libc", SetLastError = true)]
    public static extern nint send(int sockfd, byte[] buf, nint len, int flags);

    [DllImport("libc", SetLastError = true)]
    public static extern nint recv(int sockfd, byte[] buf, nint len, int flags);

    [DllImport("libc", SetLastError = true)]
    public static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    public static extern int shutdown(int sockfd, int how);

    /// <summary>Builds a bdaddr_t-ordered (reversed) 6-byte MAC from an "AA:BB:CC:DD:EE:FF" string.</summary>
    public static byte[] ParseBdAddr(string address)
    {
        byte[] bytes = address.Split(':').Select(s => Convert.ToByte(s, 16)).ToArray();
        if (bytes.Length != 6)
        {
            throw new ArgumentException($"'{address}' is not a valid Bluetooth address.", nameof(address));
        }

        Array.Reverse(bytes);
        return bytes;
    }

    public static byte[] BuildSockAddrL2(string address, int psm)
    {
        byte[] addr = new byte[SizeOfSockAddrL2];
        BitConverter.GetBytes((ushort)AF_BLUETOOTH).CopyTo(addr, 0);
        BitConverter.GetBytes((ushort)psm).CopyTo(addr, 2);
        ParseBdAddr(address).CopyTo(addr, 4);
        // l2_cid = 0, l2_bdaddr_type = 0 (BDADDR_BREDR) - left zeroed.
        return addr;
    }

    public static byte[] BuildSockAddrRc(string address, byte channel)
    {
        byte[] addr = new byte[SizeOfSockAddrRc];
        BitConverter.GetBytes((ushort)AF_BLUETOOTH).CopyTo(addr, 0);
        ParseBdAddr(address).CopyTo(addr, 2);
        addr[8] = channel;
        return addr;
    }
}
