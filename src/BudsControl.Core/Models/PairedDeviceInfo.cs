namespace BudsControl.Core.Models;

/// <summary>A Bluetooth device already paired at the OS level, as reported by the platform's Bluetooth stack.</summary>
public sealed record PairedDeviceInfo(string Name, string Address, EarbudsKind Kind);
