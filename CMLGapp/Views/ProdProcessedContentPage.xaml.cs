using CMLGapp.Services;

namespace CMLGapp.Views;

public partial class ProdProcessedContentPage : BaseContentPage
{
    private OpcUaService _opcuaService;
	public ProdProcessedContentPage()
	{
		InitializeComponent();
        _opcuaService = OpcUaService.Instance;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _opcuaService.StartAppAsync();

        // Initial load
        await LoadAndDisplayProdProcessed();
        await LoadAndDisplayProdConsumed();
        await LoadAndDisplayProdDefect();

        // Real-time updates
        // processed
        _opcuaService.MonitorNodes("ProdProcessedCount", async (_) =>
        {
            await LoadAndDisplayProdProcessed();
        });
        //consumed
        _opcuaService.MonitorNodes("ProdConsumedCount", async (_) =>
        {
            await LoadAndDisplayProdConsumed();
        });
        //defect
        _opcuaService.MonitorNodes("ProdDefectiveCount", async (_) =>
        {
            await LoadAndDisplayProdDefect();
        });
    }
    private async Task LoadAndDisplayProdProcessed()
    {
        var prodProcessed = await _opcuaService.LoadProdProcessedAsync();
        if (prodProcessed != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var item in prodProcessed)
                {
                    lblProdProcessedMsg.Text = $" {item.Name}";
                    lblProdProcessedCount.Text = $"Count: {item.Count}";
                    lblProdProcessedAccCount.Text = $"Acccount : {item.AccCount}";

                }
            });
        }
    }
    private async Task LoadAndDisplayProdConsumed()
    {
        var prodConsumed = await _opcuaService.LoadProdConsumedAsync();
        if (prodConsumed != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var item in prodConsumed)
                {
                    lblProdConsumedMsg.Text = $" {item.Name}";
                    lblProdConsumedCount.Text = $"Count: {item.Count}";
                    lblProdConsumedAccCount.Text = $"Acccount : {item.AccCount}";
                    
                }
            });
        }
    }

    private async Task LoadAndDisplayProdDefect()
    {
        var prodDefect = await _opcuaService.LoadProdDefectAsync();
        if (prodDefect != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var item in prodDefect)
                {
                    lblProdDefectMsg.Text = $" {item.Name}";
                    lblProdDefectCount.Text = $"Count: {item.Count}";
                    lblProdDefectAccCount.Text = $"Acccount : {item.AccCount}";

                }
            });
        }
    }



    //enc class
}