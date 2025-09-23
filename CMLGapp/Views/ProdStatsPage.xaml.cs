using CMLGapp.Services;
using Microcharts;
using Microcharts.Maui; // ChartView
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Alias so any local Math helper won’t shadow System.Math
using SysMath = System.Math;

namespace CMLGapp.Views
{
    public partial class ProdStatsPage : ContentPage
    {
        private OpcUaService _opcuaService;
        private bool isLabelfadein = true;

        private enum RangeFilter { Today, Week, Month, Day }

        // Default filter is WEEK (as requested)
        private RangeFilter _filter = RangeFilter.Week;
        private DateTime _selectedDate;

        private ChartView _toolsChart;
        private ChartView _moneyTotalChart;

        private double _moneyLifetime = 0;
        private List<DailyRow> _allRows = new();

        private const int HumanMinutesPerTool = 8;     // human time per tool
        private const double MoneyFactorPerHour = 30.0;  // $/hour factor

        // Live-update support
        private CancellationTokenSource _liveCts;
        private int _liveCount = 0;
        private int _liveAccCount = 0;
        private double _liveMachineHours = 0;
        private double _liveSavedHours = 0;
        private double _liveMoneyToday = 0;

        public ProdStatsPage()
        {
            InitializeComponent();
            _opcuaService = OpcUaService.Instance;

            lblDateTime.Text = DateTime.Now.ToString(" / dd.MM.yyyy");
            LabelFadeInOut(lblMachineStatus, machineStatusColor);

            // Phone Calendar 
            dpDay.IsVisible = DeviceInfo.Idiom != DeviceIdiom.Phone;

            SizeChanged += (_, __) => ApplyTightHeights();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await LoadCsvAsync("tool_measurement_savings_5months.csv"); // Resources/Raw

            // default page
            _filter = RangeFilter.Week;
            SetActiveFilterButton(_filter);

            // First fill
            await RefreshAllAsync();

            TryHookOpcUaMonitors();

            // Start live loop
            _liveCts?.Cancel();
            _liveCts = new CancellationTokenSource();
            _ = StartLiveUpdaterAsync(_liveCts.Token);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // stop live loop
            _liveCts?.Cancel();

            if (_toolsChart != null)
            {
                _toolsChart.Chart = null;
                toolsBarHost.Content = null;
                _toolsChart.Handler?.DisconnectHandler();
                _toolsChart = null;
            }
            if (_moneyTotalChart != null)
            {
                _moneyTotalChart.Chart = null;
                moneySavedHost.Content = null;
                _moneyTotalChart.Handler?.DisconnectHandler();
                _moneyTotalChart = null;
            }
        }

        // ----------------- Data model (CSV) -----------------
        private sealed class DailyRow
        {
            public DateTime Date { get; set; }
            public int ToolsMeasured { get; set; }
            public int TotalToolsMeasured { get; set; }
            public double SavedWorkingHours { get; set; }  // hours
            public double TotalWorkingHours { get; set; }  // hours
            public double MoneySavedToday { get; set; }
            public double MoneySavedTotal { get; set; }
        }

        // ----------------- CSV loader -----------------
        private async Task LoadCsvAsync(string fileName)
        {
            if (_allRows.Count > 0) return;

            using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            using var reader = new StreamReader(stream);

            _ = reader.ReadLine(); // header
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = line.Split(',');

                _allRows.Add(new DailyRow
                {
                    Date = DateTime.Parse(parts[0], CultureInfo.InvariantCulture),
                    ToolsMeasured = SafeInt(parts[1]),
                    TotalToolsMeasured = SafeInt(parts[2]),
                    SavedWorkingHours = SafeDouble(parts[3]),
                    TotalWorkingHours = SafeDouble(parts[4]),
                    MoneySavedToday = SafeDouble(parts[5]),
                    MoneySavedTotal = SafeDouble(parts[6]),
                });
            }

            _allRows = _allRows.OrderBy(r => r.Date).ToList();
            _moneyLifetime = _allRows.Sum(r => r.MoneySavedToday);

            var min = _allRows.First().Date.Date;
            var max = _allRows.Last().Date.Date;
            dpDay.MinimumDate = min;
            dpDay.MaximumDate = max;
            dpDay.Date = max;
            _selectedDate = max;
        }

        private static int SafeInt(string s) =>
            int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
        private static double SafeDouble(string s) =>
            double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;

        // ----------------- Filters -----------------
        private async void OnFilterToday(object sender, EventArgs e)
        {
            _filter = RangeFilter.Today;
            SetActiveFilterButton(_filter);
            await RefreshAllAsync();
        }

        private async void OnFilterWeek(object sender, EventArgs e)
        {
            _filter = RangeFilter.Week;
            SetActiveFilterButton(_filter);
            await RefreshAllAsync();
        }

        private async void OnFilterMonth(object sender, EventArgs e)
        {
            _filter = RangeFilter.Month;
            SetActiveFilterButton(_filter);
            await RefreshAllAsync();
        }

        private void SetActiveFilterButton(RangeFilter f)
        {
            btnToday.BackgroundColor = Color.FromArgb(f == RangeFilter.Today ? "#39434C" : "#2F3942");
            btnWeek.BackgroundColor = Color.FromArgb(f == RangeFilter.Week ? "#39434C" : "#2F3942");
            btnMonth.BackgroundColor = Color.FromArgb((f == RangeFilter.Month || f == RangeFilter.Day) ? "#39434C" : "#2F3942");

            dpDay.IsVisible = (f == RangeFilter.Month || f == RangeFilter.Day);

            string scope = f switch
            {
                RangeFilter.Today => "TODAY",
                RangeFilter.Week => "WEEK",
                RangeFilter.Month => "MONTH",
                RangeFilter.Day => _selectedDate.ToString("dd.MM.yyyy"),
                _ => "WEEK"
            };

            lblToolsBarTitle.Text = $"TOOLS MEASURED Graph ({scope})";

            lblToolsTotalTitle.Text = f switch
            {
                RangeFilter.Today => "TOOLS MEASURED (TODAY)",
                RangeFilter.Week => "TOOLS MEASURED (WEEK)",
                RangeFilter.Month => "TOOLS MEASURED (MONTH)",
                RangeFilter.Day => $"TOOLS MEASURED ({_selectedDate:dd.MM.yyyy})",
                _ => "TOOLS MEASURED (WEEK)"
            };

            lblMoneySavedTodayTitle.Text = f switch
            {
                RangeFilter.Today => "MONEY SAVED TODAY",
                RangeFilter.Week => "MONEY SAVED (WEEK)",
                RangeFilter.Month => "MONEY SAVED (MONTH)",
                RangeFilter.Day => $"MONEY SAVED ({_selectedDate:dd.MM.yyyy})",
                _ => lblMoneySavedTodayTitle.Text
            };
        }

        // ----------------- Calendar -----------------
        private async void OnDayPicked(object sender, DateChangedEventArgs e)
        {
            _selectedDate = e.NewDate.Date;
            _filter = RangeFilter.Day;
            SetActiveFilterButton(_filter);
            await RefreshAllAsync();
        }

        // ----------------- Period helpers -----------------
        private List<DailyRow> CurrentPeriodRows()
        {
            if (_allRows.Count == 0) return new();

            var lastCsvDay = _allRows.Max(r => r.Date).Date;
            DateTime start = lastCsvDay, end = lastCsvDay;

            switch (_filter)
            {
                case RangeFilter.Today:
                    start = end = lastCsvDay;
                    break;
                case RangeFilter.Week:
                    start = lastCsvDay.AddDays(-6);
                    end = lastCsvDay;
                    break;
                case RangeFilter.Month:
                    start = new DateTime(lastCsvDay.Year, lastCsvDay.Month, 1);
                    end = lastCsvDay;
                    break;
                case RangeFilter.Day:
                    start = end = _selectedDate;
                    break;
            }

            return _allRows.Where(r => r.Date.Date >= start && r.Date.Date <= end).ToList();
        }

        private List<DailyRow> CurrentPeriodRowsWithLive()
        {
            var rows = CurrentPeriodRows();
            if (rows.Count == 0) return rows;

            var today = DateTime.Today;
            var hasToday = rows.Any(r => r.Date.Date == today);

            var liveRow = new DailyRow
            {
                Date = today,
                ToolsMeasured = _liveCount,
                TotalToolsMeasured = _liveAccCount,
                SavedWorkingHours = _liveSavedHours,
                TotalWorkingHours = _liveMachineHours,
                MoneySavedToday = _liveMoneyToday,
                MoneySavedTotal = 0
            };

            if (!hasToday && (_filter == RangeFilter.Week || _filter == RangeFilter.Month))
            {
                rows.Add(liveRow);
            }
            else if (hasToday)
            {
                var idx = rows.FindIndex(r => r.Date.Date == today);
                rows[idx] = liveRow;
            }

            return rows.OrderBy(r => r.Date).ToList();
        }

        private static double PeriodDeltaCumulative(List<DailyRow> all, List<DailyRow> period, Func<DailyRow, double> sel)
        {
            if (period.Count == 0) return 0;
            var last = sel(period[^1]);
            var firstDate = period[0].Date.Date;
            var prior = all.Where(r => r.Date.Date < firstDate).Select(sel).DefaultIfEmpty(0).LastOrDefault();
            return SysMath.Max(0, last - prior);
        }

        // ----------------- Charts -----------------
        private ChartView NewChartView() => new()
        {
            BackgroundColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.FillAndExpand,
            VerticalOptions = LayoutOptions.FillAndExpand
        };

        private void ReplaceChart(ContentView host, ref ChartView field, Chart chart)
        {
            if (field != null)
            {
                field.Chart = null;
                host.Content = null;
                field.Handler?.DisconnectHandler();
                field = null;
            }

            var cv = NewChartView();
            cv.Chart = chart;
            host.Content = cv;
            field = cv;

            cv.InvalidateSurface();
            host.ForceLayout();
        }

        private void BuildToolsBarChart(List<DailyRow> rows)
        {
            var cyan = SKColor.Parse("#7DE0FF");
            var entries = rows.Select(r =>
                new ChartEntry(r.TotalToolsMeasured)
                {
                    Label = r.Date.ToString("dd.MM"),
                    ValueLabel = r.TotalToolsMeasured.ToString(CultureInfo.InvariantCulture),
                    Color = cyan,
                    TextColor = cyan,
                    ValueLabelColor = cyan
                }).ToList();

            var chart = new BarChart
            {
                Entries = entries,
                LabelTextSize = 22,
                ValueLabelTextSize = 22,
                LabelColor = SKColors.Cyan,
                IsAnimated = false,
                BackgroundColor = SKColors.Transparent,
                MaxValue = (float)(entries.Count > 0
                    ? SysMath.Max(entries.Max(e => float.TryParse(e.ValueLabel, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0f) * 1.1f, 1)
                    : 1)
            };

            ReplaceChart(toolsBarHost, ref _toolsChart, chart);
        }

        private void BuildMoneySavedTotalChart(List<DailyRow> rows)
        {
            var green = SKColor.Parse("#77E07C");
            var cyan = SKColor.Parse("#7DE0FF");
            var text = SKColor.Parse("#E7EEF5");

            var entries = rows.Select(r =>
                new ChartEntry((float)r.MoneySavedToday)
                {
                    Label = r.Date.ToString("dd.MM"),
                    ValueLabel = r.MoneySavedToday.ToString("N0", CultureInfo.InvariantCulture),
                    Color = green,
                    ValueLabelColor = cyan,
                    TextColor = text
                }).ToList();

            var chart = new LineChart
            {
                Entries = entries,
                MinValue = 0,
                LineMode = LineMode.Spline,
                LineSize = 4,
                PointMode = PointMode.Circle,
                PointSize = 14,
                PointAreaAlpha = 50,
                ValueLabelTextSize = 22,
                LabelTextSize = 22,
                LabelColor = SKColors.Cyan,
                BackgroundColor = SKColors.Transparent,
                IsAnimated = false
            };

            ReplaceChart(moneySavedHost, ref _moneyTotalChart, chart);
        }

        // ----------------- Live updater -----------------
        private async Task StartLiveUpdaterAsync(CancellationToken ct)
        {
            await Task.Delay(200, ct);
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await UpdateFromLiveOnceAsync();
                    await RefreshAllAsync();
                }
                catch
                {
                    // keep loop alive
                }

                await Task.Delay(2000, ct);
            }
        }

        private async Task UpdateFromLiveOnceAsync()
        {
            var (_, count, accCount) = await GetProdMeasured();

            double machineSec = await GetMachineSecondsTodayAsync();
            double machineHours = machineSec / 3600.0;
            double humanHours = (accCount * HumanMinutesPerTool) / 60.0;
            double savedHours = SysMath.Max(0, humanHours - machineHours);
            double moneyToday = savedHours * MoneyFactorPerHour;

            _liveCount = count;
            _liveAccCount = accCount;
            _liveMachineHours = machineHours;
            _liveSavedHours = savedHours;
            _liveMoneyToday = moneyToday;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblToolsPeriodValue.Text = _liveCount.ToString("N0", CultureInfo.InvariantCulture);
                lblTotalWorkingHours.Text = _liveMachineHours.ToString("N1", CultureInfo.InvariantCulture);
                lblMoneySavedToday.Text = $"$ {_liveMoneyToday.ToString("N2", CultureInfo.InvariantCulture)}";
            });
        }

        // ----------------- Refresh (CSV + Live) -----------------
        private async Task<double> GetMachineSecondsTodayAsync()
        {
            var unitMode = await _opcuaService.LoadUnitMode();
            const int stateCurrentMode = 4;
            var seconds = await _opcuaService.LoadStateCurrentTime(unitMode - 1, stateCurrentMode);
            return SysMath.Max(0, seconds);
        }

        private async Task<(int id, int Count, int AccCount)> GetProdMeasured()
        {
            try
            {
                var prodMeasured = await _opcuaService.LoadProdConsumedAsync();
                if (prodMeasured != null && prodMeasured.Any())
                {
                    var item = prodMeasured.First();
                    return (item.ID, item.Count, item.AccCount);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Loading GetProdMeasured(): " + e);
            }
            return (0, 0, 0);
        }

        private async Task RefreshAllAsync()
        {
            if (_allRows.Count == 0) return;

            if (_filter == RangeFilter.Today)
            {
                var todayRow = new DailyRow
                {
                    Date = DateTime.Today,
                    ToolsMeasured = _liveCount,
                    TotalToolsMeasured = _liveAccCount,
                    SavedWorkingHours = _liveSavedHours,
                    TotalWorkingHours = _liveMachineHours,
                    MoneySavedToday = _liveMoneyToday,
                    MoneySavedTotal = 0
                };

                // Card values for TODAY
                lblToolsTotalValue.Text = todayRow.TotalToolsMeasured.ToString("N0", CultureInfo.InvariantCulture);
                lblHoursSaved.Text = todayRow.SavedWorkingHours.ToString("N1", CultureInfo.InvariantCulture);
                lblTotalWorkingHours.Text = todayRow.TotalWorkingHours.ToString("N1", CultureInfo.InvariantCulture);
                lblMoneySavedToday.Text = $"$ {_liveMoneyToday.ToString("N2", CultureInfo.InvariantCulture)}";
                lblMoneySavedTotal.Text = $"$ {_moneyLifetime.ToString("N2", CultureInfo.InvariantCulture)}";

                BuildToolsBarChart(new List<DailyRow> { todayRow });
                BuildMoneySavedTotalChart(new List<DailyRow> { todayRow });
                return;
            }

            // Week / Month / Day
            var period = (_filter == RangeFilter.Day) ? CurrentPeriodRows() : CurrentPeriodRowsWithLive();

            // Range total 
            var toolsTotalRange = period.Sum(r => r.TotalToolsMeasured);
            lblToolsTotalValue.Text = toolsTotalRange.ToString("N0", CultureInfo.InvariantCulture);

            // Saved/Total working hours for the period
            var savedHours = period.Sum(r => r.SavedWorkingHours);
            lblHoursSaved.Text = savedHours.ToString("N1", CultureInfo.InvariantCulture);

            var totalHours = PeriodDeltaCumulative(_allRows, period, r => r.TotalWorkingHours);
            if ((_filter == RangeFilter.Week || _filter == RangeFilter.Month) && period.Any(r => r.Date.Date == DateTime.Today))
            {
                // live machine hours 
                var csvTodayHours = period.Where(r => r.Date.Date == DateTime.Today).Select(r => r.TotalWorkingHours).DefaultIfEmpty(0).LastOrDefault();
                totalHours = totalHours - csvTodayHours + _liveMachineHours;
            }
            lblTotalWorkingHours.Text = totalHours.ToString("N1", CultureInfo.InvariantCulture);

            // Money saved in the selected period 
            double moneyPeriod = period.Sum(r => r.MoneySavedToday);
            if ((_filter == RangeFilter.Week || _filter == RangeFilter.Month) && period.Any(r => r.Date.Date == DateTime.Today))
            {
                moneyPeriod = period.Where(r => r.Date.Date != DateTime.Today).Sum(r => r.MoneySavedToday) + _liveMoneyToday;
            }
            lblMoneySavedToday.Text = $"$ {moneyPeriod.ToString("N2", CultureInfo.InvariantCulture)}";

            // Lifetime (from CSV)
            lblMoneySavedTotal.Text = $"$ {_moneyLifetime.ToString("N2", CultureInfo.InvariantCulture)}";

            // Charts
            BuildToolsBarChart(period);
            BuildMoneySavedTotalChart(period);
        }

        // ----------------- Layout / status / monitors -----------------
        private void ApplyTightHeights()
        {
            toolsBarHost.HeightRequest = 180;
            moneySavedHost.HeightRequest = 180;
        }

        private void TryHookOpcUaMonitors()
        {
            try
            {
                lblPlcTime.Text = DateTime.Now.ToString("HH:mm:ss");
                SetUnitModeCurrent(_opcuaService?.IsConnected == true ? 1 : 0);
                SetStateCurrent(4);

                if (_opcuaService == null) return;

                _opcuaService.MonitorPlcDateTime(time =>
                {
                    MainThread.BeginInvokeOnMainThread(() => lblPlcTime.Text = time);
                });

                _opcuaService.MonitorSingleNodeValue("UnitModeCurrent", mode =>
                {
                    MainThread.BeginInvokeOnMainThread(() => SetUnitModeCurrent(mode));
                });

                _opcuaService.MonitorSingleNodeValue("StateCurrent", state =>
                {
                    MainThread.BeginInvokeOnMainThread(() => SetStateCurrent(state));
                });
            }
            catch
            {
                // ignore
            }
        }

        private void SetUnitModeCurrent(int mode)
        {
            var (unitMode, clr) = _opcuaService.MachineUnitMode(mode);
            lblMachineMode.Text = unitMode.ToUpperInvariant();
            lblMachineMode.TextColor = clr;
        }

        private void SetStateCurrent(int status)
        {
            var (txtStatus, svgImage, clr) = _opcuaService.MachineStateCurrent(status);
            lblMachineStatus.Text = txtStatus.ToUpperInvariant();
            lblMachineStatus.TextColor = clr;
            machineStatusColor.Source = svgImage;
            bvStart.Color = Colors.Gray;
            bvRun.Color = Colors.Gray;
            bvStop.Color = Colors.Gray;

            switch (status)
            {
                case 0:
                    bvStop.Color = Colors.Red;
                    break;
                case 1:
                    bvStart.Color = Colors.Orange;
                    break;
                case 2:
                    bvStart.Color = Colors.Orange;
                    break;
                case 3:
                    bvStart.Color = Colors.Orange;
                    break;

                case 4:
                    bvRun.Color = Colors.Green;
                    break;
                case 5:
                    bvStart.Color = Colors.Orange;
                    break;
                case 6:
                    bvStop.Color = Colors.Red;
                    break;
                case 7:
                    bvRun.Color = Colors.Blue;
                    break;
                case 8:
                    bvStart.Color = Colors.Orange;
                    break;
                case 9:
                    bvStop.Color = Colors.Red;
                    break;
                case 10:
                    bvRun.Color = Colors.Blue;
                    break;
                case 11:
                    bvRun.Color = Colors.Blue;
                    break;
                case 12:
                    bvStop.Color = Colors.Red;
                    break;
                case 13:
                    bvStop.Color = Colors.Red;
                    break;
                case 14:
                    bvStop.Color = Colors.Red;
                    break;
                case 15:
                    bvStop.Color = Colors.Red;
                    break;
                case 16:
                    bvRun.Color = Colors.Blue;
                    break;
            }
        }

        private void OnHamburgerTapped(object sender, EventArgs e)
        {
            if (Application.Current?.MainPage is Shell shell)
                shell.FlyoutIsPresented = true;
        }

        private async void LabelFadeInOut(Label label, Image img)
        {
            while (isLabelfadein)
            {
                await Task.WhenAll(label.FadeTo(0, 2000), img.FadeTo(0, 2000));
                await Task.Delay(1000);
                await Task.WhenAll(label.FadeTo(1, 2000), img.FadeTo(1, 2000));
                await Task.Delay(1000);
            }
        }
    }
}
