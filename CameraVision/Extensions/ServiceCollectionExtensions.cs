using CameraVision.View;
using CameraVision.ViewModel;
using LyuCameraVision.Service;
using LyuExtensions.Extensions;
using LyuLogExtension.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CameraVision.Extensions;

/// <summary>
/// 服务注册扩展类
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 注册所有应用服务
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddZLogger();
        services.RegisterServices();
        services.AddSingleton<IMindCameraService, MindCameraService>();
        services.AddSingleton<HIK_MvCu060_CameraService>();

        return services;
    }
}
