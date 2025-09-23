using CMLGapp.Services;

namespace CMLGapp.Views;

public partial class OfflineContentPage : ContentPage
{
    
    private OpcUaService _opcUaService;

    public OfflineContentPage()
	{
		InitializeComponent();
        _opcUaService = OpcUaService.Instance;

    }
    private void OnTryToReconnect(object sender, EventArgs e)
    {
        NavigateToPages();
    }

    private async void NavigateToPages()
    {

        bool connected = await _opcUaService.StartAppAsync();
        if (connected)
        {
            Shell.Current.GoToAsync("//login");
        }
        else
        {
            Shell.Current.GoToAsync("//offline");
        }


    }

}