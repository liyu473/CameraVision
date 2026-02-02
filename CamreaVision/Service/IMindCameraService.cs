using CamreaVision.Models;

namespace CamreaVision.Service;

/// <summary>
/// 相机服务接口
/// </summary>
public interface IMindCameraService
{
    /// <summary>
    /// 初始化SDK
    /// </summary>
    /// <returns>是否成功</returns>
    bool InitializeSdk();

    /// <summary>
    /// 枚举所有可用的相机设备
    /// </summary>
    /// <returns>相机设备列表</returns>
    List<MindCameraInfo> EnumerateDevices();

    /// <summary>
    /// 打开指定的相机
    /// </summary>
    /// <param name="deviceIndex">设备索引</param>
    /// <returns>是否成功</returns>
    bool OpenCamera(int deviceIndex);

    /// <summary>
    /// 关闭当前相机
    /// </summary>
    /// <returns>是否成功</returns>
    bool CloseCamera();

    /// <summary>
    /// 开始采集图像
    /// </summary>
    /// <returns>是否成功</returns>
    bool StartCapture();

    /// <summary>
    /// 停止采集图像
    /// </summary>
    /// <returns>是否成功</returns>
    bool StopCapture();

    /// <summary>
    /// 获取一帧图像
    /// </summary>
    /// <param name="timeout">超时时间（毫秒）</param>
    /// <returns>相机帧数据</returns>
    CameraFrame? GetFrame(int timeout = 1000);

    /// <summary>
    /// 软触发一次
    /// </summary>
    /// <returns>是否成功</returns>
    bool SoftTrigger();

    /// <summary>
    /// 设置曝光时间
    /// </summary>
    /// <param name="exposureTime">曝光时间（微秒）</param>
    /// <returns>是否成功</returns>
    bool SetExposureTime(double exposureTime);

    /// <summary>
    /// 获取曝光时间
    /// </summary>
    /// <returns>曝光时间（微秒）</returns>
    double GetExposureTime();

    /// <summary>
    /// 设置增益
    /// </summary>
    /// <param name="gain">增益值</param>
    /// <returns>是否成功</returns>
    bool SetGain(int gain);

    /// <summary>
    /// 获取增益
    /// </summary>
    /// <returns>增益值</returns>
    int GetGain();

    /// <summary>
    /// 设置分辨率
    /// </summary>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <returns>是否成功</returns>
    bool SetResolution(int width, int height);

    /// <summary>
    /// 获取当前分辨率
    /// </summary>
    /// <returns>分辨率信息</returns>
    CameraResolution? GetResolution();

    /// <summary>
    /// 设置触发模式
    /// </summary>
    /// <param name="mode">0-连续采集，1-软触发，2-硬触发</param>
    /// <returns>是否成功</returns>
    bool SetTriggerMode(int mode);


    /// <summary>
    /// 保存图像到文件
    /// </summary>
    /// <param name="frame">帧数据</param>
    /// <param name="filePath">文件路径</param>
    /// <param name="quality">质量（1-100）</param>
    /// <returns>是否成功</returns>
    bool SaveImage(CameraFrame frame, string filePath, int quality = 100);

    /// <summary>
    /// 相机是否已打开
    /// </summary>
    bool IsOpened { get; }

    /// <summary>
    /// 相机是否正在采集
    /// </summary>
    bool IsCapturing { get; }

    /// <summary>
    /// 当前打开的相机信息
    /// </summary>
    MindCameraInfo? CurrentCamera { get; }

    /// <summary>
    /// 帧回调事件
    /// </summary>
    event EventHandler<CameraFrame>? FrameReceived;

    /// <summary>
    /// 打开自带的设置页面
    /// </summary>
    void OpenSettingPage();


    /// <summary>
    /// 彩色/黑白模式切换
    /// </summary>
    /// <param name="isColor"></param>
    /// <returns></returns>
    bool SetColorMode(bool isColor);
}
