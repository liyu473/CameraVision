using CameraVision.ViewModel;
using LyuCameraVision.Service;
using LyuExtensions.Aspects;
using System.Windows;

namespace CameraVision.View;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
[Singleton]
public partial class MainWindow : Window
{
    [Inject]
    private readonly MainViewModel _vm;
    public MainWindow(IMindCameraService mindservice, HIK_MvCu060_CameraService hikservice)
    {
        InitializeComponent();
        DataContext = _vm;

        Closed += (s, e) =>
        {
            mindservice.CloseCamera();

            hikservice.CloseCamera();
            hikservice.DisposeCamera();
            hikservice.FinalizeSDK();
        };
    }
}
