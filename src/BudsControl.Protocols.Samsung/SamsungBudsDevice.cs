using BudsControl.Core.Devices;
using BudsControl.Core.Models;
using BudsControl.Core.Transport;

namespace BudsControl.Protocols.Samsung;

/// <summary>
/// Controls a Galaxy Buds case over RFCOMM/SPP.
/// The payload byte layouts below (battery bytes, noise-control value mapping) are, like the
/// message IDs in <see cref="SamsungMessageId"/>, unverified placeholders - see that file's
/// doc comment for how to confirm them against real hardware.
/// </summary>
public sealed class SamsungBudsDevice : IEarbudsDevice
{
    private readonly IRfcommTransportFactory _transportFactory;
    private readonly SppFrameParser _parser = new();
    private IByteStreamTransport? _transport;

    public string Name { get; }
    public string Address { get; }
    public EarbudsKind Kind => EarbudsKind.SamsungGalaxyBuds;
    public bool IsConnected => _transport?.IsConnected ?? false;
    public BatteryStatus Battery { get; private set; } = BatteryStatus.Unknown;
    public NoiseControlMode NoiseControlMode { get; private set; } = NoiseControlMode.Off;

    public IReadOnlyList<NoiseControlMode> SupportedNoiseControlModes { get; } =
        [NoiseControlMode.Off, NoiseControlMode.NoiseCancelling, NoiseControlMode.Transparency];

    public event EventHandler<BatteryStatus>? BatteryChanged;
    public event EventHandler<NoiseControlMode>? NoiseControlModeChanged;
    public event EventHandler? Disconnected;

    public SamsungBudsDevice(string name, string address, IRfcommTransportFactory transportFactory)
    {
        Name = name;
        Address = address;
        _transportFactory = transportFactory;
        _parser.FrameReceived += OnFrameReceived;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _transport = await _transportFactory.ConnectAsync(Address, ct);
        _transport.DataReceived += (_, data) => _parser.Feed(data.Span);
        _transport.Disconnected += (_, _) => Disconnected?.Invoke(this, EventArgs.Empty);
        await RequestStatusAsync(ct);
    }

    public Task DisconnectAsync() => _transport?.DisconnectAsync() ?? Task.CompletedTask;

    public async Task SetNoiseControlModeAsync(NoiseControlMode mode, CancellationToken ct = default)
    {
        if (_transport is null)
        {
            throw new InvalidOperationException("Not connected.");
        }

        byte value = mode switch
        {
            NoiseControlMode.Off => 0x00,
            NoiseControlMode.NoiseCancelling => 0x01,
            NoiseControlMode.Transparency => 0x02,
            _ => throw new NotSupportedException($"{mode} is not supported on Galaxy Buds."),
        };

        byte[] frame = SppFrameCodec.Encode(SamsungMessageId.SetNoiseControlMode, [value]);
        await _transport.WriteAsync(frame, ct);
    }

    public Task RefreshStatusAsync(CancellationToken ct = default) => RequestStatusAsync(ct);

    private Task RequestStatusAsync(CancellationToken ct)
    {
        if (_transport is null)
        {
            return Task.CompletedTask;
        }

        byte[] frame = SppFrameCodec.Encode(SamsungMessageId.RequestStatusUpdated, ReadOnlySpan<byte>.Empty);
        return _transport.WriteAsync(frame, ct);
    }

    private void OnFrameReceived(object? sender, SppFrame frame)
    {
        switch (frame.MessageId)
        {
            case SamsungMessageId.BatteryStatusUpdated when frame.Payload.Length >= 2:
                Battery = new BatteryStatus(frame.Payload[0], frame.Payload[1], null);
                BatteryChanged?.Invoke(this, Battery);
                break;

            case SamsungMessageId.NoiseControlModeUpdated when frame.Payload.Length >= 1:
                NoiseControlMode = frame.Payload[0] switch
                {
                    0x01 => NoiseControlMode.NoiseCancelling,
                    0x02 => NoiseControlMode.Transparency,
                    _ => NoiseControlMode.Off,
                };
                NoiseControlModeChanged?.Invoke(this, NoiseControlMode);
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transport is not null)
        {
            await _transport.DisposeAsync();
        }
    }
}
