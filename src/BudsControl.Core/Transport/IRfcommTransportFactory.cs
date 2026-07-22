namespace BudsControl.Core.Transport;

/// <summary>
/// Opens an RFCOMM (Bluetooth Serial Port Profile) connection to an already-paired device.
/// Samsung Galaxy Buds are controlled over an RFCOMM channel discovered via SDP for the
/// standard Serial Port Profile UUID (00001101-0000-1000-8000-00805F9B34FB).
/// </summary>
public interface IRfcommTransportFactory
{
    Task<IByteStreamTransport> ConnectAsync(string deviceAddress, CancellationToken ct = default);
}
