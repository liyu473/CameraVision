using CamreaVision.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LyuWpfHelper.ViewModels;
using Microsoft.Extensions.Logging;
using MvCameraControl;
using System.Collections.ObjectModel;

namespace CamreaVision.ViewModel;

public partial class HIK_MvCu060_ViewModel : ViewModelBase
{
    private readonly HIK_MvCu060_CameraService _cameraService;
    private readonly ILogger<HIK_MvCu060_ViewModel> _logger;
    public HIK_MvCu060_ViewModel(HIK_MvCu060_CameraService service,ILogger<HIK_MvCu060_ViewModel> logger)
    {
        _cameraService = service;
        _logger = logger;

        ScanDevices();
    }

    [ObservableProperty]
    public partial ObservableCollection<IDeviceInfo> Devices { get; set; } = [];

    [ObservableProperty]
    public partial IDeviceInfo? SelectedDevice { get; set; }

    [RelayCommand]
    private void ScanDevices()
    {
        var deviceList = _cameraService.EnumerateDevices();
        Devices = new ObservableCollection<IDeviceInfo>(deviceList);
        SelectedDevice = Devices.FirstOrDefault();
    }
}
