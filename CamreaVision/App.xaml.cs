using CamreaVision.Extensions;
using CamreaVision.Handlers;
using CamreaVision.View;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace CamreaVision;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// 依赖注入服务提供者
    /// </summary>
    public static ServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// 应用程序启动
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        // 初始化全局异常处理
        GlobalExceptionHandler.Initialize();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    /// <summary>
    /// 配置服务
    /// </summary>
    private void ConfigureServices(IServiceCollection services)
    {
        services.AddApplicationServices();
    }

    /// <summary>
    /// 应用程序退出
    /// </summary>
    protected override void OnExit(ExitEventArgs e)
    {
        ServiceProvider?.Dispose();
        base.OnExit(e);
    }
}
