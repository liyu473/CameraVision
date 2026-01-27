using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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
    private DispatcherTimer? _previewTimer;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ScanCameraCommand))]
    public partial bool IsInitialSDK { get; set; }

    public ObservableCollection<CameraInfo> Cameras { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSelectedCamera))]
    [NotifyCanExecuteChangedFor(nameof(OpenCameraCommand))]
    public partial CameraInfo? SelectedCamera { get; set; }

    public bool IsSelectedCamera => SelectedCamera != null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CloseCameraCommand))]
    [NotifyCanExecuteChangedFor(nameof(CaptureCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenSettingCommand))]
    public partial bool IsCameraOpened { get; set; }

    partial void OnIsCameraOpenedChanged(bool value)
    {
        if (!value)
        {
            IsPreview = false;
        }
    }

    [ObservableProperty]
    public partial BitmapSource? PreviewImage { get; set; }

    [ObservableProperty]
    public partial BitmapSource? CapturedImage { get; set; }

    /// <summary>
    /// 是否开启采集
    /// </summary>
    [ObservableProperty]
    public partial bool IsPreview { get; set; }

    partial void OnIsPreviewChanged(bool value)
    {
        if (value)
        {
            StartPreview();
        }
        else
        {
            StopPreview();
        }
    }

    public MainViewModel(ICameraService cameraService, ILogger<MainViewModel> logger)
    {
        _cameraService = cameraService;
        _logger = logger;

        IsInitialSDK = _cameraService.InitializeSdk();
    }

    #region MindVision

    [RelayCommand(CanExecute = (nameof(IsCameraOpened)))]
    private void OpenSetting()
    {
        _cameraService.OpenSettingPage();
    }

    [RelayCommand(CanExecute = (nameof(IsInitialSDK)))]
    private void ScanCamera()
    {
        Cameras.Clear();
        _cameraService.EnumerateDevices().ForEach(c => Cameras.Add(c));
    }

    [RelayCommand(CanExecute = (nameof(IsSelectedCamera)))]
    private void OpenCamera()
    {
        if (_cameraService.OpenCamera(SelectedCamera!.DeviceIndex))
        {
            _cameraService.StartCapture();
            IsCameraOpened = true;
            IsPreview = true;
        }
    }

    [RelayCommand(CanExecute = (nameof(IsCameraOpened)))]
    private void CloseCamera()
    {
        StopPreview();
        _cameraService.StopCapture();
        _cameraService.CloseCamera();
        IsCameraOpened = false;
        PreviewImage = null;
    }

    [RelayCommand(CanExecute = (nameof(IsCameraOpened)))]
    private void Capture()
    {
        _logger.ZLogInformation($"开始捕捉图像");
        var frame = _cameraService.GetFrame(500);
        if (frame?.BitmapSource != null)
        {
            CapturedImage = frame.BitmapSource;
            _logger.ZLogInformation($"捕捉图像完成");
        }
    }

    private void StartPreview()
    {
        _previewTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33), // ~30fps
        };
        _previewTimer.Tick += (s, e) =>
        {
            var frame = _cameraService.GetFrame(100);
            if (frame?.BitmapSource != null)
            {
                PreviewImage = frame.BitmapSource;
            }
        };
        _previewTimer.Start();
    }

    private void StopPreview()
    {
        _previewTimer?.Stop();
        _previewTimer = null;
    }

    #endregion
}
