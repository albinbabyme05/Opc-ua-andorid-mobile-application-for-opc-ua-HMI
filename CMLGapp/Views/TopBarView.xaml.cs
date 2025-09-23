using CMLGapp.Services;

namespace CMLGapp.Views;

public partial class TopBarView : ContentView
{
    public static readonly BindableProperty PageTitleProperty =
        BindableProperty.Create(nameof(PageTitle), typeof(string), typeof(TopBarView), default(string));

    public string PageTitle
    {
        get => (string)GetValue(PageTitleProperty);
        set => SetValue(PageTitleProperty, value);
    }

    private bool isLabelfadein = true;
    private OpcUaService _opcuaService;
    private bool _initialized;  // 

    public TopBarView()
    {
        InitializeComponent();
        lblDateTime.Text = " / " + DateTime.Now.ToString("dd.MM.yyyy");
        LabelFadeInOut(lblMachineStatus, machineStatusColor);
    }

    
    public void Initialize(OpcUaService opcuaService)
    {
        if (_initialized || opcuaService == null) return;
        _opcuaService = opcuaService;
        _initialized = true;
        TryHookOpcUaMonitors();
    }

    
    protected override void OnParentSet()
    {
        base.OnParentSet();
        if (Parent != null && !_initialized)
        {
            Initialize(OpcUaService.Instance);  
        }
    }

    private void TryHookOpcUaMonitors()
    {
        try
        {
            lblPlcTime.Text = DateTime.Now.ToString("HH:mm:ss");
            SetUnitModeCurrent(_opcuaService?.IsConnected == true ? 1 : 0);
            SetStateCurrent(4);

            if (_opcuaService == null) return;

            _opcuaService.MonitorPlcDateTime(time =>
                MainThread.BeginInvokeOnMainThread(() => lblPlcTime.Text = time));

            _opcuaService.MonitorSingleNodeValue("UnitModeCurrent", mode =>
                MainThread.BeginInvokeOnMainThread(() => SetUnitModeCurrent(mode)));

            _opcuaService.MonitorSingleNodeValue("StateCurrent", state =>
                MainThread.BeginInvokeOnMainThread(() => SetStateCurrent(state)));
        }
        catch { /* no-op */ }
    }

    private void SetUnitModeCurrent(int mode)
    {
        var (unitMode, clr) = _opcuaService.MachineUnitMode(mode);
        lblMachineMode.Text = unitMode.ToUpperInvariant();
        lblMachineMode.TextColor = clr;
    }

    private void SetStateCurrent(int status)
    {
        var (txtStatus, svgImage, clr) = _opcuaService.MachineStateCurrent(status);
        lblMachineStatus.Text = txtStatus.ToUpperInvariant();
        lblMachineStatus.TextColor = clr;
        machineStatusColor.Source = svgImage;
        bvStart.Color = Colors.Gray;
        bvRun.Color = Colors.Gray;
        bvStop.Color = Colors.Gray;
        switch (status)
        {
            case 0:
                bvStop.Color = Colors.Red;
                break;
            case 1:
                bvStart.Color = Colors.Orange;
                break;
            case 2:
                bvStart.Color = Colors.Orange;
                break;
            case 3:
                bvStart.Color = Colors.Orange;
                break;

            case 4:
                bvRun.Color = Colors.Green;
                break;
            case 5:
                bvStart.Color = Colors.Orange;
                break;
            case 6:
                bvStop.Color = Colors.Red;
                break;
            case 7:
                bvRun.Color = Colors.Blue;
                break;
            case 8:
                bvStart.Color = Colors.Orange;
                break;
            case 9:
                bvStop.Color = Colors.Red;
                break;
            case 10:
                bvRun.Color = Colors.Blue;
                break;
            case 11:
                bvRun.Color = Colors.Blue;
                break;
            case 12:
                bvStop.Color = Colors.Red;
                break;
            case 13:
                bvStop.Color = Colors.Red;
                break;
            case 14:
                bvStop.Color = Colors.Red;
                break;
            case 15:
                bvStop.Color = Colors.Red;
                break;
            case 16:
                bvRun.Color = Colors.Blue;
                break;
        }
    }

    private void OnHamburgerTapped(object sender, EventArgs e)
    {
        if (Application.Current?.MainPage is Shell shell)
            shell.FlyoutIsPresented = true;
    }

    private async void LabelFadeInOut(Label label, Image img)
    {
        while (isLabelfadein)
        {
            await Task.WhenAll(label.FadeTo(0, 2000), img.FadeTo(0, 2000));
            await Task.Delay(1000);
            await Task.WhenAll(label.FadeTo(1, 2000), img.FadeTo(1, 2000));
            await Task.Delay(1000);
        }
    }
}
