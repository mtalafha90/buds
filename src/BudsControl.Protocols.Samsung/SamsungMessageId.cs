namespace BudsControl.Protocols.Samsung;

/// <summary>
/// Message IDs for the Galaxy Buds RFCOMM/SPP control protocol.
///
/// *** UNVERIFIED - DO NOT TRUST THESE HEX VALUES WITHOUT CHECKING FIRST ***
/// This app could not reach a live packet capture or a confirmed copy of the upstream
/// GalaxyBudsClient source from its build sandbox, so these values are placeholders copied from
/// general public write-ups on the reverse-engineered protocol. Samsung has also changed message
/// IDs between buds generations (Buds / Buds+ / Buds Live / Buds Pro / Buds 2 / Buds 2 Pro / Buds
/// FE all differ somewhat).
///
/// Before relying on this for real control, verify against:
///   - https://github.com/ThePBone/GalaxyBudsClient (search for "MsgIds" / "SPPMessage" in
///     GalaxyBudsClient.Platform) - the most complete open-source reference.
///   - A Wireshark/btmon capture of the official Samsung Wearable app talking to your own buds.
/// </summary>
public enum SamsungMessageId : byte
{
    RequestStatusUpdated = 0x81,
    StatusUpdated = 0x82,
    SetNoiseControlMode = 0x84,
    NoiseControlModeUpdated = 0x85,
    BatteryStatusUpdated = 0x8A,
}
