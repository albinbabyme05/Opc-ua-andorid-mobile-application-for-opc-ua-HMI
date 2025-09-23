using CMLGapp.Services;
using Microsoft.Maui.Storage;

namespace CMLGapp.Views;

public partial class LoadingPage : ContentPage
{
    private bool _started;  
    private CancellationTokenSource _cts;
    private int _rotationDurationMs = 2500;

    public LoadingPage()
	{
		InitializeComponent();
        
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (_started) return;
        _started = true;

        _cts = new CancellationTokenSource();

        await Task.Yield();

     
        _ = RotateLogoAsync(_cts.Token);
        _ = FillProgressAsync(_cts.Token);

        // heavy work off UI thread
        await Task.Run(async () =>
        {
            var opc = OpcUaService.Instance;
            bool connected = await opc.StartAppAsync();
            if (connected)
            {
                var alarm = new AlarmNotificationService();
                await alarm.StartMonitoringAsync();
            }
        });


        // Navigate without Shell animation
        var target = Preferences.Get("IsLoggedIn", false) ? "//MainPage" : "//login";
        await Shell.Current.GoToAsync(target, false);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // stop animations when leaving
        _cts?.Cancel(); 
    }

    private async Task RotateLogoAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await rotatingImage.RotateTo(360, (uint)_rotationDurationMs, Easing.Linear);
            rotatingImage.Rotation = 0;
        }
    }

    private async Task FillProgressAsync(CancellationToken token)
    {
        ProgressBar.Progress = 0;
        while (!token.IsCancellationRequested && ProgressBar.Progress < 1.0)
        {
            ProgressBar.Progress += 0.01;
            await Task.Delay(80, token);
        }
    }

}

