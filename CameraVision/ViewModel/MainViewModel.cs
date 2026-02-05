using CameraVision.View;
using CameraVision.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using LyuWpfHelper.ViewModels;
using LyuExtensions.Aspects;

namespace CameraVision.ViewModel;

[Singleton]
public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        SelectedPageIndex = 0;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Content))]
    public partial int SelectedPageIndex { get; set; } = -1;

    public object? Content =>
        SelectedPageIndex switch
        {
            0 => ServiceLocator.GetService<MindVisionView>(),
            1 => ServiceLocator.GetService<HIK_MvCu060_View>(),
            _ => null,
        };
}
