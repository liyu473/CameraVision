using System.Windows;
using CamreaVision.Helpers;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace CamreaVision.Handlers;

/// <summary>
/// 全局异常处理器
/// </summary>
public static class GlobalExceptionHandler
{
    private static ILogger Logger =>
        ServiceLocator.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler");

    /// <summary>
    /// 初始化全局异常处理
    /// </summary>
    public static void Initialize()
    {
        // 捕获UI线程未处理的异常
        Application.Current.DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 捕获非UI线程未处理的异常
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // 捕获Task异步异常
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private static int _dialogShowing;

    private static void ShowErrorDialog(string title, Exception ex)
    {
        var message = $"{ex.Message}\n";

        // 限流：如果已经在弹窗，就不重复弹
        if (Interlocked.Exchange(ref _dialogShowing, 1) == 1)
            return;

        void Show()
        {
            try
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Interlocked.Exchange(ref _dialogShowing, 0);
            }
        }

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.HasShutdownStarted)
        {
            Interlocked.Exchange(ref _dialogShowing, 0);
            return;
        }

        if (dispatcher.CheckAccess())
            Show();
        else
            dispatcher.BeginInvoke(Show);
    }

    /// <summary>
    /// UI线程异常处理
    /// </summary>
    private static void OnDispatcherUnhandledException(
        object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e
    )
    {
        Logger.ZLogError(e.Exception, $"UI线程异常");
        ShowErrorDialog("UI线程捕获", e.Exception);

        e.Handled = true;
    }

    /// <summary>
    /// 非UI线程异常处理
    /// </summary>
    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Logger.ZLogError(exception, $"非UI线程异常(IsTerminating={e.IsTerminating})");
            ShowErrorDialog(e.IsTerminating ? "程序即将退出" : "后台线程发生错误", exception);
        }
        else
        {
            Logger.ZLogError($"非UI线程异常：{e.ExceptionObject}");
        }
    }

    /// <summary>
    /// Task异步异常处理
    /// </summary>
    private static void OnUnobservedTaskException(
        object? sender,
        UnobservedTaskExceptionEventArgs e
    )
    {
        Logger.ZLogError(e.Exception, $"任务异常");

        var ex = e.Exception.Flatten().InnerException ?? e.Exception;
        ShowErrorDialog("异步任务发生错误", ex);

        e.SetObserved(); // 标记已观察
    }
}
