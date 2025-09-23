using CMLGapp.Models;
using System.Text;
using System.Text.Json;
namespace CMLGapp.Views;

using CMLGapp.Helpers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

[QueryProperty(nameof(Email), "Email")]
public partial class ResetPasswordPage : ContentPage
{
    public string Email { get; set; }

    private readonly string filePath = Path.Combine(FileSystem.AppDataDirectory, "user_data.json");
    public ResetPasswordPage()
	{
		InitializeComponent();
	}
    private async void OnResetClicked(object sender, EventArgs e)
    {
        string newPass = passwordEntry.Text?.Trim();
        string confirm = confirmEntry.Text?.Trim();

        if (newPass != confirm)
        {
            await DisplayAlert("Mismatch", "Passwords do not match", "OK");
            return;
        }

        try
        {
            if (!File.Exists(filePath))
            {
                await DisplayAlert("Error", "User data file not found.", "OK");
                return;
            }

            string json = File.ReadAllText(filePath);
            var users = JsonSerializer.Deserialize<List<UserModel>>(json);

            if (users == null)
            {
                await DisplayAlert("Error", "No user records found.", "OK");
                return;
            }

            var user = users.FirstOrDefault(u => u.Email == Email);
            if (user != null)
            {
                user.PasswordHash = ValidationHelper.HashPassword(newPass);
                string updatedJson = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, updatedJson);

                await DisplayAlert("Success", "Password reset successfully", "OK");
                await Shell.Current.GoToAsync(nameof(LoginContentPage));
            }
            else
            {
                await DisplayAlert("Error", "User not found.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Failed to reset password: {ex.Message}", "OK");
        }
    }

    private void OnPasswordChanged(object sender, TextChangedEventArgs e)
    {
        ValidationHelper.ShowToggleIfNotEmpty(passwordEntry, togglePasswordVisibility);

        if (string.IsNullOrWhiteSpace(passwordEntry.Text))
        {
            _ = ValidationHelper.AnimateValidation(passwordValidation, false);
            _ = ValidationHelper.AnimateValidation(confirmPasswordValidation, false);
            return;
        }

        bool isValid = ValidationHelper.IsStrongPassword(passwordEntry.Text);
        _ = ValidationHelper.AnimateValidation(passwordValidation, !isValid, "Password must include upper, lower, digit & special char, min 8 chars");

        OnConfirmPasswordChanged(null, null);
    }


    private void OnConfirmPasswordChanged(object sender, TextChangedEventArgs e)
    {
        ValidationHelper.ShowToggleIfNotEmpty(confirmEntry, toggleConfirmPasswordVisibility);

        if (string.IsNullOrWhiteSpace(confirmEntry.Text))
        {
            _ = ValidationHelper.AnimateValidation(confirmPasswordValidation, false);
            return;
        }

        bool isMatch = passwordEntry.Text == confirmEntry.Text;
        _ = ValidationHelper.AnimateValidation(confirmPasswordValidation, !isMatch, "Passwords do not match.");
    }


    private void OnTogglePasswordVisibilityClicked(object sender, EventArgs e)
    {
        ValidationHelper.TogglePasswordVisibility(togglePasswordVisibility, passwordEntry);
    }

    private void OnToggleConfirmPasswordVisibilityClicked(object sender, EventArgs e)
    {
        ValidationHelper.TogglePasswordVisibility(toggleConfirmPasswordVisibility, confirmEntry);
    }
}
