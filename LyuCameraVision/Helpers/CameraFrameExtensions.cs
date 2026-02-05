using LyuCameraVision.Models;
using OpenCvSharp;

namespace LyuCameraVision.Helpers;

/// <summary>
/// CameraFrame 扩展方法 - 跨平台实现
/// 将迈德威视相机的 RGB24 格式转换为标准图像格式
/// </summary>
public static class CameraFrameExtensions
{
    /// <summary>
    /// 将 CameraFrame (RGB24) 转换为 BMP 格式的字节数组
    /// </summary>
    /// <param name="frame">相机帧数据（RGB24 格式）</param>
    /// <returns>BMP 格式的字节数组</returns>
    public static byte[]? ToBmpBytes(this CameraFrame frame)
    {
        if (frame == null || frame.ImageData == null || frame.ImageData.Length == 0)
            return null;

        try
        {
            // RGB24 格式：每像素 3 字节 (R, G, B)
            using var mat = new Mat(frame.Height, frame.Width, MatType.CV_8UC3);
            System.Runtime.InteropServices.Marshal.Copy(frame.ImageData, 0, mat.Data, frame.ImageData.Length);
            
            // RGB 转 BGR (OpenCV 使用 BGR 格式)
            using var bgrMat = new Mat();
            Cv2.CvtColor(mat, bgrMat, ColorConversionCodes.RGB2BGR);
            
            // 编码为 BMP
            Cv2.ImEncode(".bmp", bgrMat, out var buffer);
            return buffer;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 将 CameraFrame (RGB24) 转换为 PNG 格式的字节数组
    /// </summary>
    /// <param name="frame">相机帧数据（RGB24 格式）</param>
    /// <returns>PNG 格式的字节数组</returns>
    public static byte[]? ToPngBytes(this CameraFrame frame)
    {
        if (frame == null || frame.ImageData == null || frame.ImageData.Length == 0)
            return null;

        try
        {
            using var mat = new Mat(frame.Height, frame.Width, MatType.CV_8UC3);
            System.Runtime.InteropServices.Marshal.Copy(frame.ImageData, 0, mat.Data, frame.ImageData.Length);
            
            using var bgrMat = new Mat();
            Cv2.CvtColor(mat, bgrMat, ColorConversionCodes.RGB2BGR);
            
            Cv2.ImEncode(".png", bgrMat, out var buffer);
            return buffer;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 将 CameraFrame (RGB24) 转换为 JPEG 格式的字节数组
    /// </summary>
    /// <param name="frame">相机帧数据（RGB24 格式）</param>
    /// <param name="quality">JPEG 质量 (0-100)，默认 95</param>
    /// <returns>JPEG 格式的字节数组</returns>
    public static byte[]? ToJpegBytes(this CameraFrame frame, int quality = 95)
    {
        if (frame == null || frame.ImageData == null || frame.ImageData.Length == 0)
            return null;

        try
        {
            using var mat = new Mat(frame.Height, frame.Width, MatType.CV_8UC3);
            System.Runtime.InteropServices.Marshal.Copy(frame.ImageData, 0, mat.Data, frame.ImageData.Length);
            
            using var bgrMat = new Mat();
            Cv2.CvtColor(mat, bgrMat, ColorConversionCodes.RGB2BGR);
            
            var encodeParams = new ImageEncodingParam(ImwriteFlags.JpegQuality, quality);
            Cv2.ImEncode(".jpg", bgrMat, out var buffer, encodeParams);
            return buffer;
        }
        catch
        {
            return null;
        }
    }
}
