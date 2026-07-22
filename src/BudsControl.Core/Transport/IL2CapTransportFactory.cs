namespace BudsControl.Core.Transport;

/// <summary>
/// Opens an L2CAP connection to an already-paired device on a fixed PSM (Protocol/Service
/// Multiplexer). AirPods are controlled over Apple's proprietary AAP protocol on a fixed L2CAP
/// PSM rather than an SDP-discovered RFCOMM channel.
/// </summary>
public interface IL2CapTransportFactory
{
    Task<IByteStreamTransport> ConnectAsync(string deviceAddress, int psm, CancellationToken ct = default);
}
