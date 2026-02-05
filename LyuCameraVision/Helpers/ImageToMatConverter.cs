using MvCameraControl;
using OpenCvSharp;

namespace LyuCameraVision.Helpers;

/// <summary>
/// 图像转换工具类
/// 提供从海康相机 IImage 接口到 OpenCV Mat 的转换功能
/// </summary>
public static class ImageToMatConverter
{
    /// <summary>
    /// 将 IImage 转换为 OpenCV Mat
    /// </summary>
    /// <param name="image">海康相机的图像接口</param>
    /// <returns>OpenCV Mat 对象，如果转换失败则返回 null</returns>
    public static Mat? ConvertToMat(IImage image)
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
            Mat? mat = null;

            switch (image.PixelType)
            {
                // 单色格式 - 8位灰度
                case MvGvspPixelType.PixelType_Gvsp_Mono8:
                    mat = new Mat(height, width, MatType.CV_8UC1);
                    System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, mat.Data, pixelData.Length);
                    break;

                // BGR8 格式 - OpenCV 原生格式
                case MvGvspPixelType.PixelType_Gvsp_BGR8_Packed:
                    mat = new Mat(height, width, MatType.CV_8UC3);
                    System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, mat.Data, pixelData.Length);
                    break;

                // RGB8 格式 - 需要转换为 BGR8
                case MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                    {
                        var tempMat = new Mat(height, width, MatType.CV_8UC3);
                        System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, tempMat.Data, pixelData.Length);
                        mat = new Mat();
                        Cv2.CvtColor(tempMat, mat, ColorConversionCodes.RGB2BGR);
                        tempMat.Dispose();
                    }
                    break;

                // BGRA8 格式 - 4通道
                case MvGvspPixelType.PixelType_Gvsp_BGRA8_Packed:
                    mat = new Mat(height, width, MatType.CV_8UC4);
                    System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, mat.Data, pixelData.Length);
                    break;

                // RGBA8 格式 - 需要转换为 BGRA8
                case MvGvspPixelType.PixelType_Gvsp_RGBA8_Packed:
                    {
                        var tempMat = new Mat(height, width, MatType.CV_8UC4);
                        System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, tempMat.Data, pixelData.Length);
                        mat = new Mat();
                        Cv2.CvtColor(tempMat, mat, ColorConversionCodes.RGBA2BGRA);
                        tempMat.Dispose();
                    }
                    break;

                // YUV422 格式 - 转换为 BGR
                case MvGvspPixelType.PixelType_Gvsp_YUV422_Packed:
                case MvGvspPixelType.PixelType_Gvsp_YUV422_YUYV_Packed:
                    {
                        var yuvMat = new Mat(height, width, MatType.CV_8UC2);
                        System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, yuvMat.Data, pixelData.Length);
                        mat = new Mat();
                        Cv2.CvtColor(yuvMat, mat, ColorConversionCodes.YUV2BGR_YUYV);
                        yuvMat.Dispose();
                    }
                    break;

                // Bayer 格式 - 需要去马赛克处理
                case MvGvspPixelType.PixelType_Gvsp_BayerGR8:
                    {
                        var bayerMat = new Mat(height, width, MatType.CV_8UC1);
                        System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, bayerMat.Data, pixelData.Length);
                        mat = new Mat();
                        Cv2.CvtColor(bayerMat, mat, ColorConversionCodes.BayerGR2BGR);
                        bayerMat.Dispose();
                    }
                    break;

                case MvGvspPixelType.PixelType_Gvsp_BayerRG8:
                    {
                        var bayerMat = new Mat(height, width, MatType.CV_8UC1);
                        System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, bayerMat.Data, pixelData.Length);
                        mat = new Mat();
                        Cv2.CvtColor(bayerMat, mat, ColorConversionCodes.BayerRG2BGR);
                        bayerMat.Dispose();
                    }
                    break;

                case MvGvspPixelType.PixelType_Gvsp_BayerGB8:
                    {
                        var bayerMat = new Mat(height, width, MatType.CV_8UC1);
                        System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, bayerMat.Data, pixelData.Length);
                        mat = new Mat();
                        Cv2.CvtColor(bayerMat, mat, ColorConversionCodes.BayerGB2BGR);
                        bayerMat.Dispose();
                    }
                    break;

                case MvGvspPixelType.PixelType_Gvsp_BayerBG8:
                    {
                        var bayerMat = new Mat(height, width, MatType.CV_8UC1);
                        System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, bayerMat.Data, pixelData.Length);
                        mat = new Mat();
                        Cv2.CvtColor(bayerMat, mat, ColorConversionCodes.BayerBG2BGR);
                        bayerMat.Dispose();
                    }
                    break;

                // Mono10/12/16 格式 - 转换为 16位灰度
                case MvGvspPixelType.PixelType_Gvsp_Mono10:
                case MvGvspPixelType.PixelType_Gvsp_Mono12:
                case MvGvspPixelType.PixelType_Gvsp_Mono16:
                    {
                        // 这些格式通常是 16 位数据
                        mat = new Mat(height, width, MatType.CV_16UC1);
                        System.Runtime.InteropServices.Marshal.Copy(pixelData, 0, mat.Data, pixelData.Length);
                    }
                    break;

                // 其他格式暂不支持
                default:
                    return null;
            }

            return mat;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 将 IImage 转换为 OpenCV Mat（带自动格式转换）
    /// 如果原始格式不是 BGR8，会自动转换为 BGR8 以便于处理
    /// </summary>
    /// <param name="image">海康相机的图像接口</param>
    /// <returns>BGR8 格式的 OpenCV Mat 对象，如果转换失败则返回 null</returns>
    public static Mat? ConvertToMatBgr(IImage image)
    {
        var mat = ConvertToMat(image);
        if (mat == null)
            return null;

        try
        {
            // 如果已经是 BGR 格式，直接返回
            if (mat.Type() == MatType.CV_8UC3)
                return mat;

            // 如果是灰度图，转换为 BGR
            if (mat.Type() == MatType.CV_8UC1)
            {
                var bgrMat = new Mat();
                Cv2.CvtColor(mat, bgrMat, ColorConversionCodes.GRAY2BGR);
                mat.Dispose();
                return bgrMat;
            }

            // 如果是 BGRA，转换为 BGR
            if (mat.Type() == MatType.CV_8UC4)
            {
                var bgrMat = new Mat();
                Cv2.CvtColor(mat, bgrMat, ColorConversionCodes.BGRA2BGR);
                mat.Dispose();
                return bgrMat;
            }

            // 如果是 16 位灰度，归一化到 8 位然后转 BGR
            if (mat.Type() == MatType.CV_16UC1)
            {
                var normalized = new Mat();
                mat.ConvertTo(normalized, MatType.CV_8UC1, 1.0 / 256.0);
                mat.Dispose();
                
                var bgrMat = new Mat();
                Cv2.CvtColor(normalized, bgrMat, ColorConversionCodes.GRAY2BGR);
                normalized.Dispose();
                return bgrMat;
            }

            // 其他格式直接返回
            return mat;
        }
        catch
        {
            mat?.Dispose();
            return null;
        }
    }

    /// <summary>
    /// 将 IImage 转换为 OpenCV Mat（灰度格式）
    /// 如果原始格式不是灰度，会自动转换为灰度
    /// </summary>
    /// <param name="image">海康相机的图像接口</param>
    /// <returns>灰度格式的 OpenCV Mat 对象，如果转换失败则返回 null</returns>
    public static Mat? ConvertToMatGray(IImage image)
    {
        var mat = ConvertToMat(image);
        if (mat == null)
            return null;

        try
        {
            // 如果已经是灰度格式，直接返回
            if (mat.Type() == MatType.CV_8UC1)
                return mat;

            // 如果是彩色图，转换为灰度
            if (mat.Type() == MatType.CV_8UC3)
            {
                var grayMat = new Mat();
                Cv2.CvtColor(mat, grayMat, ColorConversionCodes.BGR2GRAY);
                mat.Dispose();
                return grayMat;
            }

            // 如果是 BGRA，转换为灰度
            if (mat.Type() == MatType.CV_8UC4)
            {
                var grayMat = new Mat();
                Cv2.CvtColor(mat, grayMat, ColorConversionCodes.BGRA2GRAY);
                mat.Dispose();
                return grayMat;
            }

            // 如果是 16 位灰度，归一化到 8 位
            if (mat.Type() == MatType.CV_16UC1)
            {
                var grayMat = new Mat();
                mat.ConvertTo(grayMat, MatType.CV_8UC1, 1.0 / 256.0);
                mat.Dispose();
                return grayMat;
            }

            // 其他格式直接返回
            return mat;
        }
        catch
        {
            mat?.Dispose();
            return null;
        }
    }
}
