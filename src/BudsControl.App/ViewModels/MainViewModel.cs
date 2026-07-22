using System.Collections.ObjectModel;
using BudsControl.App.Composition;
using BudsControl.Core.Devices;
using BudsControl.Core.Transport;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BudsControl.App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IPairedDeviceDiscovery _discovery;
    private readonly IEarbudsDeviceFactory _deviceFactory;

    public ObservableCollection<DeviceViewModel> Devices { get; } = [];

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string? _statusMessage;

    public MainViewModel() : this(PlatformServices.CreateDiscovery(), new EarbudsDeviceFactory(PlatformServices.CreateRfcommFactory(), PlatformServices.CreateL2CapFactory()))
    {
    }

    public MainViewModel(IPairedDeviceDiscovery discovery, IEarbudsDeviceFactory deviceFactory)
    {
        _discovery = discovery;
        _deviceFactory = deviceFactory;
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        IsScanning = true;
        StatusMessage = null;
        try
        {
            Devices.Clear();
            IReadOnlyList<Core.Models.PairedDeviceInfo> paired = await _discovery.GetPairedDevicesAsync();
            foreach (Core.Models.PairedDeviceInfo info in paired)
            {
                IEarbudsDevice? device = _deviceFactory.Create(info);
                if (device is not null)
                {
                    Devices.Add(new DeviceViewModel(device));
                }
            }

            if (Devices.Count == 0)
            {
                StatusMessage = "No paired Galaxy Buds or AirPods found. Pair them in your OS Bluetooth settings first, then scan again.";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }
}
