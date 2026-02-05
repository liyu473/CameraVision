using CameraVision;
using Microsoft.Extensions.DependencyInjection;

namespace CameraVision.Helpers;

/// <summary>
/// 服务定位器 - 用于在非依赖注入上下文中获取服务
/// </summary>
public static class ServiceLocator
{
    /// <summary>
    /// 获取指定类型的服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    /// <exception cref="InvalidOperationException">当服务未注册时抛出</exception>
    public static T GetService<T>() where T : notnull
    {
        var service = App.ServiceProvider.GetService<T>();
        return service == null ? throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册到依赖注入容器中") : service;
    }

    /// <summary>
    /// 获取必需的服务（如果服务未注册会抛出异常）
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例</returns>
    public static T GetRequiredService<T>() where T : notnull
    {
        return App.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// 尝试获取服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="service">输出的服务实例</param>
    /// <returns>如果服务存在返回true，否则返回false</returns>
    public static bool TryGetService<T>(out T? service) where T : class
    {
        service = App.ServiceProvider.GetService<T>();
        return service != null;
    }

    /// <summary>
    /// 获取所有指定类型的服务
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <returns>服务实例集合</returns>
    public static IEnumerable<T> GetServices<T>() where T : notnull
    {
        return App.ServiceProvider.GetServices<T>();
    }
}
