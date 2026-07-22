namespace BudsControl.Protocols.Samsung;

/// <summary>Feed raw bytes as they arrive off the socket; raises FrameReceived for each complete, checksum-valid frame. Handles arbitrary chunk boundaries and resyncs past corrupt data.</summary>
public sealed class SppFrameParser
{
    private readonly List<byte> _buffer = new();

    public event EventHandler<SppFrame>? FrameReceived;

    public void Feed(ReadOnlySpan<byte> data)
    {
        _buffer.AddRange(data.ToArray());

        while (true)
        {
            int sofIndex = _buffer.IndexOf(SppFrameCodec.StartOfFrame);
            if (sofIndex < 0)
            {
                _buffer.Clear();
                return;
            }

            if (sofIndex > 0)
            {
                _buffer.RemoveRange(0, sofIndex);
            }

            var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(_buffer);
            if (SppFrameCodec.TryDecode(span, out SppFrame? frame, out int consumed))
            {
                _buffer.RemoveRange(0, consumed);
                FrameReceived?.Invoke(this, frame!);
                continue;
            }

            // Not enough bytes yet to know - wait for more data. Header is SOF+Type+Length (3
            // bytes) before the Length byte itself is even readable; once readable, the frame's
            // total size is Length (MsgId+Payload+CRC) + 4 (SOF+Type+Length+EOF).
            if (span.Length < 3 || span.Length < span[2] + 4)
            {
                return;
            }

            // We had enough bytes but decode failed (bad EOF/checksum) - drop the SOF byte and resync.
            _buffer.RemoveAt(0);
        }
    }
}
