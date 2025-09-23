using CMLGapp.ViewModels;

namespace CMLGapp.Views;

public partial class ManageLayoutPage : BaseContentPage
{
	public ManageLayoutPage()
	{
        InitializeComponent();
        BindingContext = new MainLayoutViewModel();
    }
}