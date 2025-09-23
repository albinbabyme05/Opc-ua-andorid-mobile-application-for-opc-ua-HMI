using System.Runtime.InteropServices.Marshalling;

namespace CMLGapp.Views;

public partial class DetailsPage : ContentPage
{
	public DetailsPage()
	{
		InitializeComponent();
	}
    

    private async void OnSummaryTapped(object sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync(nameof(CMLGSummaryContentPage));
        }
        else
        {
            await DisplayAlert("Error", "Shell.current is null", "ok");
        }
    }
    private async void OnAlarmHistroyTapped(object sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync(nameof(AlarmHistoryContentPage));
        }
        else
        {
            await DisplayAlert("Error", "Shell.current is null", "ok");
        }
    }

    private async void OnAlarmTapped(object sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync(nameof(AlarmContentPage));
        }
        else
        {
            await DisplayAlert("Error", "Alarm content is null", "ok");
        }
    }

    private async void OnProdProcessedTapped(object sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync(nameof(ProdProcessedContentPage));
        }
        else
        {
            await DisplayAlert("Error", "Product measured is null", "ok");
        }

    }

    private async void OnChartsTapped(object sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync(nameof(ProdDefectContentpage));
        }
        else
        {
            await DisplayAlert("Error", "Charts can not shown", "ok");
        }

    }
    
    private async void OnLayoutTapped(object sender, EventArgs e)
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync(nameof(LayoutPage));
        }
        else
        {
            await DisplayAlert("Error", "Layout can not shown", "ok");
        }

    }

}