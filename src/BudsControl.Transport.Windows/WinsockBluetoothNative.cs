using System.Runtime.InteropServices;

namespace BudsControl.Transport.Windows;

/// <summary>
/// P/Invoke bindings for classic Winsock's native Bluetooth address family (AF_BTH), as defined
/// in ws2bth.h. WinRT's StreamSocket does not expose arbitrary L2CAP PSM connections (it only
/// documents RFCOMM via RfcommDeviceService), so real desktop Bluetooth accessory apps on Windows
/// go through raw Winsock instead - this mirrors that approach rather than WinRT.
/// This is stable, long-documented Win32 API, but - like the Linux P/Invoke layer - it has not
/// been exercised against a real socket in this project's Linux build sandbox. If WSAConnect ever
/// fails with WSAEINVAL, the most likely culprit is SizeOfSockAddrBth below, which pads
/// SOCKADDR_BTH to 8-byte alignment because of its ULONGLONG member - double check against
/// &lt;ws2bth.h&gt; on the target machine's SDK if that happens.
/// </summary>
internal static class WinsockBluetoothNative
{
    public const int AF_BTH = 32;
    public const int SOCK_STREAM = 1;
    public const int BTHPROTO_RFCOMM = 0x0003;
    public const int BTHPROTO_L2CAP = 0x0100;
    public const int SizeOfSockAddrBth = 40;

    [DllImport("ws2_32.dll", SetLastError = true)]
    public static extern int WSAStartup(ushort wVersionRequested, byte[] lpWsaData);

    [DllImport("ws2_32.dll", SetLastError = true)]
    public static extern nint socket(int af, int type, int protocol);

    [DllImport("ws2_32.dll", SetLastError = true)]
    public static extern int connect(nint s, byte[] name, int namelen);

    [DllImport("ws2_32.dll", SetLastError = true)]
    public static extern int send(nint s, byte[] buf, int len, int flags);

    [DllImport("ws2_32.dll", SetLastError = true)]
    public static extern int recv(nint s, byte[] buf, int len, int flags);

    [DllImport("ws2_32.dll", SetLastError = true)]
    public static extern int closesocket(nint s);

    [DllImport("ws2_32.dll", SetLastError = true)]
    public static extern int shutdown(nint s, int how);

    public static void EnsureInitialized()
    {
        // WSAStartup must be called once before any other Winsock call. 512 bytes is more than
        // enough for WSADATA on any Windows version - we never read the struct back.
        WSAStartup(0x0202, new byte[512]);
    }

    public static byte[] BuildSockAddrBth(string address, uint port)
    {
        byte[] addr = new byte[SizeOfSockAddrBth];
        BitConverter.GetBytes((ushort)AF_BTH).CopyTo(addr, 0);
        BitConverter.GetBytes(ParseBthAddr(address)).CopyTo(addr, 8);
        // serviceClassId (GUID, offset 16) left as GUID_NULL - connecting directly by port/PSM.
        BitConverter.GetBytes(port).CopyTo(addr, 32);
        return addr;
    }

    /// <summary>BTH_ADDR packs "AA:BB:CC:DD:EE:FF" as a plain big-endian 48-bit integer (first octet most significant) in the low 6 bytes of a ULONGLONG.</summary>
    private static ulong ParseBthAddr(string address)
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
