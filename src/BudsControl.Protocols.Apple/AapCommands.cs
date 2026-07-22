using BudsControl.Core.Models;

namespace BudsControl.Protocols.Apple;

/// <summary>
/// Builds raw AAP command packets. See <see cref="AapConstants"/> for the verification caveat -
/// the same applies here: these exact byte sequences are unverified placeholders.
/// </summary>
public static class AapCommands
{
    /// <summary>Sent immediately after the L2CAP connection opens, before any other command is accepted.</summary>
    public static byte[] Handshake { get; } = [0x00, 0x00, 0x04, 0x00, 0x01, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00];

    /// <summary>Subscribes to status notifications (battery, ear detection, noise control changes) - commonly sent right after the handshake.</summary>
    public static byte[] RequestNotifications { get; } = [0x04, 0x00, 0x04, 0x00, 0x0F, 0x00, 0xFF, 0xFF, 0xFF, 0xFF];

    public static byte[] SetNoiseControlMode(NoiseControlMode mode)
    {
        byte value = mode switch
        {
            NoiseControlMode.Off => 0x01,
            NoiseControlMode.NoiseCancelling => 0x02,
            NoiseControlMode.Transparency => 0x03,
            NoiseControlMode.Adaptive => 0x04,
            _ => throw new NotSupportedException($"{mode} is not supported on AirPods."),
        };

        return [0x04, 0x00, 0x04, 0x00, 0x09, 0x00, 0x0D, 0x00, value, 0x00, 0x00, 0x00];
    }
}
