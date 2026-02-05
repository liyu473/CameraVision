using CameraVision.ViewModel;
using LyuExtensions.Aspects;

namespace CameraVision.View;

/// <summary>
/// 海康相机视图的交互逻辑
/// </summary>
[Singleton]
public partial class HIK_MvCu060_View : System.Windows.Controls.UserControl
{
    /// <summary>
    /// 构造函数，初始化视图并绑定数据上下文
    /// </summary>
    /// <param name="vm">视图模型实例</param>
    public HIK_MvCu060_View(HIK_MvCu060_ViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
    }
}

