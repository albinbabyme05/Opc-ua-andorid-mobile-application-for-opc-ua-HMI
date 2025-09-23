using CMLGapp.Helpers;
using CMLGapp.Services;
using Plugin.LocalNotification.EventArgs;

namespace CMLGapp.Views;

public partial class AlarmHistoryContentPage : BaseContentPage
{
    private OpcUaService _opcuaService;
    public AlarmHistoryContentPage()
    {
        InitializeComponent();
        _opcuaService = OpcUaService.Instance;
    }

    // viewing chnages in the screen
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await _opcuaService.StartAppAsync();

        // Initial load
       await LoadAndDisplayAlarmHistory();

        // Real-time updates
        _opcuaService.MonitorNodes("AlarmHistory", async (_) =>
        {
            await LoadAndDisplayAlarmHistory();
        });
    }


    // dynamicly fetching card w.r.to changes 
    private async Task LoadAndDisplayAlarmHistory()
    {
        var history = await _opcuaService.LoadAlarmHistoryAsync();

        // show one empty card and return
        if (history == null || history.Count == 0)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                AlarmHistoryContainer.Children.Clear();
                AlarmHistoryContainer.Children.Add(ValidationHelper.BuildEmptyCard());
            });
            return;
        }

        var sln = new ErrorCodeHandle();
        await sln.LoadErrorCodesAsync();
        MainThread.BeginInvokeOnMainThread(() =>
        {
            //clear the previous loaded cards
            AlarmHistoryContainer.Children.Clear();
            bool isAlarmHistroyEmpty = false;

            //create card by looping through 
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
                            new BoxView{ HeightRequest = 1, BackgroundColor = Colors.Gray,HorizontalOptions = LayoutOptions.Fill,
                                Margin = new Thickness(0, 5)},
                            new Label { Text = $"{item.Message}", FontSize = 16, TextColor = Color.FromArgb("#FA7D7D") }, //red
                            new Label { Text = $"Occured at: {(item.DateTime != null && item.DateTime.Any() ?item.DateTime?.FirstOrDefault().ToString() : "N/A")}",
                                FontSize = 14, Padding=3, TextColor = Color.FromArgb("#FA7D7D")},

                            new Label { Text = $"Solution", FontSize = 16 ,Padding = 3, TextColor = Color.FromArgb("#8C96A0") },
                            new BoxView{ HeightRequest = 1, BackgroundColor = Colors.Gray,HorizontalOptions = LayoutOptions.Fill,
                                Margin = new Thickness(0, 5)},
                            new Label { Text = $" {result}", FontSize = 16 ,Padding = 3, TextColor = Color.FromArgb("#64C87D") }, //green
                            new Label { Text = $"Solved at: {(item.AckDateTime != null && item.AckDateTime.Any() ? item.AckDateTime.First().ToString() : "N/A")}",
                                FontSize = 14 ,Padding=3, TextColor= Color.FromArgb("#64C87D") }
                        }
                    }
                };
                AlarmHistoryContainer.Children.Add(card);
                isAlarmHistroyEmpty = true;
            }
            
            //show empty card
            if (!isAlarmHistroyEmpty)
            {
                AlarmHistoryContainer.Children.Add(ValidationHelper.BuildEmptyCard());
            }
        });
    }


}