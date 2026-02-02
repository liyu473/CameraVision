using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CamreaVision.Helpers;
using CamreaVision.Models;
using CamreaVision.Service;
using CamreaVision.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LyuWpfHelper.ViewModels;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace CamreaVision.ViewModel;

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
