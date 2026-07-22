namespace BudsControl.Core.Transport;

/// <summary>
/// A connected, ordered, reliable byte stream to an earbuds case/device - the shape shared by
/// an RFCOMM (Samsung) and an L2CAP (Apple) connection once each is established.
/// Platform transport implementations (Linux/Windows) only need to implement this.
/// </summary>
public interface IByteStreamTransport : IAsyncDisposable
{
    bool IsConnected { get; }

    /// <summary>Raised on every inbound chunk. Consumers are expected to re-assemble framing themselves.</summary>
    event EventHandler<ReadOnlyMemory<byte>>? DataReceived;

    /// <summary>Raised once if the connection drops unexpectedly (not via DisconnectAsync).</summary>
    event EventHandler? Disconnected;

    Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default);

    Task DisconnectAsync();
}
