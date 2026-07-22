using System.Runtime.InteropServices;
using BudsControl.Core.Transport;

namespace BudsControl.Transport.Linux;

/// <summary>Wraps an already-connected AF_BLUETOOTH socket fd as an IByteStreamTransport, with a background read loop.</summary>
internal sealed class NativeSocketTransport : IByteStreamTransport
{
    private readonly int _fd;
    private readonly CancellationTokenSource _readLoopCts = new();
    private readonly Task _readLoopTask;
    private int _disposed;

    public bool IsConnected { get; private set; } = true;

    public event EventHandler<ReadOnlyMemory<byte>>? DataReceived;
    public event EventHandler? Disconnected;

    public NativeSocketTransport(int fd)
    {
        _fd = fd;
        _readLoopTask = Task.Run(ReadLoop);
    }

    private void ReadLoop()
    {
        byte[] buffer = new byte[1024];
        while (!_readLoopCts.IsCancellationRequested)
        {
            nint n = BlueZNative.recv(_fd, buffer, buffer.Length, 0);
            if (n <= 0)
            {
                break;
            }

            DataReceived?.Invoke(this, buffer.AsMemory(0, (int)n));
        }

        if (IsConnected)
        {
            IsConnected = false;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }
    }

    public Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Transport is not connected.");
        }

        byte[] bytes = data.ToArray();
        nint sent = BlueZNative.send(_fd, bytes, bytes.Length, 0);
        if (sent < 0)
        {
            throw new IOException($"send() failed (errno {Marshal.GetLastPInvokeError()}).");
        }

        return Task.CompletedTask;
    }

    public Task DisconnectAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            IsConnected = false;
            _readLoopCts.Cancel();
            BlueZNative.shutdown(_fd, 2 /* SHUT_RDWR */);
            BlueZNative.close(_fd);
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
            // best-effort - recv() unblocks once the fd is closed above, but don't hang disposal on it.
        }
    }
}
