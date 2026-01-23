using System.Collections.ObjectModel;
using CamreaVision.Models;
using CamreaVision.Service;
using LyuWpfHelper.ViewModels;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace CamreaVision.ViewModel;

public partial class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly ICameraService _cameraService;

    public ObservableCollection<CameraInfo> Cameras { get; } = [];

    public MainViewModel(ICameraService cameraService, ILogger<MainViewModel> logger)
    {
        _cameraService = cameraService;
        _logger = logger;

        InitialCamera();       
    }

    private void InitialCamera()
    {
        if (_cameraService.InitializeSdk())
        {
            _logger.ZLogError($"初始化相机失败！");
            return;
        }

        _cameraService.EnumerateDevices().ForEach(c => Cameras.Add(c));
    }
}
