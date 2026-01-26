using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CamreaVision.Models;
using Microsoft.Extensions.Logging;
using MVSDK;
using ZLogger;

namespace CamreaVision.Service;

/// <summary>
/// 相机服务实现类
/// </summary>
public class CameraService : ICameraService, IDisposable
{
    private readonly ILogger<CameraService> _logger;
    private int _cameraHandle = -1;
    private IntPtr _grabber = IntPtr.Zero;
    private tSdkCameraDevInfo _currentDeviceInfo;
    private tSdkCameraDevInfo[]? _cachedDeviceList = null;
    private bool _isOpened = false;
    private bool _isCapturing = false;
    private CameraInfo? _currentCamera = null;
    private IntPtr _frameBuffer = IntPtr.Zero;
    
    // 保存回调委托引用，防止被GC回收导致闪退
    private pfnCameraGrabberFrameCallback? _frameCallback;

    public bool IsOpened => _isOpened;
    public bool IsCapturing => _isCapturing;
    public CameraInfo? CurrentCamera => _currentCamera;

    public event EventHandler<CameraFrame>? FrameReceived;

    public CameraService(ILogger<CameraService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 初始化SDK
    /// </summary>
    public bool InitializeSdk()
    {
        try
        {
            _logger.ZLogInformation($"准备初始化SDK...");                   
            _logger.ZLogInformation($"准备调用CameraSdkInit...");
            var status = MvApi.CameraSdkInit(1);
            
            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                _logger.ZLogInformation($"相机SDK初始化成功");
                return true;
            }
            else
            {
                _logger.ZLogError($"相机SDK初始化失败: {status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"初始化SDK时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 枚举所有可用的相机设备
    /// </summary>
    public List<CameraInfo> EnumerateDevices()
    {
        var devices = new List<CameraInfo>();
        try
        {
            tSdkCameraDevInfo[]? deviceList;
            var status = MvApi.CameraEnumerateDevice(out deviceList);

            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS && deviceList != null)
            {
                _cachedDeviceList = deviceList; // 缓存设备列表
                _logger.ZLogInformation($"找到 {deviceList.Length} 个相机设备");
                for (int i = 0; i < deviceList.Length; i++)
                {
                    var device = deviceList[i];
                    devices.Add(new CameraInfo
                    {
                        ProductSeries = Encoding.UTF8.GetString(device.acProductSeries).TrimEnd('\0'),
                        ProductName = Encoding.UTF8.GetString(device.acProductName).TrimEnd('\0'),
                        FriendlyName = Encoding.UTF8.GetString(device.acFriendlyName).TrimEnd('\0'),
                        LinkName = Encoding.UTF8.GetString(device.acLinkName).TrimEnd('\0'),
                        DriverVersion = Encoding.UTF8.GetString(device.acDriverVersion).TrimEnd('\0'),
                        SensorType = Encoding.UTF8.GetString(device.acSensorType).TrimEnd('\0'),
                        PortType = Encoding.UTF8.GetString(device.acPortType).TrimEnd('\0'),
                        SerialNumber = Encoding.UTF8.GetString(device.acSn).TrimEnd('\0'),
                        Instance = device.uInstance,
                        DeviceIndex = i
                    });
                }
            }
            else
            {
                _logger.ZLogWarning($"枚举设备失败: {status}");
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"枚举设备时发生异常");
        }
        return devices;
    }

    /// <summary>
    /// 打开指定的相机 - 使用CameraInit底层API（.NET 10兼容）
    /// </summary>
    public bool OpenCamera(int deviceIndex)
    {
        try
        {
            _logger.ZLogInformation($"开始打开相机，设备索引: {deviceIndex}");

            if (_isOpened)
            {
                _logger.ZLogWarning($"相机已经打开，请先关闭");
                return false;
            }

            if (_cachedDeviceList == null || _cachedDeviceList.Length == 0)
            {
                _logger.ZLogError($"设备列表为空，请先调用EnumerateDevices");
                return false;
            }

            if (deviceIndex >= _cachedDeviceList.Length)
            {
                _logger.ZLogError($"无效的设备索引: {deviceIndex}");
                return false;
            }

            _currentDeviceInfo = _cachedDeviceList[deviceIndex];
            _logger.ZLogInformation($"准备初始化相机: {Encoding.UTF8.GetString(_currentDeviceInfo.acFriendlyName).TrimEnd('\0')}");

            // 使用CameraInit（CameraGrabber_Create在.NET 10下会卡死）
            _logger.ZLogInformation($"调用CameraInit...");
            CameraSdkStatus status = MvApi.CameraInit(ref _currentDeviceInfo, -1, -1, ref _cameraHandle);
            _logger.ZLogInformation($"CameraInit返回: {status}, 句柄: {_cameraHandle}");
            
            if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                _logger.ZLogError($"CameraInit失败: {status}");
                return false;
            }

            // 获取相机能力描述
            tSdkCameraCapbility capability;
            status = MvApi.CameraGetCapability(_cameraHandle, out capability);
            if (status != CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                _logger.ZLogError($"获取相机能力失败: {status}");
                MvApi.CameraUnInit(_cameraHandle);
                _cameraHandle = -1;
                return false;
            }

            // 分配RGB缓冲区
            int bufferSize = capability.sResolutionRange.iWidthMax * capability.sResolutionRange.iHeightMax * 3;
            _logger.ZLogInformation($"分配缓冲区大小: {bufferSize} 字节");
            _frameBuffer = Marshal.AllocHGlobal(bufferSize);

            _isOpened = true;
            _currentCamera = new CameraInfo
            {
                ProductSeries = Encoding.UTF8.GetString(_currentDeviceInfo.acProductSeries).TrimEnd('\0'),
                ProductName = Encoding.UTF8.GetString(_currentDeviceInfo.acProductName).TrimEnd('\0'),
                FriendlyName = Encoding.UTF8.GetString(_currentDeviceInfo.acFriendlyName).TrimEnd('\0'),
                LinkName = Encoding.UTF8.GetString(_currentDeviceInfo.acLinkName).TrimEnd('\0'),
                DriverVersion = Encoding.UTF8.GetString(_currentDeviceInfo.acDriverVersion).TrimEnd('\0'),
                SensorType = Encoding.UTF8.GetString(_currentDeviceInfo.acSensorType).TrimEnd('\0'),
                PortType = Encoding.UTF8.GetString(_currentDeviceInfo.acPortType).TrimEnd('\0'),
                SerialNumber = Encoding.UTF8.GetString(_currentDeviceInfo.acSn).TrimEnd('\0'),
                Instance = _currentDeviceInfo.uInstance,
                DeviceIndex = deviceIndex
            };

            _logger.ZLogInformation($"相机打开完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"打开相机时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 关闭当前相机
    /// </summary>
    public bool CloseCamera()
    {
        try
        {
            if (!_isOpened)
            {
                _logger.ZLogWarning($"相机未打开");
                return false;
            }

            if (_isCapturing)
            {
                StopCapture();
            }

            if (_grabber != IntPtr.Zero)
            {
                var status = MvApi.CameraGrabber_Destroy(_grabber);
                if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                {
                    _logger.ZLogInformation($"相机关闭成功");
                    
                    if (_frameBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(_frameBuffer);
                        _frameBuffer = IntPtr.Zero;
                    }

                    _isOpened = false;
                    _currentCamera = null;
                    _cameraHandle = -1;
                    _grabber = IntPtr.Zero;
                    return true;
                }
                else
                {
                    _logger.ZLogError($"相机关闭失败: {status}");
                    return false;
                }
            }
            else if (_cameraHandle != -1)
            {
                var status = MvApi.CameraUnInit(_cameraHandle);
                if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
                {
                    _logger.ZLogInformation($"相机关闭成功");
                    
                    if (_frameBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(_frameBuffer);
                        _frameBuffer = IntPtr.Zero;
                    }

                    _isOpened = false;
                    _currentCamera = null;
                    _cameraHandle = -1;
                    return true;
                }
                else
                {
                    _logger.ZLogError($"相机关闭失败: {status}");
                    return false;
                }
            }
            else
            {
                _logger.ZLogWarning($"相机未初始化");
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"关闭相机时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 开始采集图像
    /// </summary>
    public bool StartCapture()
    {
        try
        {
            if (!_isOpened)
            {
                _logger.ZLogWarning($"相机未打开");
                return false;
            }

            if (_isCapturing)
            {
                _logger.ZLogWarning($"相机已经在采集中");
                return true;
            }

            CameraSdkStatus status;
            if (_grabber != IntPtr.Zero)
            {
                // 使用Grabber API
                status = MvApi.CameraGrabber_StartLive(_grabber);
            }
            else
            {
                // 回退到基础API
                status = MvApi.CameraPlay(_cameraHandle);
            }

            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                _logger.ZLogInformation($"开始采集图像");
                _isCapturing = true;
                return true;
            }
            else
            {
                _logger.ZLogError($"开始采集失败: {status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"开始采集时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 停止采集图像
    /// </summary>
    public bool StopCapture()
    {
        try
        {
            if (!_isOpened)
            {
                _logger.ZLogWarning($"相机未打开");
                return false;
            }

            if (!_isCapturing)
            {
                _logger.ZLogWarning($"相机未在采集中");
                return true;
            }

            CameraSdkStatus status;
            if (_grabber != IntPtr.Zero)
            {
                // 使用Grabber API
                status = MvApi.CameraGrabber_StopLive(_grabber);
            }
            else
            {
                // 回退到基础API
                status = MvApi.CameraStop(_cameraHandle);
            }

            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                _logger.ZLogInformation($"停止采集图像");
                _isCapturing = false;
                return true;
            }
            else
            {
                _logger.ZLogError($"停止采集失败: {status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"停止采集时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 获取一帧图像
    /// </summary>
    public CameraFrame? GetFrame(int timeout = 1000)
    {
        try
        {
            if (!_isOpened)
            {
                _logger.ZLogWarning($"相机未打开");
                return null;
            }

            IntPtr pFrameBuffer;
            tSdkFrameHead frameHead;
            var status = MvApi.CameraGetImageBuffer(_cameraHandle, out frameHead, out pFrameBuffer, (uint)timeout);

            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                // 转换为RGB24格式
                MvApi.CameraImageProcess(_cameraHandle, pFrameBuffer, _frameBuffer, ref frameHead);

                // 创建帧数据
                int imageSize = frameHead.iWidth * frameHead.iHeight * 3;
                byte[] imageData = new byte[imageSize];
                Marshal.Copy(_frameBuffer, imageData, 0, imageSize);

                var frame = new CameraFrame
                {
                    ImageData = imageData,
                    Width = frameHead.iWidth,
                    Height = frameHead.iHeight,
                    MediaType = frameHead.uiMediaType,
                    FrameNumber = 0,
                    TimeStamp = frameHead.uiTimeStamp,
                    ExposureTime = frameHead.uiExpTime,
                    AnalogGain = (uint)frameHead.fAnalogGain,
                    CaptureTime = DateTime.Now,
                    // 创建BitmapSource用于WPF显示
                    BitmapSource = CreateBitmapSource(imageData, frameHead.iWidth, frameHead.iHeight)
                };

                // 释放缓冲区
                MvApi.CameraReleaseImageBuffer(_cameraHandle, pFrameBuffer);

                // 触发事件
                FrameReceived?.Invoke(this, frame);

                return frame;
            }
            else if (status == CameraSdkStatus.CAMERA_STATUS_TIME_OUT)
            {
                _logger.ZLogDebug($"获取图像超时");
                return null;
            }
            else
            {
                _logger.ZLogError($"获取图像失败: {status}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取帧时发生异常");
            return null;
        }
    }

    /// <summary>
    /// 软触发一次
    /// </summary>
    public bool SoftTrigger()
    {
        try
        {
            if (!_isOpened)
            {
                _logger.ZLogWarning($"相机未打开");
                return false;
            }

            var status = MvApi.CameraSoftTrigger(_cameraHandle);
            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                _logger.ZLogDebug($"软触发成功");
                return true;
            }
            else
            {
                _logger.ZLogError($"软触发失败: {status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"软触发时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 设置曝光时间
    /// </summary>
    public bool SetExposureTime(double exposureTime)
    {
        try
        {
            if (!_isOpened)
            {
                _logger.ZLogWarning($"相机未打开");
                return false;
            }

            var status = MvApi.CameraSetExposureTime(_cameraHandle, exposureTime);
            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                _logger.ZLogDebug($"设置曝光时间成功: {exposureTime}us");
                return true;
            }
            else
            {
                _logger.ZLogError($"设置曝光时间失败: {status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"设置曝光时间时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 获取曝光时间
    /// </summary>
    public double GetExposureTime()
    {
        try
        {
            if (!_isOpened)
            {
                _logger.ZLogWarning($"相机未打开");
                return 0;
            }

            double exposureTime = 0;
            var status = MvApi.CameraGetExposureTime(_cameraHandle, ref exposureTime);
            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                return exposureTime;
            }
            else
            {
                _logger.ZLogError($"获取曝光时间失败: {status}");
                return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取曝光时间时发生异常");
            return 0;
        }
    }

    /// <summary>
    /// 设置增益
    /// </summary>
    public bool SetGain(int gain)
    {
        try
        {
            if (!_isOpened)
            {
                _logger.ZLogWarning($"相机未打开");
                return false;
            }

            var status = MvApi.CameraSetAnalogGain(_cameraHandle, gain);
            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                _logger.ZLogDebug($"设置增益成功: {gain}");
                return true;
            }
            else
            {
                _logger.ZLogError($"设置增益失败: {status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"设置增益时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 获取增益
    /// </summary>
    public int GetGain()
    {
        try
        {
            if (!_isOpened)
            {
                _logger.ZLogWarning($"相机未打开");
                return 0;
            }

            int gain = 0;
            var status = MvApi.CameraGetAnalogGain(_cameraHandle, ref gain);
            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                return gain;
            }
            else
            {
                _logger.ZLogError($"获取增益失败: {status}");
                return 0;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取增益时发生异常");
            return 0;
        }
    }

    /// <summary>
    /// 设置分辨率
    /// </summary>
    public bool SetResolution(int width, int height)
    {
        try
        {
            if (!_isOpened)
            {
                _logger.ZLogWarning($"相机未打开");
                return false;
            }

            tSdkImageResolution resolution = new()
            {
                iIndex = 0xFF,
                iWidth = width,
                iHeight = height,
                iWidthFOV = width,
                iHeightFOV = height
            };

            var status = MvApi.CameraSetImageResolution(_cameraHandle, ref resolution);
            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                _logger.ZLogInformation($"设置分辨率成功: {width}x{height}");
                return true;
            }
            else
            {
                _logger.ZLogError($"设置分辨率失败: {status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"设置分辨率时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 获取当前分辨率
    /// </summary>
    public CameraResolution? GetResolution()
    {
        try
        {
            if (!_isOpened)
            {
                _logger.ZLogWarning($"相机未打开");
                return null;
            }

            tSdkImageResolution resolution;
            var status = MvApi.CameraGetImageResolution(_cameraHandle, out resolution);
            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                return new CameraResolution
                {
                    Index = resolution.iIndex,
                    Description = Encoding.UTF8.GetString(resolution.acDescription).TrimEnd('\0'),
                    Width = resolution.iWidth,
                    Height = resolution.iHeight,
                    HorizontalOffset = resolution.iHOffsetFOV,
                    VerticalOffset = resolution.iVOffsetFOV,
                    WidthFOV = resolution.iWidthFOV,
                    HeightFOV = resolution.iHeightFOV
                };
            }
            else
            {
                _logger.ZLogError($"获取分辨率失败: {status}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"获取分辨率时发生异常");
            return null;
        }
    }

    /// <summary>
    /// 设置触发模式
    /// </summary>
    public bool SetTriggerMode(int mode)
    {
        try
        {
            if (!_isOpened)
            {
                _logger.ZLogWarning($"相机未打开");
                return false;
            }

            var status = MvApi.CameraSetTriggerMode(_cameraHandle, mode);
            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                _logger.ZLogInformation($"设置触发模式成功: {mode}");
                return true;
            }
            else
            {
                _logger.ZLogError($"设置触发模式失败: {status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"设置触发模式时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 保存图像到文件
    /// </summary>
    public bool SaveImage(CameraFrame frame, string filePath, int quality = 100)
    {
        try
        {
            if (frame == null || frame.ImageData == null || frame.ImageData.Length == 0)
            {
                _logger.ZLogWarning($"帧数据为空");
                return false;
            }

            string extension = Path.GetExtension(filePath).ToLower();
            IntPtr imageBuffer = Marshal.AllocHGlobal(frame.ImageData.Length);
            Marshal.Copy(frame.ImageData, 0, imageBuffer, frame.ImageData.Length);

            CameraSdkStatus status;
            tSdkFrameHead frameHead = CreateFrameHead(frame);
            
            if (extension == ".bmp")
            {
                status = MvApi.CameraSaveImage(_cameraHandle, filePath, imageBuffer, ref frameHead, emSdkFileType.FILE_BMP, (byte)quality);
            }
            else if (extension == ".jpg" || extension == ".jpeg")
            {
                status = MvApi.CameraSaveImage(_cameraHandle, filePath, imageBuffer, ref frameHead, emSdkFileType.FILE_JPG, (byte)quality);
            }
            else if (extension == ".png")
            {
                status = MvApi.CameraSaveImage(_cameraHandle, filePath, imageBuffer, ref frameHead, emSdkFileType.FILE_PNG, (byte)quality);
            }
            else
            {
                Marshal.FreeHGlobal(imageBuffer);
                _logger.ZLogError($"不支持的文件格式: {extension}");
                return false;
            }

            Marshal.FreeHGlobal(imageBuffer);

            if (status == CameraSdkStatus.CAMERA_STATUS_SUCCESS)
            {
                _logger.ZLogInformation($"保存图像成功: {filePath}");
                return true;
            }
            else
            {
                _logger.ZLogError($"保存图像失败: {status}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"保存图像时发生异常");
            return false;
        }
    }

    /// <summary>
    /// 创建BitmapSource用于WPF显示
    /// </summary>
    private BitmapSource CreateBitmapSource(byte[] imageData, int width, int height)
    {
        try
        {
            var bitmap = BitmapSource.Create(
                width,
                height,
                96,
                96,
                PixelFormats.Bgr24,
                null,
                imageData,
                width * 3);

            bitmap.Freeze();
            return bitmap;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"创建BitmapSource时发生异常");
            return null!;
        }
    }

    /// <summary>
    /// 创建帧头
    /// </summary>
    private tSdkFrameHead CreateFrameHead(CameraFrame frame)
    {
        return new tSdkFrameHead
        {
            iWidth = frame.Width,
            iHeight = frame.Height,
            uiMediaType = frame.MediaType,
            uiTimeStamp = frame.TimeStamp,
            uiExpTime = frame.ExposureTime
        };
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        if (_isOpened)
        {
            CloseCamera();
        }
    }
}
