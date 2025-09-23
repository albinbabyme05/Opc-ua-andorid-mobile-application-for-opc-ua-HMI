using System;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using CMLGapp.Services;

namespace CMLGapp.Views
{
    public partial class AnalyticsContentPage : BaseContentPage
    {
        // Preference keys
        public const string PrefEmail = "EmailNotifyAddress";
        public const string PrefEnabled = "EmailNotifyEnabled";

        public string SavedEmail { get; set; } =
            Preferences.Get(PrefEmail, string.Empty);

        public AnalyticsContentPage()
        {
            InitializeComponent();
            BindingContext = this;

            EnableSwitch.IsToggled = Preferences.Get(PrefEnabled, false);
        }

        private async void OnEnableToggled(object sender, ToggledEventArgs e)
        {
            Preferences.Set(PrefEnabled, e.Value);
            Status("Email alerts " + (e.Value ? "enabled" : "disabled"));
            await System.Threading.Tasks.Task.CompletedTask;
        }

        private void OnSetClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                Status("Please enter a valid email address.");
                return;
            }

            Preferences.Set(PrefEmail, email);
            SavedEmail = email;
            OnPropertyChanged(nameof(SavedEmail));

            Status("Email saved.");
        }

        private async void OnSendTestClicked(object sender, EventArgs e)
        {
            if (!Preferences.Get(PrefEnabled, false))
            {
                Status("Enable email alerts first.");
                return;
            }
            var email = Preferences.Get(PrefEmail, "");
            if (string.IsNullOrWhiteSpace(email))
            {
                Status("Please set an email first.");
                return;
            }

            try
            {
                await AlarmEmailService.SendAlarmAsync(
                    email,
                    subject: "CMLG – Test alarm mail",
                    htmlBody: @"<p>This is a <b>test</b> alarm email from CMLG.</p>
                                <p>If you can read this, your email setup works ✅</p>");
                Status("Test email sent.");
            }
            catch (Exception ex)
            {
                Status("Failed to send: " + ex.Message);
            }
        }

        private void Status(string text) => StatusLabel.Text = text;
    }
}
