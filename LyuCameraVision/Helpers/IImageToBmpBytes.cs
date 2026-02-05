using MvCameraControl;
using OpenCvSharp;

namespace LyuCameraVision.Helpers;

/// <summary>
/// 图像转换工具类 - 跨平台实现
/// 将海康相机 IImage 接口转换为标准图像格式字节数组
/// </summary>
public static class IImageToBmpBytes
{
    /// <summary>
    /// 将 IImage 转换为 BMP 格式的字节数组（跨平台实现）
    /// </summary>
    /// <param name="image">海康相机的图像接口</param>
    /// <returns>BMP 格式的字节数组，可直接用于显示或保存</returns>
    public static byte[]? ToBmpBytes(this IImage image)
    {
        if (image == null || image.PixelData == null || image.PixelData.Length == 0)
            return null;

        try
        {
            // 使用现有的 ImageToMatConverter 转换为 Mat
            var mat = ImageToMatConverter.ConvertToMatBgr(image);
            if (mat == null)
                return null;

            try
            {
                // 使用 OpenCV 编码为 BMP 格式
                Cv2.ImEncode(".bmp", mat, out var buffer);
                return buffer;
            }
            finally
            {
                mat.Dispose();
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 将 IImage 转换为 PNG 格式的字节数组（跨平台实现）
    /// PNG 格式支持无损压缩，文件更小
    /// </summary>
    /// <param name="image">海康相机的图像接口</param>
    /// <returns>PNG 格式的字节数组</returns>
    public static byte[]? ToPngBytes(this IImage image)
    {
        if (image == null || image.PixelData == null || image.PixelData.Length == 0)
            return null;

        try
        {
            var mat = ImageToMatConverter.ConvertToMatBgr(image);
            if (mat == null)
                return null;

            try
            {
                Cv2.ImEncode(".png", mat, out var buffer);
                return buffer;
            }
            finally
            {
                mat.Dispose();
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 将 IImage 转换为 JPEG 格式的字节数组（跨平台实现）
    /// JPEG 格式有损压缩，文件最小，适合传输
    /// </summary>
    /// <param name="image">海康相机的图像接口</param>
    /// <param name="quality">JPEG 质量 (0-100)，默认 95</param>
    /// <returns>JPEG 格式的字节数组</returns>
    public static byte[]? ToJpegBytes(this IImage image, int quality = 95)
    {
        if (image == null || image.PixelData == null || image.PixelData.Length == 0)
            return null;

        try
        {
            var mat = ImageToMatConverter.ConvertToMatBgr(image);
            if (mat == null)
                return null;

            try
            {
                var encodeParams = new ImageEncodingParam(ImwriteFlags.JpegQuality, quality);
                Cv2.ImEncode(".jpg", mat, out var buffer, encodeParams);
                return buffer;
            }
            finally
            {
                mat.Dispose();
            }
        }
        catch
        {
            return null;
        }
    }
}
