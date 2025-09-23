using CMLGapp.Services;

namespace CMLGapp.Views;

public partial class ResetCodePage : ContentPage, IQueryAttributable
{
    public string Email;
    public ResetCodePage()
	{
		InitializeComponent();
	}
    // Called when navigation passes query parameters
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.ContainsKey("Email"))
        {
            Email = query["Email"] as string;
        }
    }

    private async void OnVerifyCodeClicked(object sender, EventArgs e)
    {
        string enteredCode = codeEntry.Text?.Trim();
        
        if (ResetTokenService.VerifyCode(Email, enteredCode))
        {
            //Console.WriteLine($">>>>>>>>>>>>>>>>>>>Code generated for {Email}>>>>>>>>>>>>:<<<<<<< {enteredCode}<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
            ResetTokenService.RemoveCode(Email);
            await Shell.Current.GoToAsync(nameof(ResetPasswordPage), true, new Dictionary<string, object>
            {
                { "Email", Email }
            });
        }
        else
        {
            await DisplayAlert("Invalid", "Code does not match or expired", "OK");
        }
    }
}