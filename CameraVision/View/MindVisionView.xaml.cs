using CameraVision.ViewModel;
using LyuExtensions.Aspects;

namespace CameraVision.View;

/// <summary>
/// MindVisionView.xaml 的交互逻辑
/// </summary>
[Singleton]
public partial class MindVisionView : System.Windows.Controls.UserControl
{
    [Inject]
    private readonly MindVisionViewModel _vm;
    public MindVisionView()
    {
        InitializeComponent();
        DataContext = _vm;
    }
}
