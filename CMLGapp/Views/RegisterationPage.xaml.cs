using System.Text.Json;
using System.Text.Json.Serialization;
namespace CMLGapp.Views;
using CMLGapp.Models;
using CMLGapp.Helpers;



public partial class RegisterationPage : ContentPage
{
    private readonly string filePath;
    public RegisterationPage()
    {
        InitializeComponent();
        filePath = Path.Combine(FileSystem.AppDataDirectory, "user_data.json");
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyResponsiveLayout(Width, Height);
    }
    private async void OnSignUpClicked(object sender, EventArgs e)
    {
        var name = fullName.Text?.Trim();
        var emailText = email.Text?.Trim();
        var pass = password.Text?.Trim();
        var confirm = confirmPassword.Text?.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(emailText) ||
            string.IsNullOrWhiteSpace(pass) || string.IsNullOrWhiteSpace(confirm))
        {
            return;
        }

        if (!ValidationHelper.IsValidUserName(emailText))
        {
            await DisplayAlert("Error", "Username can only contain letters, numbers, _, @, .", "OK");
            return;
        }

        if (!ValidationHelper.IsStrongPassword(pass))
        {
            await DisplayAlert("Error", "Password must include uppercase, lowercase, digit, special character, at least 8 characters.", "OK");
            return;
        }

        if (pass != confirm)
        {
            await DisplayAlert("Error", "Passwords do not match.", "OK");
            return;
        }

        List<UserModel> users = LoadUsers();

        if (users.Any(u => u.Email.Equals(emailText, StringComparison.OrdinalIgnoreCase)))
        {
            await DisplayAlert("Error", "This email/username is already registered.", "OK");
            return;
        }

        var user = new UserModel
        {
            UUID = Guid.NewGuid().ToString(),
            FullName = name,
            Email = emailText,
            PasswordHash = ValidationHelper.HashPassword(pass)
        };

        users.Add(user);

        //  to prevent json corruption
        await SafeWriteUsersAsync(users);  

        await DisplayAlert("Success", "Registration complete!", "OK");
        await Navigation.PopAsync();
    }

    //  JSON save
    private async Task SafeWriteUsersAsync(List<UserModel> users)
    {
        try
        {
            string json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });

            var tmp = filePath + ".tmp";  
            await File.WriteAllTextAsync(tmp, json); 
                                                     
            if (File.Exists(filePath)) File.Delete(filePath); 
            File.Move(tmp, filePath);                         
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not save user data: {ex.Message}", "OK");
        }
    }

    private List<UserModel> LoadUsers()
    {
        try
        {
            // Validate the json file
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json)) return new List<UserModel>();
                return JsonSerializer.Deserialize<List<UserModel>>(json) ?? new List<UserModel>();
            }
        }
        catch (Exception ex)
        {
            try
            {
                var bak = filePath + ".bak";
                if (File.Exists(bak)) File.Delete(bak);
                File.Move(filePath, bak);
            }
            catch { /* ignore */ }

            DisplayAlert("Warning", "User store was corrupted and has been reset.", "OK"); //
        }

        return new List<UserModel>();
    }

    private async void OnLoginTapped(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private void OnUsernameChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(email.Text))
        {
            usernameCheck.IsVisible = false;
            _ = ValidationHelper.AnimateValidation(usernameValidation, false);
            return;
        }

        bool isValid = ValidationHelper.IsValidUserName(email.Text);
        usernameCheck.IsVisible = isValid;
        _ = ValidationHelper.AnimateValidation(usernameValidation, !isValid, "Username can only contain letters, numbers, and _");
    }

    private void OnPasswordChanged(object sender, TextChangedEventArgs e)
    {
        ValidationHelper.ShowToggleIfNotEmpty(password, togglePasswordVisibility);

        // Hide the warning label, if the field entry is empty
        if (string.IsNullOrWhiteSpace(password.Text))
        {
            _ = ValidationHelper.AnimateValidation(passwordValidation, false);
            _ = ValidationHelper.AnimateValidation(confirmPasswordValidation, false);
            return;
        }

        bool isValid = ValidationHelper.IsStrongPassword(password.Text);
        _ = ValidationHelper.AnimateValidation(passwordValidation, !isValid, "Password must include upper, lower, digit & special char, min 8 chars");

        OnConfirmPasswordChanged(null, null); 
    }


    private void OnConfirmPasswordChanged(object sender, TextChangedEventArgs e)
    {
        ValidationHelper.ShowToggleIfNotEmpty(confirmPassword, toggleConfirmPasswordVisibility);

        if (string.IsNullOrWhiteSpace(confirmPassword.Text))
        {
            _ = ValidationHelper.AnimateValidation(confirmPasswordValidation, false);
            return;
        }

        bool isMatch = password.Text == confirmPassword.Text;
        _ = ValidationHelper.AnimateValidation(confirmPasswordValidation, !isMatch, "Passwords do not match.");
    }



    private void OnTogglePasswordVisibilityClicked(object sender, EventArgs e)
    {
        ValidationHelper.TogglePasswordVisibility(togglePasswordVisibility, password);
    }

    private void OnToggleConfirmPasswordVisibilityClicked(object sender, EventArgs e)
    {
        ValidationHelper.TogglePasswordVisibility(toggleConfirmPasswordVisibility, confirmPassword);
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        // Re-apply on rotation / resize
        ApplyResponsiveLayout(width, height);
    }

    private void ApplyResponsiveLayout(double width, double height)
    {
        bool isPhone = DeviceInfo.Idiom == DeviceIdiom.Phone;
        bool isPortrait = height > width;

    }

}