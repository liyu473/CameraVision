using CamreaVision.ViewModel;
using System.Windows.Controls;

namespace CamreaVision.View;

/// <summary>
/// MindVisionView.xaml 的交互逻辑
/// </summary>
public partial class MindVisionView : UserControl
{
    private readonly MindVisionViewModel _vm;
    public MindVisionView(MindVisionViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        _vm = vm;
    }
}
