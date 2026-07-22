namespace BudsControl.Core.Models;

/// <summary>Ambient sound / active noise control state supported by both earbud families.</summary>
public enum NoiseControlMode
{
    Off,
    NoiseCancelling,
    Transparency,
    Adaptive,
}
