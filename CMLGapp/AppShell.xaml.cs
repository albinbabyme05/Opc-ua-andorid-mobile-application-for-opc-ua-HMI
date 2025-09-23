using CMLGapp.Services;
using CMLGapp.Views;

namespace CMLGapp

{
    public partial class AppShell : Shell
    {
        private static readonly string[] OfflineAllowedTails = { "/mainpage", "/analytics", "/login", "/offline" };

        public AppShell()
        {
            //Routing.RegisterRoute(nameof(LoginContentPage), typeof(CMLGapp.Views.LoginContentPage));
            //Routing.RegisterRoute(nameof(LoadingPage), typeof(CMLGapp.Views.LoadingPage));
            InitializeComponent();

            Routing.RegisterRoute(nameof(OfflineContentPage), typeof(CMLGapp.Views.OfflineContentPage));
            Routing.RegisterRoute(nameof(RegisterationPage), typeof(CMLGapp.Views.RegisterationPage));

            Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
            Routing.RegisterRoute(nameof(ResetCodePage), typeof(ResetCodePage));
            Routing.RegisterRoute(nameof(ResetPasswordPage), typeof(ResetPasswordPage));
            

            Routing.RegisterRoute(nameof(DetailsPage), typeof(CMLGapp.Views.DetailsPage));
            Routing.RegisterRoute(nameof(AlarmContentPage), typeof(CMLGapp.Views.AlarmContentPage));
            Routing.RegisterRoute(nameof(AlarmHistoryContentPage), typeof(CMLGapp.Views.AlarmHistoryContentPage));
            Routing.RegisterRoute(nameof(ProdProcessedContentPage), typeof(CMLGapp.Views.ProdProcessedContentPage));
            Routing.RegisterRoute(nameof(ProdDefectContentpage), typeof(CMLGapp.Views.ProdDefectContentpage));
            Routing.RegisterRoute(nameof(ProdConsumedContentpage), typeof(CMLGapp.Views.ProdConsumedContentpage));
            Routing.RegisterRoute(nameof(DeviceToolMeasuringStatus), typeof(DeviceToolMeasuringStatus));

            Routing.RegisterRoute(nameof(LayoutPage), typeof(CMLGapp.Views.LayoutPage));
            Routing.RegisterRoute(nameof(ManageLayoutPage), typeof(CMLGapp.Views.ManageLayoutPage));

            Routing.RegisterRoute(nameof(CMLGSummaryContentPage), typeof(CMLGapp.Views.CMLGSummaryContentPage));
            Routing.RegisterRoute(nameof(StopReasonContentPage), typeof(CMLGapp.Views.StopReasonContentPage));
            Routing.RegisterRoute(nameof(MenuContentPage), typeof(CMLGapp.Views.MenuContentPage));


            Routing.RegisterRoute(nameof(ProdStatsPage), typeof(CMLGapp.Views.ProdStatsPage));
            Routing.RegisterRoute(nameof(PalletUI), typeof(CMLGapp.Views.PalletUI));
            Routing.RegisterRoute(nameof(AnalyticsContentPage), typeof(CMLGapp.Views.AnalyticsContentPage));

            this.Navigating += AppShell_Navigating;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            DisplayUserName();
        }

        private void AppShell_Navigating(object sender, ShellNavigatingEventArgs e)
        {
            var opc = OpcUaService.Instance;
            if (opc == null || opc.IsConnected) return;

            var route = e.Target?.Location?.OriginalString?.ToLowerInvariant() ?? string.Empty;

            bool allowed = OfflineAllowedTails.Any(t => route.EndsWith(t));
            if (!allowed)
            {
                e.Cancel();
                MainThread.BeginInvokeOnMainThread(async () =>
                    await Application.Current.MainPage.DisplayAlert(
                        "Offline",
                        "App is offline. Only Main and Analytics are available.",
                        "OK"));
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            FlyoutIsPresented = false;
            Preferences.Set("IsLoggedIn", false);
            Preferences.Remove("LoggedInEmail");
            Preferences.Remove("LoggedInPassword");
            //Preferences.Remove("LoggedInFullName");
            lblUserName.Text = "Guest";
            await Shell.Current.GoToAsync("//login");
        }

        //chnage password
        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            FlyoutIsPresented = false;
            await GoToAsync(nameof(ForgotPasswordPage));
        }

        //analytics
        private async void OnProdStatusClicked(object sender, EventArgs e)
        {
            FlyoutIsPresented = false;
            await GoToAsync(nameof(ProdStatsPage));
        }

        //overview
        private async void OnOverviewClicked(object sender, EventArgs e)
        {
            FlyoutIsPresented = false;
            await Shell.Current.GoToAsync("//MainPage");
        }

        //Notification
        private async void OnNotitficationClicked(object sender, EventArgs e)
        {
            FlyoutIsPresented = false;
            await GoToAsync(nameof(AlarmContentPage));
        }

        //menu settings
        private async void OnSettingClicked(object sender, EventArgs e)
        {
            FlyoutIsPresented = false;
            await GoToAsync(nameof(AnalyticsContentPage));
        }

        //service
        private async void OnMenuClicked(object sender, EventArgs e)
        {
            FlyoutIsPresented = false;
            await GoToAsync(nameof(MenuContentPage));
        }
        
        public void UpdateUserName(string fullName)
        {
            lblUserName.Text = fullName;
        }

        private void DisplayUserName()
        {
            string displayName = Preferences.Get("LoggedInFullName", String.Empty);
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                lblUserName.Text = displayName;
            }
            else
            {
                lblUserName.Text = "Guest";
            }

        }

        
        
    }
}

