using System.Windows.Media.Imaging;

namespace CamreaVision.Models;

/// <summary>
/// 相机帧数据
/// </summary>
public class CameraFrame
{
    /// <summary>
    /// 图像数据
    /// </summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// 图像宽度
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 图像高度
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 图像格式
    /// </summary>
    public uint MediaType { get; set; }

    /// <summary>
    /// 帧号
    /// </summary>
    public uint FrameNumber { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public uint TimeStamp { get; set; }

    /// <summary>
    /// 曝光时间（微秒）
    /// </summary>
    public uint ExposureTime { get; set; }

    /// <summary>
    /// 增益值
    /// </summary>
    public uint AnalogGain { get; set; }

    /// <summary>
    /// BitmapSource用于WPF显示
    /// </summary>
    public BitmapSource? BitmapSource { get; set; }

    /// <summary>
    /// 捕获时间
    /// </summary>
    public DateTime CaptureTime { get; set; } = DateTime.Now;
}
