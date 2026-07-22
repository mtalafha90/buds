using BudsControl.Core.Devices;
using BudsControl.Core.Models;
using BudsControl.Core.Transport;

namespace BudsControl.Protocols.Samsung;

/// <summary>
/// Controls a Galaxy Buds case over RFCOMM/SPP.
///
/// Connection handshake: the device is expected to push a StatusUpdated/ExtendedStatusUpdated
/// frame unsolicited shortly after the socket opens (there is no "please send me your status"
/// command - see SamsungMessageId's doc comment). Per the reference protocol notes, the client is
/// then expected to acknowledge it by echoing the same frame back as a Response, followed by a
/// ManagerInfo identification message - both handled in OnFrameReceived below. Without that
/// handshake the device may never accept further commands like NoiseControls.
///
/// The battery/noise-control payload layouts are, like the message IDs in
/// <see cref="SamsungMessageId"/>, sourced from the reference decoder classes rather than
/// guessed - except the exact NoiseControlModes numeric mapping, which is still a guess (see that
/// file's doc comment).
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
        // No explicit "request status" step - the device pushes StatusUpdated on its own once
        // connected; see the handshake note in this class's doc comment.
    }

    public Task DisconnectAsync() => _transport?.DisconnectAsync() ?? Task.CompletedTask;

    public Task SetNoiseControlModeAsync(NoiseControlMode mode, CancellationToken ct = default)
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

        byte[] frame = SppFrameCodec.Encode(SppMsgType.Request, SamsungMessageId.NoiseControls, [value]);
        return _transport.WriteAsync(frame, ct);
    }

    /// <summary>StatusUpdated is pushed unsolicited by the device, not requested - this just re-sends the ManagerInfo handshake in case the device wants a fresh one.</summary>
    public Task RefreshStatusAsync(CancellationToken ct = default) => SendManagerInfoAsync(ct);

    private async void OnFrameReceived(object? sender, SppFrame frame)
    {
        switch (frame.MessageId)
        {
            case SamsungMessageId.StatusUpdated when frame.Payload.Length >= 3:
                ParseStatusUpdated(frame.Payload);
                await AcknowledgeAndIdentifyAsync(frame);
                break;

            case SamsungMessageId.ExtendedStatusUpdated when frame.Payload.Length >= 4:
                ParseExtendedStatusUpdated(frame.Payload);
                await AcknowledgeAndIdentifyAsync(frame);
                break;

            case SamsungMessageId.NoiseControlsUpdate when frame.Payload.Length >= 1:
                NoiseControlMode = frame.Payload[0] switch
                {
                    0x01 => NoiseControlMode.NoiseCancelling,
                    0x02 => NoiseControlMode.Transparency,
                    _ => NoiseControlMode.Off,
                };
                NoiseControlModeChanged?.Invoke(this, NoiseControlMode);
                break;

            case SamsungMessageId.AmbientModeUpdated when frame.Payload.Length >= 1:
                NoiseControlMode = frame.Payload[0] == 1 ? NoiseControlMode.Transparency : NoiseControlMode.Off;
                NoiseControlModeChanged?.Invoke(this, NoiseControlMode);
                break;
        }
    }

    /// <summary>Layout confirmed against the reference StatusUpdateDecoder (non-original-Buds branch): [0] revision, [1] batteryL, [2] batteryR, [3] isCoupled, [4] mainConnection, [5] placement, [6] batteryCase (if present), [7] charging bitfield (if present).</summary>
    private void ParseStatusUpdated(byte[] payload)
    {
        int? left = payload[1] <= 100 ? payload[1] : null;
        int? right = payload[2] <= 100 ? payload[2] : null;
        int? caseP = payload.Length > 6 && payload[6] <= 100 ? payload[6] : null;
        bool caseCharging = payload.Length > 7 && (payload[7] & 0x01) != 0;

        Battery = new BatteryStatus(left, right, caseP, caseCharging);
        BatteryChanged?.Invoke(this, Battery);
    }

    /// <summary>Layout per the older protocol notes doc (index shifted by one vs StatusUpdated): [0] versionOfMr, [1] earType, [2] batteryL, [3] batteryR. Case/charging bytes for this variant were not confirmed against source, so are left unset here.</summary>
    private void ParseExtendedStatusUpdated(byte[] payload)
    {
        int? left = payload[2] <= 100 ? payload[2] : null;
        int? right = payload[3] <= 100 ? payload[3] : null;

        Battery = Battery with { LeftPercent = left, RightPercent = right };
        BatteryChanged?.Invoke(this, Battery);
    }

    private Task AcknowledgeAndIdentifyAsync(SppFrame received)
    {
        if (_transport is null)
        {
            return Task.CompletedTask;
        }

        byte[] ack = SppFrameCodec.Encode(SppMsgType.Response, received.MessageId, received.Payload);
        return _transport.WriteAsync(ack).ContinueWith(_ => SendManagerInfoAsync());
    }

    private Task SendManagerInfoAsync(CancellationToken ct = default)
    {
        if (_transport is null)
        {
            return Task.CompletedTask;
        }

        // [0] fixed=1, [1] IsSamsungDevice (1=Samsung, 2=Other - we're not an Android app), [2] Android SDK placeholder.
        byte[] frame = SppFrameCodec.Encode(SppMsgType.Request, SamsungMessageId.ManagerInfo, [0x01, 0x02, 30]);
        return _transport.WriteAsync(frame, ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_transport is not null)
        {
            await _transport.DisposeAsync();
        }
    }
}
