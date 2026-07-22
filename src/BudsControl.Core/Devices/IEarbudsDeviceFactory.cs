using BudsControl.Core.Models;

namespace BudsControl.Core.Devices;

/// <summary>Builds the right IEarbudsDevice implementation (Samsung SPP or Apple AAP) for a paired device.</summary>
public interface IEarbudsDeviceFactory
{
    /// <summary>Returns null if the device's kind is not supported (i.e. neither Samsung Galaxy Buds nor Apple AirPods).</summary>
    IEarbudsDevice? Create(PairedDeviceInfo pairedDevice);
}
