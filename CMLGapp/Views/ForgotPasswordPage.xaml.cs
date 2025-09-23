using CMLGapp.Services;
using System.Text.Json;
using CMLGapp.Models;
namespace CMLGapp.Views;

public partial class ForgotPasswordPage : ContentPage
{
    private readonly string filePath;
    public ForgotPasswordPage()
	{
		InitializeComponent();
        filePath = Path.Combine(FileSystem.AppDataDirectory, "user_data.json");
    }

    private async void OnSendCodeClicked(object sender, EventArgs e)
    {
        string email = emailEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(email))
        {
            await DisplayAlert("Error", "Please enter your registered email.", "OK");
            return;
        }

        if (!File.Exists(filePath))
        {
            await DisplayAlert("Error", "User data not found.", "OK");
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            var users = JsonSerializer.Deserialize<List<UserModel>>(json) ?? new();

            var matchedUser = users.FirstOrDefault(u => u.Email == email);
            if (matchedUser != null)
            {
                string code = ResetTokenService.GenerateCode(email);
                await EmailService.SendResetCodeAsync(email, code);

                await Shell.Current.GoToAsync(nameof(ResetCodePage), true, new Dictionary<string, object>
                {
                    { "Email", email }
                });
            }
            else
            {
                await DisplayAlert("Error", "Email not found in system.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to load user data: {ex.Message}", "OK");
        }
    }



}