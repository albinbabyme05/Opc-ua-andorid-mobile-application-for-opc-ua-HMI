
using CMLGapp.Services;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;

using CMLGapp.Helpers;

namespace CMLGapp.Views;

public partial class AlarmContentPage : BaseContentPage
{
    private OpcUaService _opcuaService;
    private HashSet<int> _notifiedAlarmIds = new();


    public AlarmContentPage()
	{
		InitializeComponent();
        _opcuaService = OpcUaService.Instance;
    }
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _opcuaService.StartAppAsync();

        // Initial load
        await LoadAndDisplayAlarm();

        // Real-time updates
        _opcuaService.MonitorNodes("Alarm", async (_) =>
        {
            await LoadAndDisplayAlarm();
        });
    }
    // dynamicly fetching card w.r.to changes 
    private async Task LoadAndDisplayAlarm()
    {
        var history = await _opcuaService.LoadAlarmAsync();

        // show one empty card and return
        if (history == null || history.Count == 0)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                AlarmContainer.Children.Clear();
                AlarmContainer.Children.Add(ValidationHelper.BuildEmptyCard());
            });
           return;
        }

        var sln = new ErrorCodeHandle();
        await sln.LoadErrorCodesAsync();

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            AlarmContainer.Children.Clear();

            bool isAlarmEmpty = false;

            foreach (var item in history)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Message))
                    continue;

                string result = sln.GetSolution(item.ID, item.Value, item.Category);
                string errorName = sln.GetErrorName(item.ID, item.Value, item.Category);

                var card = new Frame
                {
                    BackgroundColor = Color.FromArgb("#28323C"),
                    BorderColor = Color.FromArgb("#28323C"),
                    CornerRadius = 15,
                    Padding = 5,
                    Content = new VerticalStackLayout
                    {
                        Children =
                    {
                        new Label { Text = $"🔔 {errorName} Alarm", FontSize = 16, TextColor = Color.FromArgb("#8C96A0") },
                        new BoxView { HeightRequest = 1, BackgroundColor = Colors.Gray, HorizontalOptions = LayoutOptions.Fill, Margin = new Thickness(0,5) },
                        new Label { Text = item.Message, FontSize = 16, TextColor = Color.FromArgb("#FA7D7D") }, //red
                        new Label { Text = $"Occurred at : {(item.DateTime != null && item.DateTime.Any() ? item.DateTime.FirstOrDefault().ToString() : "N/A")}",
                            FontSize = 14, Padding = 3, TextColor = Color.FromArgb("#FA7D7D") },
                        new Label { Text = "Solution", FontSize = 16, TextColor = Color.FromArgb("#8C96A0") },
                        new BoxView { HeightRequest = 1, BackgroundColor = Colors.Gray, HorizontalOptions = LayoutOptions.Fill, Margin = new Thickness(0,5) },
                        new Label { Text = result, FontSize = 16, Padding = 3, TextColor = Color.FromArgb("#64C87D") }, //green
                    }
                    }
                };

                AlarmContainer.Children.Add(card);
                isAlarmEmpty = true;
            }

            //show empty card
            if (!isAlarmEmpty)
            {
                AlarmContainer.Children.Add(ValidationHelper.BuildEmptyCard());
            }
        });

        
    }



}