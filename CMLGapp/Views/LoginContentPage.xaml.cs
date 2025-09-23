namespace CMLGapp.Views;
using System.Text.Json;
using CMLGapp.Helpers;
using CMLGapp.Models; 


public partial class LoginContentPage : ContentPage
{
    private readonly AlarmNotificationService _alarmNotificationService = new();

    public LoginContentPage()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        string savedEmail = Preferences.Get("LoggedInEmail", string.Empty);
        userName.Text = savedEmail;
        password.Text = string.Empty;
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        string userEntry = userName.Text?.Trim();
        string passwordEntry = password.Text?.Trim();
        await ValidateUser(userEntry, passwordEntry);
    }

    private async Task ValidateUser(string userName, string password)
    {
        if (userName == "zoller" && password == "zoller")
        {
            //get username in flyout
            Preferences.Set("IsLoggedIn", true);
            Preferences.Set("LoggedInEmail", "zoller");
            Preferences.Set("LoggedInFullName", "Admin");

            await _alarmNotificationService.StartMonitoringAsync();
            await Shell.Current.GoToAsync("//MainPage", false);
            return;
        }

        string filePath = Path.Combine(FileSystem.AppDataDirectory, "user_data.json");

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                var users = JsonSerializer.Deserialize<List<UserModel>>(json);

                if (users != null)
                {
                    string hashedPassword = ValidationHelper.HashPassword(password);
                    var match = users.FirstOrDefault(u =>
                        u.Email == userName && u.PasswordHash == hashedPassword);

                    if (match != null)
                    {
                        //auto login
                        Preferences.Set("IsLoggedIn", true);
                        Preferences.Set("LoggedInEmail", match.Email);

                        //get username in Flyout
                        Preferences.Set("LoggedInFullName", match.FullName);

                        if (Shell.Current is AppShell appShell)
                        {
                            appShell.UpdateUserName(match.FullName);
                        }

                        await _alarmNotificationService.StartMonitoringAsync();
                        await Shell.Current.GoToAsync("//MainPage", false);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("⚠️ Error", $"Failed to read user data: {ex.Message}", "OK");
                return;
            }
        }

        await DisplayAlert("⚠️ Error", "Invalid username or password", "OK");
    }

    private void OnPasswordChanged(object sender, TextChangedEventArgs e)
    {
        ValidationHelper.ShowToggleIfNotEmpty(password, togglePasswordVisibility);
    }

    private void OnTogglePasswordVisibilityClicked(object sender, EventArgs e)
    {
        ValidationHelper.TogglePasswordVisibility(togglePasswordVisibility, password);
    }

    private async void OnRegisterTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(RegisterationPage));
    }

    private async void OnForgotPasswordTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ForgotPasswordPage));
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

       
        if (width < 400)
        {
            lblHeader.FontSize = 32;     
        }
        else if (width < 700)
        {
            lblHeader.FontSize = 38;    
        }
        else
        {
            lblHeader.FontSize = 50;     
        }
    }


}