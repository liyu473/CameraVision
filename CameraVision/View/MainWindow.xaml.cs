using CameraVision.ViewModel;
using LyuCameraVision.Service;
using System.Windows;

namespace CameraVision.View;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    public MainWindow(IMindCameraService mindservice, HIK_MvCu060_CameraService hikservice, MainViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        _vm = vm;

        Closed += (s, e) =>
        {
            mindservice.CloseCamera();

            hikservice.CloseCamera();
            hikservice.DisposeCamera();
            hikservice.FinalizeSDK();
        };
    }
}
