using CMLGapp.Services;

namespace CMLGapp.Views;

public class BaseContentPage : ContentPage
{
    protected OpcUaService OpcUaService { get; }

    public BaseContentPage()
    {

        //apply template
        if (Application.Current?.Resources.TryGetValue("AppPageTemplate", out var tpl) == true)
            ControlTemplate = (ControlTemplate)tpl;

        //opczua instance
        OpcUaService = OpcUaService.Instance;   
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = OpcUaService.StartAppAsync();       
    }
}
