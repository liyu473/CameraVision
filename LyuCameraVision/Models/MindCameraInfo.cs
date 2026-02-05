namespace LyuCameraVision.Models;

/// <summary>
/// 相机设备信息
/// </summary>
public class MindCameraInfo
{
    /// <summary>
    /// 产品系列
    /// </summary>
    public string ProductSeries { get; set; } = string.Empty;

    /// <summary>
    /// 产品名称
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// 友好名称
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    /// 设备链接名称
    /// </summary>
    public string LinkName { get; set; } = string.Empty;

    /// <summary>
    /// 驱动版本
    /// </summary>
    public string DriverVersion { get; set; } = string.Empty;

    /// <summary>
    /// 传感器类型
    /// </summary>
    public string SensorType { get; set; } = string.Empty;

    /// <summary>
    /// 接口类型
    /// </summary>
    public string PortType { get; set; } = string.Empty;

    /// <summary>
    /// 产品唯一序列号
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// 实例索引号
    /// </summary>
    public uint Instance { get; set; }

    /// <summary>
    /// 设备索引
    /// </summary>
    public int DeviceIndex { get; set; }
}
