using CMLGapp.ViewModels;
using Microsoft.Maui.Controls;

namespace CMLGapp.Views;

public partial class MenuContentPage : ContentPage
{
    private readonly MainLayoutViewModel viewModel;

    public MenuContentPage()
    {
        InitializeComponent();
        viewModel = new MainLayoutViewModel();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.ReloadLayout();
    }

    // Responsive span based on actual width (handles portrait/landscape & tiny phones)
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        // Safety: ensure named layout exists
        if (MenuGridLayout == null) return;

        // Simple breakpoints (tweak as you like)
        // < 380px: 1 column (very narrow devices)
        // < 600px: 2 columns (typical portrait phones)
        // else:    3 columns (wide phones landscape / tablets / desktop)
        if (width < 380)
            MenuGridLayout.Span = 1;
        else if (width < 600)
            MenuGridLayout.Span = 2;
        else
            MenuGridLayout.Span = 3;
    }

    private async void OnManageLayoutClicked(object sender, EventArgs e)
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync(nameof(ManageLayoutPage));
        else
            await DisplayAlert("Error", "Shell.Current is null", "OK");
    }

    private async void OnViewMoreClicked(object sender, EventArgs e)
    {
        if (Shell.Current != null)
            await Shell.Current.GoToAsync(nameof(DetailsPage));
        else
            await DisplayAlert("Error", "Shell.Current is null", "OK");
    }

    //private async void OnLogoutClicked(object sender, EventArgs e)
    //{
    //    Preferences.Set("IsLoggedIn", false);
    //    Preferences.Remove("LoggedInEmail");
    //    Preferences.Remove("LoggedInPassword");
    //    await Shell.Current.GoToAsync("//login");
    //}
}
