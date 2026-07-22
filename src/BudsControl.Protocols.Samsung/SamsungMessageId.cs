namespace BudsControl.Protocols.Samsung;

/// <summary>
/// Message IDs for the Galaxy Buds RFCOMM/SPP control protocol ("legacy" framing: SOF 0xFE / EOF
/// 0xEE - see MsgConstants in the reference source, which also defines newer 0xFD/0xDD and
/// 0xFC/0xCC framings this app does not implement).
///
/// Unlike the first version of this file, these numeric values are NOT guesses - they were
/// fetched directly from the current source of the reference open-source client
/// (https://github.com/ThePBone/GalaxyBudsClient, GalaxyBudsClient/Message/SppMessageEnums.cs,
/// enum MsgIds), so confidence in the IDs themselves is high. What's still genuinely unverified:
///   - The exact numeric mapping of NoiseControlModes (the payload byte for NoiseControls /
///     NoiseControlsUpdate) - this app guesses 0=Off, 1=NoiseCancelling, 2=Transparency.
///   - Which command set your specific buds model actually implements - some models (especially
///     budget ones without true ANC hardware) may only support the boolean SetAmbientMode /
///     AmbientModeUpdated pair rather than the 3-way NoiseControls selector.
/// </summary>
public enum SamsungMessageId : byte
{
    /// <summary>Generic acknowledgement sent by the device in reply to most messages. Payload: [0] original message ID, [1] result code (0 = success).</summary>
    Resp = 81,

    /// <summary>Pushed unsolicited by the device shortly after the RFCOMM connection opens. Payload layout varies by device generation - see SamsungBudsDevice's parsing.</summary>
    StatusUpdated = 96,

    /// <summary>Richer variant of StatusUpdated some models push instead/also - includes ambient sound, EQ and touch state alongside battery.</summary>
    ExtendedStatusUpdated = 97,

    /// <summary>Send after acknowledging a status update, to identify the connecting app. Payload: [0]=1 (fixed), [1] IsSamsungDevice (1=Samsung, 2=Other), [2] Android SDK version (arbitrary placeholder for a non-Android client).</summary>
    ManagerInfo = 136,

    /// <summary>3-way noise control select (Off/ANC/Ambient) - send to change mode on models with true ANC hardware.</summary>
    NoiseControls = 120,

    /// <summary>Pushed by the device after a NoiseControls change (or unsolicited on state change). Payload: [0] mode.</summary>
    NoiseControlsUpdate = 119,

    /// <summary>Simple boolean ambient-sound passthrough toggle - send to change mode on models without a 3-way ANC selector.</summary>
    SetAmbientMode = 128,

    /// <summary>Pushed by the device after a SetAmbientMode change. Payload: [0] 0 or 1.</summary>
    AmbientModeUpdated = 129,

    /// <summary>Send to request battery chemistry/type strings - NOT the battery percentage, which arrives via StatusUpdated/ExtendedStatusUpdated instead.</summary>
    BatteryType = 148,
}
