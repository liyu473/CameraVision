using CamreaVision.ViewModel;
using System.Windows.Controls;

namespace CamreaVision.View;

/// <summary>
/// HIK_MvCu060_View.xaml 的交互逻辑
/// </summary>
public partial class HIK_MvCu060_View : UserControl
{
    private readonly HIK_MvCu060_ViewModel _vm;
    public HIK_MvCu060_View(HIK_MvCu060_ViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        _vm = vm;
    }
}
