using BudsControl.Core.Models;

namespace BudsControl.Protocols.Apple;

/// <summary>
/// Best-effort parser for inbound AAP notification packets (battery, noise control mode).
/// Same caveat as <see cref="AapConstants"/>: the header prefixes and field layout below are
/// unverified. This parser is deliberately defensive - it returns null rather than guessing when
/// a packet doesn't match the expected shape, so a wrong layout fails loudly (nothing updates)
/// instead of silently showing wrong battery numbers.
/// </summary>
public static class AapNotificationParser
{
    private static readonly byte[] BatteryHeader = [0x04, 0x00, 0x04, 0x00, 0x04, 0x00];
    private static readonly byte[] NoiseControlHeader = [0x04, 0x00, 0x04, 0x00, 0x0B, 0x00];

    public static BatteryStatus? TryParseBattery(ReadOnlySpan<byte> packet)
    {
        if (!packet.StartsWith(BatteryHeader) || packet.Length < BatteryHeader.Length + 1)
        {
            return null;
        }

        int count = packet[BatteryHeader.Length];
        int offset = BatteryHeader.Length + 1;
        int? left = null, right = null, caseP = null;
        bool caseCharging = false;

        for (int i = 0; i < count && offset + 3 <= packet.Length; i++, offset += 3)
        {
            byte component = packet[offset];
            byte level = packet[offset + 1];
            byte status = packet[offset + 2];
            int? percent = level <= 100 ? level : null;

            switch (component)
            {
                case 0x02: left = percent; break;
                case 0x04: right = percent; break;
                case 0x08:
                    caseP = percent;
                    caseCharging = status == 0x01;
                    break;
            }
        }

        if (left is null && right is null && caseP is null)
        {
            return null;
        }

        return new BatteryStatus(left, right, caseP, caseCharging);
    }

    public static NoiseControlMode? TryParseNoiseControlMode(ReadOnlySpan<byte> packet)
    {
        if (!packet.StartsWith(NoiseControlHeader) || packet.Length < NoiseControlHeader.Length + 1)
        {
            return null;
        }

        return packet[NoiseControlHeader.Length] switch
        {
            0x01 => NoiseControlMode.Off,
            0x02 => NoiseControlMode.NoiseCancelling,
            0x03 => NoiseControlMode.Transparency,
            0x04 => NoiseControlMode.Adaptive,
            _ => null,
        };
    }
}
