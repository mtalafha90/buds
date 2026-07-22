namespace BudsControl.Core.Models;

/// <summary>
/// Best-effort classification of a paired device by its advertised Bluetooth name.
/// This is a heuristic, not a protocol-level identification - a device could rename
/// itself and fool it. It only decides which control protocol to *try*.
/// </summary>
public static class EarbudsKindClassifier
{
    public static EarbudsKind Classify(string deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return EarbudsKind.Unknown;
        }

        if (deviceName.Contains("Galaxy Buds", StringComparison.OrdinalIgnoreCase))
        {
            return EarbudsKind.SamsungGalaxyBuds;
        }

        if (deviceName.Contains("AirPods", StringComparison.OrdinalIgnoreCase))
        {
            return EarbudsKind.AppleAirPods;
        }

        return EarbudsKind.Unknown;
    }
}
