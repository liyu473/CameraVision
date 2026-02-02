using CamreaVision.Models;
using CamreaVision.Service;
using CamreaVision.View;
using CamreaVision.ViewModel;
using LyuLogExtension.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace CamreaVision.Extensions;

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
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<IMindCameraService, MindCameraService>();
        services.AddSingleton<HIK_MvCu060_CameraService>();
        services.AddSingleton<HIK_MvCu060_View>();
        services.AddSingleton<HIK_MvCu060_ViewModel>();
        services.AddSingleton<MindVisionView>();
        services.AddSingleton<MindVisionViewModel>();

        return services;
    }
}
