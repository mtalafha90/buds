using BudsControl.Core.Devices;
using BudsControl.Core.Models;
using BudsControl.Core.Transport;

namespace BudsControl.Protocols.Apple;

/// <summary>
/// Controls AirPods over L2CAP using Apple's proprietary AAP protocol.
/// Relies on the L2CAP transport preserving message boundaries per read (SOCK_SEQPACKET on
/// Linux) - each inbound chunk is treated as one complete AAP packet, not re-framed.
/// </summary>
public sealed class AirPodsDevice : IEarbudsDevice
{
    private readonly IL2CapTransportFactory _transportFactory;
    private IByteStreamTransport? _transport;

    public string Name { get; }
    public string Address { get; }
    public EarbudsKind Kind => EarbudsKind.AppleAirPods;
    public bool IsConnected => _transport?.IsConnected ?? false;
    public BatteryStatus Battery { get; private set; } = BatteryStatus.Unknown;
    public NoiseControlMode NoiseControlMode { get; private set; } = NoiseControlMode.Off;

    public IReadOnlyList<NoiseControlMode> SupportedNoiseControlModes { get; } =
        [NoiseControlMode.Off, NoiseControlMode.NoiseCancelling, NoiseControlMode.Transparency, NoiseControlMode.Adaptive];

    public event EventHandler<BatteryStatus>? BatteryChanged;
    public event EventHandler<NoiseControlMode>? NoiseControlModeChanged;
    public event EventHandler? Disconnected;

    public AirPodsDevice(string name, string address, IL2CapTransportFactory transportFactory)
    {
        Name = name;
        Address = address;
        _transportFactory = transportFactory;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _transport = await _transportFactory.ConnectAsync(Address, AapConstants.Psm, ct);
        _transport.DataReceived += OnDataReceived;
        _transport.Disconnected += (_, _) => Disconnected?.Invoke(this, EventArgs.Empty);

        await _transport.WriteAsync(AapCommands.Handshake, ct);
        await _transport.WriteAsync(AapCommands.RequestNotifications, ct);
    }

    public Task DisconnectAsync() => _transport?.DisconnectAsync() ?? Task.CompletedTask;

    public Task SetNoiseControlModeAsync(NoiseControlMode mode, CancellationToken ct = default)
    {
        if (_transport is null)
        {
            throw new InvalidOperationException("Not connected.");
        }

        return _transport.WriteAsync(AapCommands.SetNoiseControlMode(mode), ct);
    }

    /// <summary>AAP is push-based (no request/response status query is implemented here) - this just re-sends the notification subscription.</summary>
    public Task RefreshStatusAsync(CancellationToken ct = default) =>
        _transport?.WriteAsync(AapCommands.RequestNotifications, ct) ?? Task.CompletedTask;

    private void OnDataReceived(object? sender, ReadOnlyMemory<byte> data)
    {
        BatteryStatus? battery = AapNotificationParser.TryParseBattery(data.Span);
        if (battery is not null)
        {
            Battery = battery;
            BatteryChanged?.Invoke(this, Battery);
            return;
        }

        NoiseControlMode? mode = AapNotificationParser.TryParseNoiseControlMode(data.Span);
        if (mode is not null)
        {
            NoiseControlMode = mode.Value;
            NoiseControlModeChanged?.Invoke(this, NoiseControlMode);
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
