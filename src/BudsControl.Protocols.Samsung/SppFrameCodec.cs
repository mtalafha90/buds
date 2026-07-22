namespace BudsControl.Protocols.Samsung;

/// <summary>
/// Encodes/decodes the "legacy" (non-extended) Galaxy Buds SPP frame:
///   SOF(1)=0xFE | MsgId(1) | Length(1, payload length only) | Payload(n) | CRC16-CCITT(2, big-endian, over MsgId+Payload) | EOF(1)=0xEE
///
/// This only supports payloads up to 255 bytes. Newer buds (Live/Pro/2/2 Pro) reportedly use an
/// "extended" frame variant with a 2-byte length field for larger payloads - that variant is not
/// implemented here; see the README for how to extend this codec if you need it.
/// </summary>
public static class SppFrameCodec
{
    public const byte StartOfFrame = 0xFE;
    public const byte EndOfFrame = 0xEE;

    public static byte[] Encode(SamsungMessageId messageId, ReadOnlySpan<byte> payload)
    {
        if (payload.Length > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), "Legacy SPP frames support at most 255 payload bytes; use the extended frame variant instead.");
        }

        Span<byte> forCrc = stackalloc byte[1 + payload.Length];
        forCrc[0] = (byte)messageId;
        payload.CopyTo(forCrc[1..]);
        ushort crc = Crc16Ccitt.Compute(forCrc);

        byte[] frame = new byte[1 + 1 + 1 + payload.Length + 2 + 1];
        int i = 0;
        frame[i++] = StartOfFrame;
        frame[i++] = (byte)messageId;
        frame[i++] = (byte)payload.Length;
        payload.CopyTo(frame.AsSpan(i));
        i += payload.Length;
        frame[i++] = (byte)(crc >> 8);
        frame[i++] = (byte)(crc & 0xFF);
        frame[i] = EndOfFrame;
        return frame;
    }

    /// <summary>Attempts to decode a single complete frame starting exactly at offset 0 of <paramref name="span"/>. Returns null (and no bytes consumed) if the span is too short to tell yet, or the frame is malformed.</summary>
    internal static bool TryDecode(ReadOnlySpan<byte> span, out SppFrame? frame, out int consumed)
    {
        frame = null;
        consumed = 0;

        if (span.Length < 3 || span[0] != StartOfFrame)
        {
            return false;
        }

        byte payloadLength = span[2];
        int totalLength = 1 + 1 + 1 + payloadLength + 2 + 1;
        if (span.Length < totalLength)
        {
            return false;
        }

        if (span[totalLength - 1] != EndOfFrame)
        {
            return false;
        }

        var messageId = (SamsungMessageId)span[1];
        ReadOnlySpan<byte> payload = span.Slice(3, payloadLength);

        Span<byte> forCrc = stackalloc byte[1 + payloadLength];
        forCrc[0] = span[1];
        payload.CopyTo(forCrc[1..]);
        ushort expectedCrc = Crc16Ccitt.Compute(forCrc);
        ushort actualCrc = (ushort)((span[3 + payloadLength] << 8) | span[3 + payloadLength + 1]);

        if (expectedCrc != actualCrc)
        {
            return false;
        }

        frame = new SppFrame(messageId, payload.ToArray());
        consumed = totalLength;
        return true;
    }
}
