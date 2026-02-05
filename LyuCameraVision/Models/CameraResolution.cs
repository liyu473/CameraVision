namespace LyuCameraVision.Models;

/// <summary>
/// 相机分辨率信息
/// </summary>
public class CameraResolution
{
    /// <summary>
    /// 分辨率索引
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 分辨率描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 图像宽度
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 图像高度
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 水平偏移
    /// </summary>
    public int HorizontalOffset { get; set; }

    /// <summary>
    /// 垂直偏移
    /// </summary>
    public int VerticalOffset { get; set; }

    /// <summary>
    /// FOV宽度
    /// </summary>
    public int WidthFOV { get; set; }

    /// <summary>
    /// FOV高度
    /// </summary>
    public int HeightFOV { get; set; }
}
