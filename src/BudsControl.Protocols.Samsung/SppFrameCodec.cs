namespace BudsControl.Protocols.Samsung;

/// <summary>
/// Encodes/decodes the "legacy" Galaxy Buds SPP frame:
///   SOF(1)=0xFE | Type(1: Request=0/Response=1) | Length(1) | MsgId(1) | Payload(n) | CRC16-CCITT(2, big-endian, over MsgId+Payload) | EOF(1)=0xEE
///
/// The Length byte is the size of MsgId+Payload+CRC combined (n+3), NOT the payload alone - this
/// was a real bug in an earlier version of this file (it also omitted the Type byte entirely),
/// found and fixed after fetching the actual protocol notes and reference decoder source rather
/// than continuing to guess. This only supports payloads that keep MsgId+Payload+CRC under 255
/// bytes total; newer buds reportedly use a different framing (0xFD/0xDD SOF/EOF) with a 2-byte
/// length for larger payloads - not implemented here.
/// </summary>
public static class SppFrameCodec
{
    public const byte StartOfFrame = 0xFE;
    public const byte EndOfFrame = 0xEE;

    public static byte[] Encode(SppMsgType type, SamsungMessageId messageId, ReadOnlySpan<byte> payload)
    {
        int innerLength = 1 + payload.Length + 2; // MsgId + Payload + CRC
        if (innerLength > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(payload), "Legacy SPP frames support at most 252 payload bytes.");
        }

        Span<byte> forCrc = stackalloc byte[1 + payload.Length];
        forCrc[0] = (byte)messageId;
        payload.CopyTo(forCrc[1..]);
        ushort crc = Crc16Ccitt.Compute(forCrc);

        byte[] frame = new byte[1 + 1 + 1 + 1 + payload.Length + 2 + 1];
        int i = 0;
        frame[i++] = StartOfFrame;
        frame[i++] = (byte)type;
        frame[i++] = (byte)innerLength;
        frame[i++] = (byte)messageId;
        payload.CopyTo(frame.AsSpan(i));
        i += payload.Length;
        frame[i++] = (byte)(crc >> 8);
        frame[i++] = (byte)(crc & 0xFF);
        frame[i] = EndOfFrame;
        return frame;
    }

    /// <summary>Attempts to decode a single complete frame starting exactly at offset 0 of <paramref name="span"/>. Returns false (and no bytes consumed) if the span is too short to tell yet, or the frame is malformed.</summary>
    internal static bool TryDecode(ReadOnlySpan<byte> span, out SppFrame? frame, out int consumed)
    {
        frame = null;
        consumed = 0;

        if (span.Length < 4 || span[0] != StartOfFrame)
        {
            return false;
        }

        byte innerLength = span[2]; // MsgId + Payload + CRC
        if (innerLength < 3)
        {
            return false;
        }

        int totalLength = innerLength + 4; // SOF + Type + Length + innerLength + EOF
        if (span.Length < totalLength)
        {
            return false;
        }

        if (span[totalLength - 1] != EndOfFrame)
        {
            return false;
        }

        var type = (SppMsgType)span[1];
        var messageId = (SamsungMessageId)span[3];
        int payloadLength = innerLength - 3;
        ReadOnlySpan<byte> payload = span.Slice(4, payloadLength);

        Span<byte> forCrc = stackalloc byte[1 + payloadLength];
        forCrc[0] = span[3];
        payload.CopyTo(forCrc[1..]);
        ushort expectedCrc = Crc16Ccitt.Compute(forCrc);
        ushort actualCrc = (ushort)((span[4 + payloadLength] << 8) | span[4 + payloadLength + 1]);

        if (expectedCrc != actualCrc)
        {
            return false;
        }

        frame = new SppFrame(type, messageId, payload.ToArray());
        consumed = totalLength;
        return true;
    }
}
