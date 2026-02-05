using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LyuCameraVision.Helpers;
using LyuCameraVision.Service;
using LyuExtensions.Aspects;
using LyuWpfHelper.Helpers;
using LyuWpfHelper.ViewModels;
using Microsoft.Extensions.Logging;
using MvCameraControl;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZLogger;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace CameraVision.ViewModel;

/// <summary>
/// 海康相机视图模型
/// 负责相机控制、参数设置、图像采集和显示的逻辑处理
/// </summary>
[Singleton]
public partial class HIK_MvCu060_ViewModel : ViewModelBase, IDisposable
{
    #region 私有字段

    /// <summary>
    /// 相机服务实例
    /// </summary>
    [Inject]
    private readonly HIK_MvCu060_CameraService _cameraService;

    /// <summary>
    /// 日志记录器
    /// </summary>
    [Inject]
    private readonly ILogger<HIK_MvCu060_ViewModel> _logger;

    /// <summary>
    /// 图像采集线程
    /// </summary>
    private Thread? _grabThread;

    /// <summary>
    /// 用于在UI线程上更新控件的调度器
    /// </summary>
    private readonly Dispatcher _dispatcher;

    /// <summary>
    /// 是否已释放资源
    /// </summary>
    private bool _disposed = false;

    #endregion

    #region 可观察属性 - 图像显示

    /// <summary>
    /// 当前显示的图像
    /// 绑定到 WPF Image 控件的 Source 属性
    /// </summary>
    [ObservableProperty]
    public partial BitmapSource? CurrentImage { get; set; }

    #endregion

    #region 可观察属性 - 设备相关

    /// <summary>
    /// 设备列表
    /// </summary>
    [ObservableProperty]
    public partial ObservableCollection<IDeviceInfo> Devices { get; set; } = [];

    /// <summary>
    /// 当前选中的设备
    /// </summary>
    [ObservableProperty]
    public partial IDeviceInfo? SelectedDevice { get; set; }

    #endregion

    #region 可观察属性 - 相机参数

    /// <summary>
    /// 曝光时间（微秒）
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetParametersCommand))]
    public partial float ExposureTime { get; set; }

    /// <summary>
    /// 曝光时间最小值
    /// </summary>
    [ObservableProperty]
    public partial float ExposureTimeMin { get; set; }

    /// <summary>
    /// 曝光时间最大值
    /// </summary>
    [ObservableProperty]
    public partial float ExposureTimeMax { get; set; } = 1000000;

    /// <summary>
    /// 增益值
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetParametersCommand))]
    public partial float Gain { get; set; }

    /// <summary>
    /// 增益最小值
    /// </summary>
    [ObservableProperty]
    public partial float GainMin { get; set; }

    /// <summary>
    /// 增益最大值
    /// </summary>
    [ObservableProperty]
    public partial float GainMax { get; set; } = 20;

    /// <summary>
    /// 帧率
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SetParametersCommand))]
    public partial float FrameRate { get; set; }

    /// <summary>
    /// 帧率最小值
    /// </summary>
    [ObservableProperty]
    public partial float FrameRateMin { get; set; }

    /// <summary>
    /// 帧率最大值
    /// </summary>
    [ObservableProperty]
    public partial float FrameRateMax { get; set; } = 100;

    #endregion

    #region 参数验证

    /// <summary>
    /// 曝光时间改变时的验证
    /// </summary>
    partial void OnExposureTimeChanged(float value)
    {
        if (value < ExposureTimeMin)
        {
            ExposureTime = ExposureTimeMin;
            StatusText = $"曝光时间不能小于 {ExposureTimeMin} μs，已自动修正";
        }
        else if (value > ExposureTimeMax)
        {
            ExposureTime = ExposureTimeMax;
            StatusText = $"曝光时间不能大于 {ExposureTimeMax} μs，已自动修正";
        }
    }

    /// <summary>
    /// 增益改变时的验证
    /// </summary>
    partial void OnGainChanged(float value)
    {
        if (value < GainMin)
        {
            Gain = GainMin;
            StatusText = $"增益不能小于 {GainMin} dB，已自动修正";
        }
        else if (value > GainMax)
        {
            Gain = GainMax;
            StatusText = $"增益不能大于 {GainMax} dB，已自动修正";
        }
    }

    /// <summary>
    /// 帧率改变时的验证
    /// </summary>
    partial void OnFrameRateChanged(float value)
    {
        if (value < FrameRateMin)
        {
            FrameRate = FrameRateMin;
            StatusText = $"帧率不能小于 {FrameRateMin} fps，已自动修正";
        }
        else if (value > FrameRateMax)
        {
            FrameRate = FrameRateMax;
            StatusText = $"帧率不能大于 {FrameRateMax} fps，已自动修正";
        }
    }

    #endregion

    #region 可观察属性 - 状态标志

    /// <summary>
    /// 相机是否已打开
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenCameraCommand))]
    [NotifyCanExecuteChangedFor(nameof(CloseCameraCommand))]
    [NotifyCanExecuteChangedFor(nameof(StartGrabCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopGrabCommand))]
    [NotifyCanExecuteChangedFor(nameof(GetParametersCommand))]
    [NotifyCanExecuteChangedFor(nameof(SetParametersCommand))]
    [NotifyCanExecuteChangedFor(nameof(SoftwareTriggerCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveImageCommand))]
    public partial bool IsCameraOpened { get; set; }

    /// <summary>
    /// 是否正在采集
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartGrabCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopGrabCommand))]
    [NotifyCanExecuteChangedFor(nameof(SoftwareTriggerCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveImageCommand))]
    public partial bool IsGrabbing { get; set; }

    /// <summary>
    /// 是否启用触发模式
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SoftwareTriggerCommand))]
    public partial bool IsTriggerMode { get; set; }

    /// <summary>
    /// 是否为软触发
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SoftwareTriggerCommand))]
    public partial bool IsSoftwareTrigger { get; set; } = true;

    /// <summary>
    /// 状态栏显示文本
    /// </summary>
    [ObservableProperty]
    public partial string StatusText { get; set; } = "就绪";

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数，初始化视图模型
    /// </summary>
    /// <param name="service">相机服务实例</param>
    /// <param name="logger">日志记录器</param>
    public HIK_MvCu060_ViewModel(
    )
    {
        _dispatcher = Dispatcher.CurrentDispatcher;

        // 初始化时扫描设备
        ScanDevices();
    }

    #endregion

    #region 命令 - 设备管理

    /// <summary>
    /// 扫描设备命令
    /// </summary>
    [RelayCommand]
    private void ScanDevices()
    {
        try
        {
            var deviceList = _cameraService.EnumerateDevices();
            Devices = new ObservableCollection<IDeviceInfo>(deviceList);
            SelectedDevice = Devices.FirstOrDefault();
            StatusText = $"扫描到 {Devices.Count} 个设备";
            OpenCameraCommand.NotifyCanExecuteChanged();
            _logger.ZLogInformation($"扫描到 {Devices.Count} 个设备");
        }
        catch (Exception ex)
        {
            StatusText = "扫描设备失败";
            _logger.ZLogError($"扫描设备失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 打开相机命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenCamera))]
    private void OpenCamera()
    {
        if (SelectedDevice is null)
        {
            MessageBox.Show(App.Current.MainWindow, "请先选择相机设备", "提示");
            return;
        }

        if (_cameraService.OpenCamera(SelectedDevice))
        {
            IsCameraOpened = true;
            StatusText = $"相机已打开: {SelectedDevice.ModelName}";

            // 打开相机后获取参数
            GetParameters();
        }
        else
        {
            MessageBox.Show(App.Current.MainWindow, "打开相机失败", "错误");
            CloseCamera();
        }
    }

    /// <summary>
    /// 判断是否可以打开相机
    /// </summary>
    private bool CanOpenCamera() => !IsCameraOpened && SelectedDevice != null;

    /// <summary>
    /// 关闭相机命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCloseCamera))]
    private void CloseCamera()
    {
        try
        {
            // 如果正在采集，先停止
            if (IsGrabbing)
            {
                StopGrab();
            }

            if (_cameraService.CloseCamera())
            {
                _cameraService.DisposeCamera();
                IsCameraOpened = false;
                StatusText = "相机已关闭";
            }
            else
            {
                MessageBox.Show(App.Current.MainWindow, "关闭相机失败", "错误");
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"关闭相机异常: {ex.Message}");
            MessageBox.Show(App.Current.MainWindow, $"关闭相机异常: {ex.Message}", "错误");
        }
    }

    /// <summary>
    /// 判断是否可以关闭相机
    /// </summary>
    private bool CanCloseCamera() => IsCameraOpened;

    #endregion

    #region 命令 - 采集控制

    /// <summary>
    /// 开始采集命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartGrab))]
    private void StartGrab()
    {
        try
        {
            // 设置采集标志
            IsGrabbing = true;

            // 创建并启动采集线程
            _grabThread = new Thread(GrabThreadProcess)
            {
                IsBackground = true,
                Name = "HIK_GrabThread"
            };
            _grabThread.Start();

            // 开始相机采集
            if (!_cameraService.StartGrabbing())
            {
                IsGrabbing = false;
                _grabThread?.Join(1000);
                MessageBox.Show(App.Current.MainWindow, "开始采集失败", "错误");
                return;
            }

            StatusText = "正在采集...";
        }
        catch (Exception ex)
        {
            IsGrabbing = false;
            _logger.ZLogError($"开始采集异常: {ex.Message}");
            MessageBox.Show(App.Current.MainWindow, $"开始采集异常: {ex.Message}", "错误");
        }
    }

    /// <summary>
    /// 判断是否可以开始采集
    /// </summary>
    private bool CanStartGrab() => IsCameraOpened && !IsGrabbing;

    /// <summary>
    /// 停止采集命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStopGrab))]
    private void StopGrab()
    {
        try
        {
            // 设置采集标志为false，通知采集线程退出
            IsGrabbing = false;

            // 等待采集线程结束
            _grabThread?.Join(2000);

            // 停止相机采集
            _cameraService.StopGrabbing();

            StatusText = "采集已停止";
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"停止采集异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 判断是否可以停止采集
    /// </summary>
    private bool CanStopGrab() => IsCameraOpened && IsGrabbing;

    /// <summary>
    /// 图像采集线程处理函数
    /// 在单独的线程中循环获取图像并转换为 BitmapSource 显示
    /// </summary>
    private void GrabThreadProcess()
    {
        _logger.ZLogInformation($"采集线程启动");

        while (IsGrabbing)
        {
            try
            {
                // 获取一帧图像，超时时间为1000毫秒
                if (_cameraService.GetImageBuffer(1000, out IFrameOut? frameOut))
                {
                    if (frameOut != null)
                    {
                        try
                        {
                            // 将 IImage 转换为 BitmapSource
                            var bitmapSource = frameOut.Image.ToBmpBytes()?.ToBitmapSource();

                            if (bitmapSource != null)
                            {
                                // 在 UI 线程上更新图像
                                _dispatcher.Invoke(() =>
                                {
                                    CurrentImage = bitmapSource;
                                });
                            }
                        }
                        finally
                        {
                            // 释放帧缓冲区，防止内存泄漏
                            _cameraService.FreeImageBuffer(frameOut);
                        }
                    }
                }
                else
                {
                    // 如果是触发模式，短暂睡眠以降低CPU占用
                    if (IsTriggerMode)
                    {
                        Thread.Sleep(5);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ZLogError($"采集线程异常: {ex.Message}");
                // 出现异常时短暂睡眠，避免频繁报错
                Thread.Sleep(10);
            }
        }

        _logger.ZLogInformation($"采集线程退出");
    }

    #endregion

    #region 命令 - 触发控制

    /// <summary>
    /// 触发模式改变时的处理
    /// </summary>
    partial void OnIsTriggerModeChanged(bool value)
    {
        if (!IsCameraOpened) return;

        _cameraService.SetTriggerMode(value);

        if (value && IsSoftwareTrigger)
        {
            _cameraService.SetTriggerSource(true);
        }

        StatusText = value ? "触发模式已启用" : "连续采集模式";
    }

    /// <summary>
    /// 软触发模式改变时的处理
    /// </summary>
    partial void OnIsSoftwareTriggerChanged(bool value)
    {
        if (!IsCameraOpened || !IsTriggerMode) return;

        _cameraService.SetTriggerSource(value);
        StatusText = value ? "软触发模式" : "硬触发模式(Line0)";
    }

    /// <summary>
    /// 执行软触发命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSoftwareTrigger))]
    private void SoftwareTrigger()
    {
        if (_cameraService.TriggerSoftware())
        {
            StatusText = "软触发执行成功";
        }
        else
        {
            MessageBox.Show(App.Current.MainWindow, "软触发失败", "错误");
        }
    }

    /// <summary>
    /// 判断是否可以执行软触发
    /// </summary>
    private bool CanSoftwareTrigger() => IsCameraOpened && IsGrabbing && IsTriggerMode && IsSoftwareTrigger;

    #endregion

    #region 命令 - 参数管理

    /// <summary>
    /// 获取相机参数命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanGetParameters))]
    private void GetParameters()
    {
        try
        {
            // 获取曝光时间
            if (_cameraService.GetExposureTime(out float exposure))
            {
                ExposureTime = exposure;
            }

            // 获取曝光时间范围
            if (_cameraService.GetExposureTimeRange(out float expMin, out float expMax))
            {
                ExposureTimeMin = expMin;
                ExposureTimeMax = expMax;
            }

            // 获取增益
            if (_cameraService.GetGain(out float gain))
            {
                Gain = gain;
            }

            // 获取增益范围
            if (_cameraService.GetGainRange(out float gainMin, out float gainMax))
            {
                GainMin = gainMin;
                GainMax = gainMax;
            }

            // 获取帧率
            if (_cameraService.GetFrameRate(out float frameRate))
            {
                FrameRate = frameRate;
            }

            // 获取帧率范围
            if (_cameraService.GetFrameRateRange(out float frMin, out float frMax))
            {
                FrameRateMin = frMin;
                FrameRateMax = frMax;
            }

            StatusText = "参数获取成功";
            _logger.ZLogInformation($"参数获取成功: 曝光={ExposureTime}, 增益={Gain}, 帧率={FrameRate}");
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"获取参数异常: {ex.Message}");
            StatusText = "参数获取失败";
        }
    }

    /// <summary>
    /// 判断是否可以获取参数
    /// </summary>
    private bool CanGetParameters() => IsCameraOpened;

    /// <summary>
    /// 设置相机参数命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanSetParameters))]
    private void SetParameters()
    {
        try
        {
            bool success = true;

            // 设置曝光时间
            if (!_cameraService.SetExposureTime(ExposureTime))
            {
                success = false;
                _logger.ZLogWarning($"设置曝光时间失败");
            }

            // 设置增益
            if (!_cameraService.SetGain(Gain))
            {
                success = false;
                _logger.ZLogWarning($"设置增益失败");
            }

            // 设置帧率
            if (!_cameraService.SetFrameRate(FrameRate))
            {
                success = false;
                _logger.ZLogWarning($"设置帧率失败");
            }

            if (success)
            {
                StatusText = "参数设置成功";
                _logger.ZLogInformation($"参数设置成功: 曝光={ExposureTime}, 增益={Gain}, 帧率={FrameRate}");
            }
            else
            {
                StatusText = "部分参数设置失败";
                MessageBox.Show(App.Current.MainWindow, "部分参数设置失败，请检查参数范围", "提示");
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"设置参数异常: {ex.Message}");
            MessageBox.Show(App.Current.MainWindow, $"设置参数异常: {ex.Message}", "错误");
        }
    }

    /// <summary>
    /// 判断是否可以设置参数
    /// </summary>
    private bool CanSetParameters() => IsCameraOpened;

    #endregion

    #region 命令 - 图像保存

    /// <summary>
    /// 保存图像命令
    /// </summary>
    /// <param name="format">图像格式: bmp, jpg, png, tiff</param>
    [RelayCommand(CanExecute = nameof(CanSaveImage))]
    private void SaveImage(string? format = null)
    {
        try
        {
            bool success = format?.ToLower() switch
            {
                "bmp" => _cameraService.SaveImageAsBmp(),
                "jpg" or "jpeg" => _cameraService.SaveImageAsJpg(),
                "png" => _cameraService.SaveImageAsPng(),
                "tiff" or "tif" => _cameraService.SaveImageAsTiff(),
                _ => _cameraService.SaveImageAsBmp() // 默认保存为BMP
            };

            if (success)
            {
                StatusText = $"图像保存成功 ({format ?? "bmp"})";
            }
            else
            {
                MessageBox.Show(App.Current.MainWindow, "图像保存失败", "错误");
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"保存图像异常: {ex.Message}");
            MessageBox.Show(App.Current.MainWindow, $"保存图像异常: {ex.Message}", "错误");
        }
    }

    /// <summary>
    /// 判断是否可以保存图像
    /// </summary>
    private bool CanSaveImage() => IsCameraOpened && IsGrabbing;

    #endregion

    #region IDisposable实现

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 释放资源的内部实现
    /// </summary>
    /// <param name="disposing">是否释放托管资源</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // 停止采集
                if (IsGrabbing)
                {
                    IsGrabbing = false;
                    _grabThread?.Join(2000);
                    _cameraService.StopGrabbing();
                }

                // 关闭相机
                if (IsCameraOpened)
                {
                    _cameraService.CloseCamera();
                    _cameraService.DisposeCamera();
                }
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// 析构函数
    /// </summary>
    ~HIK_MvCu060_ViewModel()
    {
        Dispose(false);
    }

    #endregion
}
