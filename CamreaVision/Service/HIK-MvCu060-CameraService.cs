using CamreaVision.Models;
using Microsoft.Extensions.Logging;
using MvCameraControl;
using ZLogger;

namespace CamreaVision.Service;

public class HIK_MvCu060_CameraService
{
    private readonly ILogger<HIK_MvCu060_CameraService> _logger;
    private readonly DeviceTLayerType enumTLayerType = DeviceTLayerType.MvGigEDevice | DeviceTLayerType.MvUsbDevice
            | DeviceTLayerType.MvGenTLGigEDevice | DeviceTLayerType.MvGenTLCXPDevice | DeviceTLayerType.MvGenTLCameraLinkDevice | DeviceTLayerType.MvGenTLXoFDevice;

    public HIK_MvCu060_CameraService(ILogger<HIK_MvCu060_CameraService> logger)
    {
        _logger = logger;
        InitializeSdk();
    }

    /// <summary>
    /// 初始化SDK
    /// </summary>
    public bool InitializeSdk()
    {
        try
        {
            SDKSystem.Initialize();
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
    /// </summary>
    public void FinalizeSDK()
    {
        SDKSystem.Finalize();
    }

    /// <summary>
    /// 枚举所有可用的相机设备
    /// </summary>
    /// <returns>相机设备列表</returns>
    public List<IDeviceInfo> EnumerateDevices()
    {
        List<IDeviceInfo> deviceInfoList = [];
        int nRet = DeviceEnumerator.EnumDevices(enumTLayerType, out deviceInfoList);
        if (nRet != MvError.MV_OK)
        {
            _logger.ZLogError($"Enumerate devices fail!", nRet);
            return [];
        }

        return deviceInfoList;
    }
}
