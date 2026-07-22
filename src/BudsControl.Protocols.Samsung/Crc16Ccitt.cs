namespace BudsControl.Protocols.Samsung;

/// <summary>
/// CRC-16/CCITT-FALSE (poly 0x1021, init 0xFFFF, no reflect, xorout 0x0000).
/// NOTE: multiple CRC16-CCITT variants exist (different init values / bit reflection). This is
/// the most commonly used default and has NOT been verified byte-for-byte against a real Galaxy
/// Buds firmware capture. If checksums are rejected by real hardware, this is the first thing to
/// re-derive from a packet capture.
/// </summary>
public static class Crc16Ccitt
{
    public static ushort Compute(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (byte b in data)
        {
            crc ^= (ushort)(b << 8);
            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 0x8000) != 0 ? (ushort)((crc << 1) ^ 0x1021) : (ushort)(crc << 1);
            }
        }

        return crc;
    }
}
