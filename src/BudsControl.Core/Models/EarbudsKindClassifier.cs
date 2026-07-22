namespace BudsControl.Core.Models;

/// <summary>
/// Best-effort classification of a paired device by its advertised Bluetooth name.
/// This is a heuristic, not a protocol-level identification - a device could rename
/// itself and fool it. It only decides which control protocol to *try*, so a false
/// positive is harmless (the device just fails to connect/handshake) - that's why this
/// matches loosely on "buds" rather than requiring the "Galaxy" prefix. Samsung drops
/// "Galaxy" from the advertised Bluetooth name on some models (e.g. "Galaxy Buds Core"
/// advertises as "Buds Core", optionally with an owner's-name possessive prefix like
/// "Mohammed's Buds Core").
/// </summary>
public static class EarbudsKindClassifier
{
    public static EarbudsKind Classify(string deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return EarbudsKind.Unknown;
        }

        if (deviceName.Contains("buds", StringComparison.OrdinalIgnoreCase))
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
