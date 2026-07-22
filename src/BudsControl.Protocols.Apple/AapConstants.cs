namespace BudsControl.Protocols.Apple;

/// <summary>
/// Constants for Apple's proprietary "AAP" (Apple Accessory Protocol) used to control AirPods
/// noise control mode over Bluetooth L2CAP, once the AirPods are already paired at the OS level.
///
/// *** UNVERIFIED - DO NOT TRUST THESE VALUES WITHOUT CHECKING FIRST ***
/// Apple has never published this protocol. Everything here is a commonly-cited value from public
/// reverse-engineering write-ups (the OpenPods and AirPodsDesktop projects, and blog posts
/// describing captures against real AirPods), which this app's build sandbox could not
/// independently re-verify against a live capture or a confirmed upstream source. Firmware
/// updates can also change details of this protocol over time.
///
/// Before relying on this for real control, verify against:
///   - https://github.com/kavishdevar/librepods (linux/ and android/ directories) - the most
///     actively maintained open-source client, with a companion Wireshark dissector referenced
///     from its README (search "apple-wireshark").
///   - A Wireshark/btsnoop capture of your iPhone/Mac talking to your own AirPods.
/// </summary>
public static class AapConstants
{
    /// <summary>Fixed L2CAP PSM AAP is commonly reported to use - not SDP-discovered.</summary>
    public const int Psm = 0x1001;
}
