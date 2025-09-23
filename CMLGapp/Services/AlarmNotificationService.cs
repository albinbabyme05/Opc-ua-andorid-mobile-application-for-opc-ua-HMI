using CMLGapp.Services;
using CMLGapp.Views;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Dispatching;

public class AlarmNotificationService
{
    private readonly OpcUaService _opcuaService;
    private readonly ErrorCodeHandle _errorCode = new();

    // Dedupe sets
    private readonly HashSet<int> _notifiedAlarmIds = new();   
    private readonly HashSet<string> _emailedAlarmKeys = new(); 

    // Reentrancy/dup-subscription guards
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _subscribed = false;

    // Preference keys (must match settings page)
    private const string PrefEmail = "EmailNotifyAddress";
    private const string PrefEnabled = "EmailNotifyEnabled";

    public AlarmNotificationService()
    {
        _opcuaService = OpcUaService.Instance;
        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;
    }

    public async Task StartMonitoringAsync()
    {
        bool connected = await _opcuaService.StartAppAsync();
        if (!connected)
        {
            Console.WriteLine("[AlarmMonitor] OPC UA not connected; monitoring not started.");
            return;
        }

        await _errorCode.LoadErrorCodesAsync();

        if (!_subscribed)
        {
            _opcuaService.MonitorNodes("Alarm", async (_) =>
            {
                await CheckAndNotifyAlarmsAsync();
            });
            _subscribed = true;
        }

       // check alarm
        await CheckAndNotifyAlarmsAsync();

        Console.WriteLine("✅ Alarm monitoring started (local notifications + email).");
    }

    private async Task CheckAndNotifyAlarmsAsync()
    {
        if (!await _gate.WaitAsync(0))
            return;

        try
        {
            var alarms = await _opcuaService.LoadAlarmAsync();
            if (alarms == null || alarms.Count == 0)
                return;

            foreach (var item in alarms)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Message))
                    continue;

                string errorName = _errorCode.GetErrorName(item.ID, item.Value, item.Category);
                string solution = _errorCode.GetSolution(item.ID, item.Value, item.Category);

                
                DateTime? occurredAt = (item.DateTime != null && item.DateTime.Any())? item.DateTime.First() : (DateTime?)null;

                if (_notifiedAlarmIds.Add(item.ID))
                {
                    var notification = new NotificationRequest
                    {
                        NotificationId = item.ID,
                        Title = $"🔔 {errorName} Notification",
                        Description = item.Message
                    };
                    LocalNotificationCenter.Current.Show(notification);
                }

               
                await SendAlarmEmailIfEnabledAsync(
                    alarmId: item.ID,
                    occurredAt: occurredAt,   
                    errorName: errorName,
                    message: item.Message,
                    solution: solution);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[AlarmMonitor] ERROR in CheckAndNotifyAlarmsAsync: " + ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    //send email based on the alam 
    private async Task SendAlarmEmailIfEnabledAsync(int alarmId, DateTime? occurredAt, string errorName, string message, string solution)
    {
        try
        {
            if (!Preferences.Get(PrefEnabled, false))
                return;

            string email = Preferences.Get(PrefEmail, "");
            if (string.IsNullOrWhiteSpace(email))
                return;

            
            long ticks = occurredAt?.Ticks ?? -1;
            string key = $"{alarmId}|{ticks}";

         
            if (!_emailedAlarmKeys.Add(key))
                return;

            string occurredAtText = occurredAt?.ToString() ?? "N/A";

            await AlarmEmailService.SendAlarmAsync(
                recipientEmail: email,
                alarmId: alarmId,
                errorName: errorName,
                message: message,
                occurredAt: occurredAtText,
                solution: solution);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[AlarmEmail] FAILED: " + ex);
        }
    }


    private void OnNotificationActionTapped(NotificationActionEventArgs e)
    {
        if (e.IsDismissed) return;

        if (e.IsTapped)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.GoToAsync(nameof(AlarmContentPage));
            });
        }
    }
}
