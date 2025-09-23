namespace CMLGapp.Views;
using CMLGapp.Services;

public partial class StopReasonContentPage : BaseContentPage
{
    private OpcUaService _opcuaService;
    public StopReasonContentPage()
    {
        InitializeComponent();
        _opcuaService = OpcUaService.Instance;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        
        _opcuaService.MonitorNodes("StopReason", async (_) =>
        {
            (int category, int value, string msge, DateTime[] dateTime) = await _opcuaService.LoadStopReasonAsync();
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblMessage.Text = $"{msge}";
                lblStopCategory.Text = $"{category}";
                lblStopValue.Text = $"{value}" ;
                lblStopDateTime.Text = $"{dateTime.FirstOrDefault().ToString("HH:mm:ss") ?? "N/A"}";
            });
        });
    }
}