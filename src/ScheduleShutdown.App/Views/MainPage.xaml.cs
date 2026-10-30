using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ScheduleShutdown.App.ViewModels;
using ScheduleShutdown.Core.Models;

namespace ScheduleShutdown.App.Views;

public sealed partial class MainPage : Page
{
    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainViewModel>();
        InitializeComponent();
    }

    public MainViewModel ViewModel { get; }

    private void ModeSelector_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args) =>
        ViewModel.Mode = sender.SelectedItem == TimerItem ? ScheduleMode.Timer : ScheduleMode.AtTime;
}
