using CMLGapp.ViewModels;

namespace CMLGapp.Views;

public partial class LayoutPage : ContentPage
{
    private MainLayoutViewModel viewModel;

    public LayoutPage()
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
}