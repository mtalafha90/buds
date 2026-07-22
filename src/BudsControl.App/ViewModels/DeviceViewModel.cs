using BudsControl.Core.Devices;
using BudsControl.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BudsControl.App.ViewModels;

public sealed partial class DeviceViewModel : ObservableObject
{
    private readonly IEarbudsDevice _device;

    public string Name => _device.Name;
    public string Address => _device.Address;
    public EarbudsKind Kind => _device.Kind;
    public IReadOnlyList<NoiseControlMode> SupportedModes => _device.SupportedNoiseControlModes;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private BatteryStatus _battery = BatteryStatus.Unknown;

    [ObservableProperty]
    private NoiseControlMode _noiseControlMode = NoiseControlMode.Off;

    public DeviceViewModel(IEarbudsDevice device)
    {
        _device = device;
        _device.BatteryChanged += (_, b) => Battery = b;
        _device.NoiseControlModeChanged += (_, m) => NoiseControlMode = m;
        _device.Disconnected += (_, _) =>
        {
            IsConnected = false;
            StatusMessage = "Disconnected.";
        };
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (IsConnected || IsConnecting)
        {
            return;
        }

        IsConnecting = true;
        StatusMessage = "Connecting...";
        try
        {
            await _device.ConnectAsync();
            IsConnected = true;
            StatusMessage = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connect failed: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task DisconnectAsync()
    {
        await _device.DisconnectAsync();
        IsConnected = false;
    }

    [RelayCommand]
    private async Task SetModeAsync(NoiseControlMode mode)
    {
        if (!IsConnected)
        {
            return;
        }

        try
        {
            await _device.SetNoiseControlModeAsync(mode);
            NoiseControlMode = mode;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to set mode: {ex.Message}";
        }
    }
}
