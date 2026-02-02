using CamreaVision.Models;
using Microsoft.Extensions.Logging;
using MvCameraControl;
using ZLogger;

namespace CamreaVision.Service;

public class HIK_MvCu060_CameraService
{
    private readonly ILogger<HIK_MvCu060_CameraService> _logger;
    private readonly DeviceTLayerType enumTLayerType =
        DeviceTLayerType.MvGigEDevice
        | DeviceTLayerType.MvUsbDevice
        | DeviceTLayerType.MvGenTLGigEDevice
        | DeviceTLayerType.MvGenTLCXPDevice
        | DeviceTLayerType.MvGenTLCameraLinkDevice
        | DeviceTLayerType.MvGenTLXoFDevice;

    private IDevice? device;

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

    public bool OpenCamera(IDeviceInfo camera)
    {
        try
        {
            device = DeviceFactory.CreateDevice(camera);
        }
        catch (Exception ex)
        {
            _logger.ZLogError($"Create Device fail!:{ex}");
            return false;
        }

        int result = device.Open();
        if (result != MvError.MV_OK)
        {
            _logger.ZLogError($"Open Device fail!", result);
            return false;
        }

        //判断是否为gige设备 
        if (device is IGigEDevice)
        {
            //转换为gigE设备 
            IGigEDevice? gigEDevice = device as IGigEDevice;

            // ch:探测网络最佳包大小(只对GigE相机有效) 
            result = gigEDevice!.GetOptimalPacketSize(out int optionPacketSize);
            if (result != MvError.MV_OK)
            {
                _logger.ZLogError($"Warning: Get Packet Size failed!", result);
                return false;
            }
            else
            {
                result = device.Parameters.SetIntValue("GevSCPSPacketSize", (long)optionPacketSize);
                if (result != MvError.MV_OK)
                {
                    _logger.ZLogError($"Warning: Set Packet Size failed!", result);
                    return false;
                }
            }
        }

        device.Parameters.SetEnumValueByString("AcquisitionMode", "Continuous");
        device.Parameters.SetEnumValueByString("TriggerMode", "Off");

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
            _logger.ZLogWarning($"Device is null, cannot close");
            return false;
        }

        int ret = device.Close();
        if (ret != MvError.MV_OK)
        {
            _logger.ZLogError($"Close device fail: {ret}");
            return false;
        }
        else
        {
            _logger.ZLogInformation($"Close device success!");
            return true;
        }
    }

    /// <summary>
    /// 销毁相机实例
    /// </summary>
    public void DisposeCamera()
    {
        if (device == null)
        {
            _logger.ZLogWarning($"Device is null, cannot dispose");
            return;
        }

        device.Dispose();
        device = null;
        _logger.ZLogInformation($"Device disposed successfully!");
    }
}
