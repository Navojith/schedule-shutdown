using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using ScheduleShutdown.App.Services;
using ScheduleShutdown.App.ViewModels;

namespace ScheduleShutdown.App.Views;

/// <summary>Small always-on-top window for the last 60 s, shown even when the main window is in the tray.</summary>
public sealed partial class FinalCountdownWindow : Window
{
    public FinalCountdownWindow(CountdownViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        TitleIcon.Source = new BitmapImage(new Uri(AppAssets.IconPngPath));
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
        }

        WindowSizing.Resize(this, 380, 340);
        WindowSizing.Center(this);

        ViewModel.Finished += (_, _) => Close();
        Closed += (_, _) => ViewModel.Dispose();
        CancelButton.Loaded += (_, _) => CancelButton.Focus(FocusState.Programmatic);
    }

    public CountdownViewModel ViewModel { get; }
}
