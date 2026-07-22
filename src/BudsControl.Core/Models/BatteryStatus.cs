namespace BudsControl.Core.Models;

/// <summary>Battery percentages, 0-100. Null means "not reported" (e.g. the case is out of range).</summary>
public sealed record BatteryStatus(int? LeftPercent, int? RightPercent, int? CasePercent, bool CaseCharging = false)
{
    public static readonly BatteryStatus Unknown = new(null, null, null);
}
