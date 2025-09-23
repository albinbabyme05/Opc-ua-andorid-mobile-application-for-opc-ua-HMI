using opcUa_Connecter.Services;
using CMLGapp.Views;
using CMLGapp.Services;
using CMLGapp.Models;
namespace CMLGapp
{
    public partial class App : Application
    {
        
        private static string endpointUrl = "opc.tcp://10.0.39.14:4840";
        private OpcUaService _opcUaService;

        private AlarmNotificationService _alarmNotificationService;
        private AppShell _appShell;

        public App()
        {
            
            InitializeComponent();
            MainPage = new AppShell();

            Dispatcher.Dispatch(async () =>
            {
                try
                {
                    await InitializeAppAsync();
                    await NavigateToStartAsync();   
                    //await Task.Yield();             
                       
                }
                catch (Exception ex)
                {
                   Console.WriteLine("App startup error: " + ex);
                    Preferences.Set("IsLoggedIn", false);
                    await Shell.Current.GoToAsync("//login", false);
                }

            });
        }

        private async Task NavigateToStartAsync()
        {
            bool isLoggedIn = Preferences.Get("IsLoggedIn", false);
            //to avoid any flicker
            if (isLoggedIn && HasValidCachedUser())
                await Shell.Current.GoToAsync("//MainPage", false);
            else
                await Shell.Current.GoToAsync("//login", false);
        }

        private async Task InitializeAppAsync()
        {
            _opcUaService = OpcUaService.Instance;
            _alarmNotificationService = new AlarmNotificationService();

            var connected = await _opcUaService.StartAppAsync();
            if (connected)
            {
                await _alarmNotificationService.StartMonitoringAsync();
            }
            //reconnect running in background
            _opcUaService.StartAutoReconnect();
        }

        private bool HasValidCachedUser()
        {
            var email = Preferences.Get("LoggedInEmail", string.Empty);
            var isLoggedIn = Preferences.Get("IsLoggedIn", false);
            return isLoggedIn && !string.IsNullOrWhiteSpace(email);
        }
    }
}
