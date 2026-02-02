using CamreaVision.Models;
using Microsoft.Extensions.Logging;
using MvCameraControl;
using ZLogger;

namespace CamreaVision.Service;

/// <summary>
/// 海康威视工业相机服务类
/// 提供相机的初始化、打开、关闭、采集、参数设置等功能
/// </summary>
public class HIK_MvCu060_CameraService
{
    #region 私有字段

    /// <summary>
    /// 日志记录器
    /// </summary>
    private readonly ILogger<HIK_MvCu060_CameraService> _logger;

    /// <summary>
    /// 支持的设备传输层类型
    /// 包括GigE、USB、GenTL等多种接口类型
    /// </summary>
    private readonly DeviceTLayerType enumTLayerType =
        DeviceTLayerType.MvGigEDevice
        | DeviceTLayerType.MvUsbDevice
        | DeviceTLayerType.MvGenTLGigEDevice
        | DeviceTLayerType.MvGenTLCXPDevice
        | DeviceTLayerType.MvGenTLCameraLinkDevice
        | DeviceTLayerType.MvGenTLXoFDevice;

    /// <summary>
    /// 相机设备实例
    /// </summary>
    private IDevice? device;

    /// <summary>
    /// 用于保存图像的帧数据
    /// </summary>
    private IFrameOut? frameForSave;

    /// <summary>
    /// 保存图像时的同步锁对象
    /// 防止多线程同时访问帧数据
    /// </summary>
    private readonly object saveImageLock = new();

    #endregion

    #region 公共属性

    /// <summary>
    /// 获取相机是否已打开
    /// </summary>
    public bool IsOpened => device != null;

    /// <summary>
    /// 获取相机是否正在采集
    /// </summary>
    public bool IsGrabbing { get; private set; }

    /// <summary>
    /// 获取当前设备实例（用于在界面上渲染图像）
    /// </summary>
    public IDevice? Device => device;

    #endregion

    #region 构造函数

    /// <summary>
    /// 构造函数，初始化相机服务
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public HIK_MvCu060_CameraService(ILogger<HIK_MvCu060_CameraService> logger)
    {
        _logger = logger;
        InitializeSdk();
    }

    #endregion

    #region SDK初始化与释放

    /// <summary>
    /// 初始化SDK
    /// 在使用相机之前必须先调用此方法
    /// </summary>
    /// <returns>初始化是否成功</returns>
    public bool InitializeSdk()
    {
        try
        {
            SDKSystem.Initialize();
            _logger.ZLogInformation($"海康SDK初始化成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"初始化相机SDK失败:{ex}");
            return false;
        }
    }

    /// <summary>
    /// 反初始化SDK
    /// 在程序退出前调用，释放SDK资源
    /// </summary>
    public void FinalizeSDK()
    {
        SDKSystem.Finalize();
        _logger.ZLogInformation($"海康SDK已释放");
    }

    #endregion

    #region 设备枚举与连接

    /// <summary>
    /// 枚举所有可用的相机设备
    /// </summary>
    /// <returns>相机设备列表，如果枚举失败则返回空列表</returns>
    public List<IDeviceInfo> EnumerateDevices()
    {
        List<IDeviceInfo> deviceInfoList = [];
        int nRet = DeviceEnumerator.EnumDevices(enumTLayerType, out deviceInfoList);
        if (nRet != MvError.MV_OK)
        {
            _logger.ZLogError($"枚举设备失败! 错误码: {nRet:X}");
            return [];
        }

        _logger.ZLogInformation($"枚举到 {deviceInfoList.Count} 个设备");
        return deviceInfoList;
    }

    /// <summary>
    /// 打开指定的相机设备
    /// </summary>
    /// <param name="camera">要打开的相机设备信息</param>
    /// <returns>是否成功打开</returns>
    public bool OpenCamera(IDeviceInfo camera)
    {
        try
        {
            // 创建设备实例
            device = DeviceFactory.CreateDevice(camera);
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"创建设备失败!:{ex}");
            return false;
        }

        // 打开设备
        int result = device.Open();
        if (result != MvError.MV_OK)
        {
            _logger.ZLogError($"打开设备失败! 错误码: {result:X}");
            return false;
        }

        // 判断是否为GigE设备，如果是则设置最佳网络包大小
        if (device is IGigEDevice gigEDevice)
        {
            // 探测网络最佳包大小(只对GigE相机有效)
            result = gigEDevice.GetOptimalPacketSize(out int optionPacketSize);
            if (result != MvError.MV_OK)
            {
                _logger.ZLogWarning($"获取最佳包大小失败! 错误码: {result:X}");
            }
            else
            {
                result = device.Parameters.SetIntValue("GevSCPSPacketSize", optionPacketSize);
                if (result != MvError.MV_OK)
                {
                    _logger.ZLogWarning($"设置包大小失败! 错误码: {result:X}");
                }
                else
                {
                    _logger.ZLogInformation($"GigE设备包大小设置为: {optionPacketSize}");
                }
            }
        }

        // 设置采集模式为连续采集，关闭触发模式
        device.Parameters.SetEnumValueByString("AcquisitionMode", "Continuous");
        device.Parameters.SetEnumValueByString("TriggerMode", "Off");

        _logger.ZLogInformation($"相机打开成功: {camera.ModelName}");
        return true;
    }

    /// <summary>
    /// 关闭相机
    /// </summary>
    /// <returns>是否成功关闭</returns>
    public bool CloseCamera()
    {
        if (device == null)
        {
            _logger.ZLogWarning($"设备为空，无法关闭");
            return false;
        }

        // 如果正在采集，先停止采集
        if (IsGrabbing)
        {
            StopGrabbing();
        }

        int ret = device.Close();
        if (ret != MvError.MV_OK)
        {
            _logger.ZLogError($"关闭设备失败! 错误码: {ret:X}");
            return false;
        }

        _logger.ZLogInformation($"相机关闭成功!");
        return true;
    }

    /// <summary>
    /// 销毁相机实例
    /// 释放相机资源
    /// </summary>
    public void DisposeCamera()
    {
        if (device == null)
        {
            _logger.ZLogWarning($"设备为空，无法销毁");
            return;
        }

        // 释放保存的帧数据
        lock (saveImageLock)
        {
            frameForSave?.Dispose();
            frameForSave = null;
        }

        device.Dispose();
        device = null;
        _logger.ZLogInformation($"设备已销毁!");
    }

    #endregion

    #region 图像采集控制

    /// <summary>
    /// 开始采集
    /// </summary>
    /// <returns>是否成功开始采集</returns>
    public bool StartGrabbing()
    {
        if (device == null)
        {
            _logger.ZLogError($"设备为空，无法开始采集");
            return false;
        }

        int result = device.StreamGrabber.StartGrabbing();
        if (result != MvError.MV_OK)
        {
            _logger.ZLogError($"开始采集失败! 错误码: {result:X}");
            return false;
        }

        IsGrabbing = true;
        _logger.ZLogInformation($"开始采集成功");
        return true;
    }

    /// <summary>
    /// 停止采集
    /// </summary>
    /// <returns>是否成功停止采集</returns>
    public bool StopGrabbing()
    {
        if (device == null)
        {
            _logger.ZLogError($"设备为空，无法停止采集");
            return false;
        }

        int result = device.StreamGrabber.StopGrabbing();
        if (result != MvError.MV_OK)
        {
            _logger.ZLogError($"停止采集失败! 错误码: {result:X}");
            return false;
        }

        IsGrabbing = false;
        _logger.ZLogInformation($"停止采集成功");
        return true;
    }

    /// <summary>
    /// 获取一帧图像
    /// </summary>
    /// <param name="timeout">超时时间（毫秒）</param>
    /// <param name="frameOut">输出的帧数据</param>
    /// <returns>是否成功获取</returns>
    public bool GetImageBuffer(uint timeout, out IFrameOut? frameOut)
    {
        frameOut = null;

        if (device == null)
        {
            return false;
        }

        int result = device.StreamGrabber.GetImageBuffer(timeout, out frameOut);
        if (result == MvError.MV_OK && frameOut != null)
        {
            // 保存帧数据用于后续保存图像
            lock (saveImageLock)
            {
                frameForSave?.Dispose();
                frameForSave = frameOut.Clone() as IFrameOut;
            }
            return true;
        }

        return false;
    }

    /// <summary>
    /// 释放帧缓冲区
    /// 获取图像后必须调用此方法释放缓冲区
    /// </summary>
    /// <param name="frameOut">要释放的帧数据</param>
    public void FreeImageBuffer(IFrameOut frameOut)
    {
        device?.StreamGrabber.FreeImageBuffer(frameOut);
    }

    /// <summary>
    /// 在指定窗口句柄上显示一帧图像
    /// 使用SDK内置的渲染方法，效率最高（官方推荐方式）
    /// </summary>
    /// <param name="handle">窗口句柄（通常是PictureBox.Handle）</param>
    /// <param name="frameOut">要显示的帧数据</param>
    public void DisplayOneFrame(IntPtr handle, IFrameOut frameOut)
    {
        device?.ImageRender.DisplayOneFrame(handle, frameOut.Image);
    }

    #endregion

    #region 触发模式控制

    /// <summary>
    /// 设置触发模式
    /// </summary>
    /// <param name="isTriggerMode">是否启用触发模式</param>
    /// <returns>是否设置成功</returns>
    public bool SetTriggerMode(bool isTriggerMode)
    {
        if (device == null)
        {
            _logger.ZLogError($"设备为空，无法设置触发模式");
            return false;
        }

        string mode = isTriggerMode ? "On" : "Off";
        int result = device.Parameters.SetEnumValueByString("TriggerMode", mode);
        if (result != MvError.MV_OK)
        {
            _logger.ZLogError($"设置触发模式失败! 错误码: {result:X}");
            return false;
        }

        _logger.ZLogInformation($"触发模式设置为: {mode}");
        return true;
    }

    /// <summary>
    /// 设置触发源
    /// </summary>
    /// <param name="isSoftTrigger">是否为软触发，false则为硬触发Line0</param>
    /// <returns>是否设置成功</returns>
    public bool SetTriggerSource(bool isSoftTrigger)
    {
        if (device == null)
        {
            _logger.ZLogError($"设备为空，无法设置触发源");
            return false;
        }

        string source = isSoftTrigger ? "Software" : "Line0";
        int result = device.Parameters.SetEnumValueByString("TriggerSource", source);
        if (result != MvError.MV_OK)
        {
            _logger.ZLogError($"设置触发源失败! 错误码: {result:X}");
            return false;
        }

        _logger.ZLogInformation($"触发源设置为: {source}");
        return true;
    }

    /// <summary>
    /// 执行软触发
    /// 在软触发模式下调用此方法触发一次采集
    /// </summary>
    /// <returns>是否触发成功</returns>
    public bool TriggerSoftware()
    {
        if (device == null)
        {
            _logger.ZLogError($"设备为空，无法执行软触发");
            return false;
        }

        int result = device.Parameters.SetCommandValue("TriggerSoftware");
        if (result != MvError.MV_OK)
        {
            _logger.ZLogError($"软触发执行失败! 错误码: {result:X}");
            return false;
        }

        _logger.ZLogInformation($"软触发执行成功");
        return true;
    }

    #endregion

    #region 参数获取

    /// <summary>
    /// 获取曝光时间
    /// </summary>
    /// <param name="exposureTime">输出的曝光时间（微秒）</param>
    /// <returns>是否获取成功</returns>
    public bool GetExposureTime(out float exposureTime)
    {
        exposureTime = 0;

        if (device == null)
        {
            _logger.ZLogError($"设备为空，无法获取曝光时间");
            return false;
        }

        int result = device.Parameters.GetFloatValue("ExposureTime", out IFloatValue floatValue);
        if (result == MvError.MV_OK)
        {
            exposureTime = (float)floatValue.CurValue;
            return true;
        }

        _logger.ZLogError($"获取曝光时间失败! 错误码: {result:X}");
        return false;
    }

    /// <summary>
    /// 获取曝光时间范围
    /// </summary>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <returns>是否获取成功</returns>
    public bool GetExposureTimeRange(out float min, out float max)
    {
        min = 0;
        max = 0;

        if (device == null) return false;

        int result = device.Parameters.GetFloatValue("ExposureTime", out IFloatValue floatValue);
        if (result == MvError.MV_OK)
        {
            min = (float)floatValue.Min;
            max = (float)floatValue.Max;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取增益值
    /// </summary>
    /// <param name="gain">输出的增益值</param>
    /// <returns>是否获取成功</returns>
    public bool GetGain(out float gain)
    {
        gain = 0;

        if (device == null)
        {
            _logger.ZLogError($"设备为空，无法获取增益");
            return false;
        }

        int result = device.Parameters.GetFloatValue("Gain", out IFloatValue floatValue);
        if (result == MvError.MV_OK)
        {
            gain = (float)floatValue.CurValue;
            return true;
        }

        _logger.ZLogError($"获取增益失败! 错误码: {result:X}");
        return false;
    }

    /// <summary>
    /// 获取增益范围
    /// </summary>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <returns>是否获取成功</returns>
    public bool GetGainRange(out float min, out float max)
    {
        min = 0;
        max = 0;

        if (device == null) return false;

        int result = device.Parameters.GetFloatValue("Gain", out IFloatValue floatValue);
        if (result == MvError.MV_OK)
        {
            min = (float)floatValue.Min;
            max = (float)floatValue.Max;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取帧率
    /// </summary>
    /// <param name="frameRate">输出的帧率值</param>
    /// <returns>是否获取成功</returns>
    public bool GetFrameRate(out float frameRate)
    {
        frameRate = 0;

        if (device == null)
        {
            _logger.ZLogError($"设备为空，无法获取帧率");
            return false;
        }

        // 获取实际帧率
        int result = device.Parameters.GetFloatValue("ResultingFrameRate", out IFloatValue floatValue);
        if (result == MvError.MV_OK)
        {
            frameRate = (float)floatValue.CurValue;
            return true;
        }

        _logger.ZLogError($"获取帧率失败! 错误码: {result:X}");
        return false;
    }

    /// <summary>
    /// 获取帧率范围
    /// </summary>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <returns>是否获取成功</returns>
    public bool GetFrameRateRange(out float min, out float max)
    {
        min = 0;
        max = 0;

        if (device == null) return false;

        int result = device.Parameters.GetFloatValue("AcquisitionFrameRate", out IFloatValue floatValue);
        if (result == MvError.MV_OK)
        {
            min = (float)floatValue.Min;
            max = (float)floatValue.Max;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取图像宽度
    /// </summary>
    /// <param name="width">输出的图像宽度</param>
    /// <returns>是否获取成功</returns>
    public bool GetImageWidth(out int width)
    {
        width = 0;

        if (device == null) return false;

        int result = device.Parameters.GetIntValue("Width", out IIntValue intValue);
        if (result == MvError.MV_OK)
        {
            width = (int)intValue.CurValue;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取图像高度
    /// </summary>
    /// <param name="height">输出的图像高度</param>
    /// <returns>是否获取成功</returns>
    public bool GetImageHeight(out int height)
    {
        height = 0;

        if (device == null) return false;

        int result = device.Parameters.GetIntValue("Height", out IIntValue intValue);
        if (result == MvError.MV_OK)
        {
            height = (int)intValue.CurValue;
            return true;
        }

        return false;
    }

    #endregion

    #region 参数设置

    /// <summary>
    /// 设置曝光时间
    /// </summary>
    /// <param name="exposureTime">曝光时间（微秒）</param>
    /// <returns>是否设置成功</returns>
    public bool SetExposureTime(float exposureTime)
    {
        if (device == null)
        {
            _logger.ZLogError($"设备为空，无法设置曝光时间");
            return false;
        }

        // 先关闭自动曝光
        device.Parameters.SetEnumValue("ExposureAuto", 0);

        int result = device.Parameters.SetFloatValue("ExposureTime", exposureTime);
        if (result != MvError.MV_OK)
        {
            _logger.ZLogError($"设置曝光时间失败! 错误码: {result:X}");
            return false;
        }

        _logger.ZLogInformation($"曝光时间设置为: {exposureTime} us");
        return true;
    }

    /// <summary>
    /// 设置增益
    /// </summary>
    /// <param name="gain">增益值</param>
    /// <returns>是否设置成功</returns>
    public bool SetGain(float gain)
    {
        if (device == null)
        {
            _logger.ZLogError($"设备为空，无法设置增益");
            return false;
        }

        // 先关闭自动增益
        device.Parameters.SetEnumValue("GainAuto", 0);

        int result = device.Parameters.SetFloatValue("Gain", gain);
        if (result != MvError.MV_OK)
        {
            _logger.ZLogError($"设置增益失败! 错误码: {result:X}");
            return false;
        }

        _logger.ZLogInformation($"增益设置为: {gain}");
        return true;
    }

    /// <summary>
    /// 设置帧率
    /// </summary>
    /// <param name="frameRate">帧率值</param>
    /// <returns>是否设置成功</returns>
    public bool SetFrameRate(float frameRate)
    {
        if (device == null)
        {
            _logger.ZLogError($"设备为空，无法设置帧率");
            return false;
        }

        // 先启用帧率控制
        int result = device.Parameters.SetBoolValue("AcquisitionFrameRateEnable", true);
        if (result != MvError.MV_OK)
        {
            _logger.ZLogWarning($"启用帧率控制失败! 错误码: {result:X}");
        }

        result = device.Parameters.SetFloatValue("AcquisitionFrameRate", frameRate);
        if (result != MvError.MV_OK)
        {
            _logger.ZLogError($"设置帧率失败! 错误码: {result:X}");
            return false;
        }

        _logger.ZLogInformation($"帧率设置为: {frameRate} fps");
        return true;
    }

    #endregion

    #region 图像保存

    /// <summary>
    /// 保存当前帧为BMP格式
    /// </summary>
    /// <param name="filePath">保存路径，如果为空则自动生成文件名</param>
    /// <returns>是否保存成功</returns>
    public bool SaveImageAsBmp(string? filePath = null)
    {
        return SaveImage(ImageFormatType.Bmp, filePath);
    }

    /// <summary>
    /// 保存当前帧为JPG格式
    /// </summary>
    /// <param name="filePath">保存路径，如果为空则自动生成文件名</param>
    /// <param name="quality">JPEG质量(1-100)</param>
    /// <returns>是否保存成功</returns>
    public bool SaveImageAsJpg(string? filePath = null, int quality = 80)
    {
        return SaveImage(ImageFormatType.Jpeg, filePath, quality);
    }

    /// <summary>
    /// 保存当前帧为PNG格式
    /// </summary>
    /// <param name="filePath">保存路径，如果为空则自动生成文件名</param>
    /// <returns>是否保存成功</returns>
    public bool SaveImageAsPng(string? filePath = null)
    {
        return SaveImage(ImageFormatType.Png, filePath);
    }

    /// <summary>
    /// 保存当前帧为TIFF格式
    /// </summary>
    /// <param name="filePath">保存路径，如果为空则自动生成文件名</param>
    /// <returns>是否保存成功</returns>
    public bool SaveImageAsTiff(string? filePath = null)
    {
        return SaveImage(ImageFormatType.Tiff, filePath);
    }

    /// <summary>
    /// 保存图像的内部实现
    /// </summary>
    /// <param name="formatType">图像格式类型</param>
    /// <param name="filePath">保存路径</param>
    /// <param name="jpegQuality">JPEG质量</param>
    /// <returns>是否保存成功</returns>
    private bool SaveImage(ImageFormatType formatType, string? filePath = null, int jpegQuality = 80)
    {
        if (device == null)
        {
            _logger.ZLogError($"设备为空，无法保存图像");
            return false;
        }

        lock (saveImageLock)
        {
            if (frameForSave == null)
            {
                _logger.ZLogError($"没有可保存的图像帧");
                return false;
            }

            try
            {
                // 如果没有指定路径，自动生成文件名
                if (string.IsNullOrEmpty(filePath))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                    filePath = $"Image_{timestamp}_w{frameForSave.Image.Width}_h{frameForSave.Image.Height}.{formatType.ToString().ToLower()}";
                }

                ImageFormatInfo imageFormatInfo = new()
                {
                    FormatType = formatType
                };

                // 如果是JPEG格式，设置质量
                if (formatType == ImageFormatType.Jpeg)
                {
                    imageFormatInfo.JpegQuality = (uint)jpegQuality;
                }

                int result = device.ImageSaver.SaveImageToFile(filePath, frameForSave.Image, imageFormatInfo, CFAMethod.Equilibrated);
                if (result != MvError.MV_OK)
                {
                    _logger.ZLogError($"保存图像失败! 错误码: {result:X}");
                    return false;
                }

                _logger.ZLogInformation($"图像保存成功: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.ZLogError($"保存图像异常: {ex.Message}");
                return false;
            }
        }
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 获取错误信息描述
    /// </summary>
    /// <param name="errorCode">错误码</param>
    /// <returns>错误描述字符串</returns>
    public static string GetErrorMessage(int errorCode)
    {
        return errorCode switch
        {
            MvError.MV_E_HANDLE => "错误或无效的句柄",
            MvError.MV_E_SUPPORT => "不支持的功能",
            MvError.MV_E_BUFOVER => "缓存已满",
            MvError.MV_E_CALLORDER => "函数调用顺序错误",
            MvError.MV_E_PARAMETER => "参数错误",
            MvError.MV_E_RESOURCE => "资源申请失败",
            MvError.MV_E_NODATA => "无数据",
            MvError.MV_E_PRECONDITION => "前提条件错误或运行环境改变",
            MvError.MV_E_VERSION => "版本不匹配",
            MvError.MV_E_NOENOUGH_BUF => "内存不足",
            MvError.MV_E_UNKNOW => "未知错误",
            MvError.MV_E_GC_GENERIC => "通用错误",
            MvError.MV_E_GC_ACCESS => "节点访问条件错误",
            MvError.MV_E_ACCESS_DENIED => "无权限",
            MvError.MV_E_BUSY => "设备忙或网络断开",
            MvError.MV_E_NETER => "网络错误",
            _ => $"未知错误码: {errorCode:X}"
        };
    }

    #endregion
}
