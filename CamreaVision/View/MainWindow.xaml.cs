using CamreaVision.ViewModel;
using System.Windows;

namespace CamreaVision.View;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// 构造函数 - 通过依赖注入获取ViewModel
    /// </summary>
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
