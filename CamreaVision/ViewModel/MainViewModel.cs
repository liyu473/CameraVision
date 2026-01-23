using System.Collections.ObjectModel;
using CamreaVision.Models;
using CamreaVision.Service;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LyuWpfHelper.ViewModels;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace CamreaVision.ViewModel;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly ICameraService _cameraService;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCameraCommand))]
    public partial bool IsInitialSDK { get; set; }

    public ObservableCollection<CameraInfo> Cameras { get; } = [];

    public MainViewModel(ICameraService cameraService, ILogger<MainViewModel> logger)
    {
        _cameraService = cameraService;
        _logger = logger;

        IsInitialSDK = _cameraService.InitializeSdk();    
    }


    #region MindVision

    [RelayCommand(CanExecute = (nameof(IsInitialSDK)))]
    private void ScanCamera()
    {
        _cameraService.EnumerateDevices().ForEach(c => Cameras.Add(c));
    }


    #endregion
}
