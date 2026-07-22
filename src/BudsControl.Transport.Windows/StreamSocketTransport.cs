using BudsControl.Core.Transport;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace BudsControl.Transport.Windows;

/// <summary>Wraps a connected WinRT StreamSocket (used for both RFCOMM and L2CAP on Windows) as an IByteStreamTransport.</summary>
internal sealed class StreamSocketTransport : IByteStreamTransport
{
    private readonly StreamSocket _socket;
    private readonly DataWriter _writer;
    private readonly DataReader _reader;
    private readonly CancellationTokenSource _readLoopCts = new();
    private readonly Task _readLoopTask;
    private int _disposed;

    public bool IsConnected { get; private set; } = true;

    public event EventHandler<ReadOnlyMemory<byte>>? DataReceived;
    public event EventHandler? Disconnected;

    public StreamSocketTransport(StreamSocket socket)
    {
        _socket = socket;
        _writer = new DataWriter(socket.OutputStream);
        _reader = new DataReader(socket.InputStream) { InputStreamOptions = InputStreamOptions.Partial };
        _readLoopTask = Task.Run(ReadLoopAsync);
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            while (!_readLoopCts.IsCancellationRequested)
            {
                uint loaded = await _reader.LoadAsync(4096).AsTask(_readLoopCts.Token);
                if (loaded == 0)
                {
                    break;
                }

                byte[] buffer = new byte[loaded];
                _reader.ReadBytes(buffer);
                DataReceived?.Invoke(this, buffer);
            }
        }
        catch (OperationCanceledException)
        {
            // expected on disconnect
        }
        catch (Exception)
        {
            // connection dropped unexpectedly - fall through to the Disconnected notification below
        }

        if (IsConnected)
        {
            IsConnected = false;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        _writer.WriteBytes(data.ToArray());
        await _writer.StoreAsync().AsTask(ct);
    }

    public Task DisconnectAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            IsConnected = false;
            _readLoopCts.Cancel();
            _socket.Dispose();
        }

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        try
        {
            await _readLoopTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // best-effort
        }

        _writer.Dispose();
        _reader.Dispose();
    }
}
