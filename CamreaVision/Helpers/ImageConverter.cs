using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MvCameraControl;

namespace CamreaVision.Helpers;

/// <summary>
/// 图像转换工具类
/// 提供从海康相机 IImage 接口到 WPF BitmapSource 的转换功能
/// </summary>
public static class ImageConverter
{
    /// <summary>
    /// 将 IImage 转换为 BitmapSource
    /// </summary>
    /// <param name="image">海康相机的图像接口</param>
    /// <returns>WPF 可用的 BitmapSource，如果转换失败则返回 null</returns>
    public static BitmapSource? ConvertToBitmapSource(IImage image)
    {
        if (image == null)
            return null;

        try
        {
            // 获取图像基本信息
            int width = (int)image.Width;
            int height = (int)image.Height;
            byte[] pixelData = image.PixelData;
            
            if (pixelData == null || pixelData.Length == 0)
                return null;

            // 根据像素格式转换
            PixelFormat pixelFormat;
            int stride;
            byte[]? convertedData = null;

            switch (image.PixelType)
            {
                // 单色格式
                case MvGvspPixelType.PixelType_Gvsp_Mono8:
                    pixelFormat = PixelFormats.Gray8;
                    stride = width;
                    convertedData = pixelData;
                    break;

                // BGR8 格式
                case MvGvspPixelType.PixelType_Gvsp_BGR8_Packed:
                    pixelFormat = PixelFormats.Bgr24;
                    stride = width * 3;
                    convertedData = pixelData;
                    break;

                // RGB8 格式 - 需要转换为 BGR8
                case MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                    pixelFormat = PixelFormats.Bgr24;
                    stride = width * 3;
                    convertedData = ConvertRgbToBgr(pixelData, width, height);
                    break;

                // BGRA8 格式
                case MvGvspPixelType.PixelType_Gvsp_BGRA8_Packed:
                    pixelFormat = PixelFormats.Bgra32;
                    stride = width * 4;
                    convertedData = pixelData;
                    break;

                // RGBA8 格式 - 需要转换为 BGRA8
                case MvGvspPixelType.PixelType_Gvsp_RGBA8_Packed:
                    pixelFormat = PixelFormats.Bgra32;
                    stride = width * 4;
                    convertedData = ConvertRgbaToBgra(pixelData, width, height);
                    break;

                // YUV422 格式 - 转换为 BGR24
                case MvGvspPixelType.PixelType_Gvsp_YUV422_Packed:
                case MvGvspPixelType.PixelType_Gvsp_YUV422_YUYV_Packed:
                    pixelFormat = PixelFormats.Bgr24;
                    stride = width * 3;
                    convertedData = ConvertYuv422ToBgr(pixelData, width, height);
                    break;

                // Bayer 格式 - 需要去马赛克处理，这里简化为灰度
                case MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                case MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                case MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                case MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                    // 简化处理：直接作为灰度图显示
                    pixelFormat = PixelFormats.Gray8;
                    stride = width;
                    convertedData = pixelData;
                    break;

                // 其他格式暂不支持
                default:
                    return null;
            }

            if (convertedData == null)
                return null;

            // 创建 WriteableBitmap 以提高性能
            var bitmap = new WriteableBitmap(width, height, 96, 96, pixelFormat, null);
            
            // 写入像素数据
            bitmap.WritePixels(
                new Int32Rect(0, 0, width, height),
                convertedData,
                stride,
                0
            );

            // 冻结以提高性能并允许跨线程访问
            bitmap.Freeze();

            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 将 RGB 格式转换为 BGR 格式
    /// </summary>
    private static byte[] ConvertRgbToBgr(byte[] rgbData, int width, int height)
    {
        byte[] bgrData = new byte[rgbData.Length];
        
        for (int i = 0; i < rgbData.Length; i += 3)
        {
            bgrData[i] = rgbData[i + 2];     // B
            bgrData[i + 1] = rgbData[i + 1]; // G
            bgrData[i + 2] = rgbData[i];     // R
        }
        
        return bgrData;
    }

    /// <summary>
    /// 将 RGBA 格式转换为 BGRA 格式
    /// </summary>
    private static byte[] ConvertRgbaToBgra(byte[] rgbaData, int width, int height)
    {
        byte[] bgraData = new byte[rgbaData.Length];
        
        for (int i = 0; i < rgbaData.Length; i += 4)
        {
            bgraData[i] = rgbaData[i + 2];     // B
            bgraData[i + 1] = rgbaData[i + 1]; // G
            bgraData[i + 2] = rgbaData[i];     // R
            bgraData[i + 3] = rgbaData[i + 3]; // A
        }
        
        return bgraData;
    }

    /// <summary>
    /// 将 YUV422 格式转换为 BGR 格式
    /// </summary>
    private static byte[] ConvertYuv422ToBgr(byte[] yuvData, int width, int height)
    {
        byte[] bgrData = new byte[width * height * 3];
        int bgrIndex = 0;

        for (int i = 0; i < yuvData.Length; i += 4)
        {
            // YUV422 格式: Y0 U Y1 V
            int y0 = yuvData[i];
            int u = yuvData[i + 1];
            int y1 = yuvData[i + 2];
            int v = yuvData[i + 3];

            // 转换第一个像素
            ConvertYuvPixelToBgr(y0, u, v, bgrData, bgrIndex);
            bgrIndex += 3;

            // 转换第二个像素
            ConvertYuvPixelToBgr(y1, u, v, bgrData, bgrIndex);
            bgrIndex += 3;
        }

        return bgrData;
    }

    /// <summary>
    /// 将单个 YUV 像素转换为 BGR
    /// </summary>
    private static void ConvertYuvPixelToBgr(int y, int u, int v, byte[] bgrData, int index)
    {
        // YUV 到 RGB 转换公式
        int c = y - 16;
        int d = u - 128;
        int e = v - 128;

        int r = (298 * c + 409 * e + 128) >> 8;
        int g = (298 * c - 100 * d - 208 * e + 128) >> 8;
        int b = (298 * c + 516 * d + 128) >> 8;

        // 限制范围
        bgrData[index] = (byte)Math.Clamp(b, 0, 255);     // B
        bgrData[index + 1] = (byte)Math.Clamp(g, 0, 255); // G
        bgrData[index + 2] = (byte)Math.Clamp(r, 0, 255); // R
    }
}
