using CamreaVision.ViewModel;

namespace CamreaVision.View;

/// <summary>
/// MindVisionView.xaml 的交互逻辑
/// </summary>
public partial class MindVisionView : System.Windows.Controls.UserControl
{
    private readonly MindVisionViewModel _vm;
    public MindVisionView(MindVisionViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        _vm = vm;
    }
}
