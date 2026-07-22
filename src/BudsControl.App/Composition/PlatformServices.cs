using BudsControl.Core.Transport;
#if LINUX
using BudsControl.Transport.Linux;
#elif WINDOWS
using BudsControl.Transport.Windows;
#endif

namespace BudsControl.App.Composition;

/// <summary>Picks the platform-specific transport implementations at compile time via the LINUX/WINDOWS constants set per-TargetFramework in BudsControl.App.csproj.</summary>
public static class PlatformServices
{
    public static IPairedDeviceDiscovery CreateDiscovery() =>
#if LINUX
        new LinuxPairedDeviceDiscovery();
#elif WINDOWS
        new WindowsPairedDeviceDiscovery();
#else
        throw new PlatformNotSupportedException();
#endif

    public static IRfcommTransportFactory CreateRfcommFactory() =>
#if LINUX
        new LinuxRfcommTransportFactory();
#elif WINDOWS
        new WindowsRfcommTransportFactory();
#else
        throw new PlatformNotSupportedException();
#endif

    public static IL2CapTransportFactory CreateL2CapFactory() =>
#if LINUX
        new LinuxL2CapTransportFactory();
#elif WINDOWS
        new WindowsL2capTransportFactory();
#else
        throw new PlatformNotSupportedException();
#endif
}
