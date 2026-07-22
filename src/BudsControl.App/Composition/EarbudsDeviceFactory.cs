using BudsControl.Core.Devices;
using BudsControl.Core.Models;
using BudsControl.Core.Transport;
using BudsControl.Protocols.Apple;
using BudsControl.Protocols.Samsung;

namespace BudsControl.App.Composition;

public sealed class EarbudsDeviceFactory(IRfcommTransportFactory rfcomm, IL2CapTransportFactory l2cap) : IEarbudsDeviceFactory
{
    public IEarbudsDevice? Create(PairedDeviceInfo pairedDevice) => pairedDevice.Kind switch
    {
        EarbudsKind.SamsungGalaxyBuds => new SamsungBudsDevice(pairedDevice.Name, pairedDevice.Address, rfcomm),
        EarbudsKind.AppleAirPods => new AirPodsDevice(pairedDevice.Name, pairedDevice.Address, l2cap),
        _ => null,
    };
}
