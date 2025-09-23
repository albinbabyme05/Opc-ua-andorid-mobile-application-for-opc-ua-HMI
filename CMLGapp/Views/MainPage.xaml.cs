
using CMLGapp.Models;
using CMLGapp.Services;
using CMLGapp.ViewModels;
using Microcharts;
using Microcharts.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;
using opcUa_Connecter.Models;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;
using SkiaSharp;
using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;
using System.Collections.Concurrent;


namespace CMLGapp.Views
{
    public partial class MainPage : ContentPage
    {
        private OpcUaService _opcuaService;
        private bool isLabelfadein = true;
        private bool _monitorsAttached = false;

        //pallet ui
        private bool _attached;
        private readonly Dictionary<int, List<Image>> _map = new();
        private int _centralPalletCount = 24;

        private Task _initTask;

        private bool _isPortrait = false;

        private readonly ConcurrentDictionary<int, int> _slotValues = new();
        private CancellationTokenSource? _bubbleCts;

        private static readonly int[] _allSlots = {
            1,7,13,19, 2,8,14,20, 3,9,15,21, 4,10,16,22, 5,11,17,23, 6,12,18,24,
            26,27,28,29,30,31, 32,33,34,35,36,37
        };

        private List<AlarmModel> _alarmList = new();
        private int _alarmIndex = 0;


        public MainPage()
        {

            InitializeComponent();

            _opcuaService = OpcUaService.Instance;
            Connectivity.Current.ConnectivityChanged += Connectivity_ConnectivityChanged;

            LabelFadeInOut(lblMachineStatus, machineStatusColor);

            BuildImageMap();
            foreach (var img in _map.Values.SelectMany(x => x))
                img.Source = "no_tool_in_pallet.svg";

            WireHoleTaps();

            this.SizeChanged += OnSizeChanged;
            this.Loaded += (_, __) =>
                Dispatcher.Dispatch(() => ApplyOrientationState(Width < Height));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (_initTask == null)
            {
                _initTask = InitializeMainPageAsync();
            }
            await _initTask.ConfigureAwait(false);
        }
        private void SizeForPortrait()
        {
            double usableWidth = Math.Max(0, this.Width - 24);

            double target = Math.Max(320, usableWidth * 0.90);

            PalletBlock.HeightRequest = target;

            TraysGrid.Scale = 1.0; 
        }

        //orientation switched
        private void OnSizeChanged(object sender, EventArgs e)
        {
            var portrait = Width < Height;
            if (portrait != _isPortrait)
            {
                _isPortrait = portrait;
                ApplyOrientationState(_isPortrait);
            }

            if (_isPortrait) SizeForPortrait();
        }


        private void ApplyOrientationState(bool portrait)
        {
            if (portrait)
            {
                CountersColRight.Width = new GridLength(0, GridUnitType.Absolute);
                VisualStateManager.GoToState(RootGrid, "Portrait");
                SizeForPortrait();                
            }
            else
            {
                CountersColLeft.Width = new GridLength(2, GridUnitType.Star);
                CountersColRight.Width = new GridLength(1.2, GridUnitType.Star);
                VisualStateManager.GoToState(RootGrid, "Landscape");
            }
        }


        private async Task InitializeMainPageAsync()
        {
            try
            {
                //subscribe all nodes before making a connection
                _opcuaService.ConnectionChanged -= OnConnectionChanged;
                _opcuaService.ConnectionChanged += OnConnectionChanged;

                

                var online = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
                UpdateOfflineBanner(!online || !_opcuaService.IsConnected);

                // timer ´before attaching monitor nodes
                await EnsureConnectedAsync(TimeSpan.FromSeconds(10));
                await LoadAndDisplayAlarmTicker();

                // plc time
                _opcuaService.MonitorPlcDateTime(UpdatePlcTime);
                CurrentDateAndTime();
                await SetPalletCapacity(); //pallet capacit
                await LoadAndDisplayPalletInfo();

                // attach once 
                AttachMonitorsOnce();

                await LoadAndDisplayProgressbar();// siplay progrss bar
                await DisplayExecutionTime();

                // background reconnection running
                _opcuaService.StartAutoReconnect();

                // pallet holes info load
                if (!_attached)
                {
                    _opcuaService.MonitorAllProductIngredientIds((index, value) =>
                    {
                        _slotValues[index] = value;

                        string svg = SvgForValue(index, value);

                        if (_map.TryGetValue(index, out var imgs))
                        {
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                foreach (var img in imgs)
                                {
                                    img.Source = svg;
                        #if WINDOWS || MACCATALYST
                                            Microsoft.Maui.Controls.TooltipProperties.SetText(img, GetSlotMessage(index));
                        #endif
                                }
                            });
                        }
                    }, samplingMs: 250);
                    _attached = true;
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("MainPage init failed: " + ex);
                await Shell.Current.GoToAsync("//login", false);
            }
        }

        // Await connection  timeout
        private async Task EnsureConnectedAsync(TimeSpan timeout)
        {
            if (_opcuaService.IsConnected) return;

            // connect  directly
            await _opcuaService.StartAppAsync();

            var start = DateTime.UtcNow;
            while (!_opcuaService.IsConnected && DateTime.UtcNow - start < timeout)
            {
                await Task.Delay(200);
            }
        }

        private void AttachMonitorsOnce()
        {
            if (_monitorsAttached || !_opcuaService.IsConnected) return;
            _monitorsAttached = true;

            _opcuaService.MonitorPlcDateTime(UpdatePlcTime);
            _opcuaService.MonitorNodes("Alarm", async _ => { await LoadAndDisplayAlarmTicker(); });

            _opcuaService.MonitorStatusNodes("Parameter", async _ => { await LoadAndDisplayPalletInfo(); });
            _opcuaService.MonitorNodes("ProdProcessedCount", async _ => { await LoadAndDisplayProgressbar(); });
            _opcuaService.MonitorNodes("StateCurrentTime", async _ => { await LoadAndDisplayStateCurrentTime(); });
            _opcuaService.MonitorStatusNodes("StateCurrentTime", async _ => { await DisplayExecutionTime(); });

            _opcuaService.MonitorSingleNodeValue("UnitModeCurrent", mode =>
            {
                MainThread.BeginInvokeOnMainThread(() => SetUnitModeCurrent(mode));
            });

            _opcuaService.MonitorSingleNodeValue("StateCurrent", state =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    SetStateCurrent(state);
                    _opcuaService.WriteDataToTextFile();
                });
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _opcuaService.ConnectionChanged -= OnConnectionChanged;
        }

        private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            var online = e.NetworkAccess == NetworkAccess.Internet;
            UpdateOfflineBanner(!online || !_opcuaService.IsConnected);

            if (online)
                _ = _opcuaService.TryReconnectAsync(TimeSpan.FromSeconds(8));
        }

        private void OnConnectionChanged(bool online)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateOfflineBanner(!online || Connectivity.Current.NetworkAccess != NetworkAccess.Internet);

                if (online)
                {
                    _monitorsAttached = false;
                    AttachMonitorsOnce();
                }
            });
        }

        //offline banner
        private void UpdateOfflineBanner(bool show) => OfflineBanner.IsVisible = show;

        //Reconnect
        private async void OnReconnectTapped(object sender, EventArgs e)
        {
            btnReconnect.IsEnabled = false;
            btnReconnect.Text = "Reconnecting...";
            var hasInternet = Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
            if (!hasInternet)
            {
                btnReconnect.IsEnabled = true;
                btnReconnect.Text = "Reconnect";
                await DisplayAlert("No Internet", "Please connect to a network first.", "OK");
                return;
            }

            var ok = await _opcuaService.TryReconnectAsync(TimeSpan.FromSeconds(8));
            btnReconnect.IsEnabled = true;
            btnReconnect.Text = "Reconnect";

            UpdateOfflineBanner(!ok || Connectivity.Current.NetworkAccess != NetworkAccess.Internet);
            if (!ok) await DisplayAlert("Still offline", "Couldn't reconnect.  trying to connect in background.", "OK");
        }

        private async void OnViewMoreClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(DetailsPage));
        }

        // set apllet capacity
        private async Task SetPalletCapacity()
        {
            (string palletName, string palletUnit, int palletId, float palletValue) = await _opcuaService.GetPalletInformationAsyc();
            int palletCpcty = int.TryParse(palletUnit, out int c) ? c : 0;
            _centralPalletCount = palletCpcty == 20 ? 20 : 24;

            bool is24 = _centralPalletCount == 24;
            Img6.IsVisible = is24;
            Img12.IsVisible = is24;
            Img18.IsVisible = is24;
            Img24.IsVisible = is24;
        }

        private static string SvgForValue(int index, int v)
        {
            if (v < 0) { return "findtool.svg"; }
            if (v == 0) { return "blackspottool.svg"; } // gripper action
            if (index >= 32 && index <= 37) return "defect_tool.svg";
            return "good_tool.svg";
        }

        private void add(int productIndex, Image img)
        {
            if (!_map.TryGetValue(productIndex, out var list))
                _map[productIndex] = list = new List<Image>();
            list.Add(img);
        }

        private void BuildImageMap()
        {
            // Center 1..24
            add(1, Img1); add(7, Img7); add(13, Img13); add(19, Img19);
            add(2, Img2); add(8, Img8); add(14, Img14); add(20, Img20);
            add(3, Img3); add(9, Img9); add(15, Img15); add(21, Img21);
            add(4, Img4); add(10, Img10); add(16, Img16); add(22, Img22);
            add(5, Img5); add(11, Img11); add(17, Img17); add(23, Img23);
            add(6, Img6); add(12, Img12); add(18, Img18); add(24, Img24);

            // Left 26..31
            add(26, Img26); add(27, Img27); add(28, Img28);
            add(29, Img29); add(30, Img30); add(31, Img31);

            // Right 32..37
            add(32, Img32); add(33, Img33); add(34, Img34);
            add(35, Img35); add(36, Img36); add(37, Img37);
        }

        //alarm notitfication in main page
        private async Task LoadAndDisplayAlarmTicker()
        {
            try
            {
                var list = await _opcuaService.LoadAlarmAsync();

                if (list == null || list.Count == 0 )
                {
                    _alarmList = new();
                    _alarmIndex = 0;
                    ShowNoAlarm();
                    return;
                }

                // lastest alarm based on date
                _alarmList = list
                    .OrderByDescending(AlarmTimestamp)
                    .ToList();

                _alarmIndex = 0;
                UpdateAlarmTickerVisual();
            }
            catch
            {
                ShowNoAlarm();
            }
        }


        private static DateTime AlarmTimestamp(AlarmModel a)
        {
            if (a?.DateTime != null && a.DateTime.Length > 0)
                return a.DateTime.Max();  // newest tick in that alarm
            return DateTime.MinValue;
        }

        private static string AlarmText(AlarmModel a)
        {
            if (a == null) return string.Empty;
            foreach (var s in new[] { a.Message })
                if (!string.IsNullOrWhiteSpace(s)) return s.Trim();
            return string.Empty;
        }


        

        private void ShowNoAlarm()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblAlarmMsg.Text = "No alarm";
                var dim = Color.FromArgb("#2E363E");
                AlarmTicker.BackgroundColor = dim;
                AlarmTicker.BorderColor = dim;
            });
        }

        private void UpdateAlarmTickerVisual()
        {
            if (_alarmList == null || _alarmList.Count == 0)
            {
                ShowNoAlarm();
                return;
            }

            var current = _alarmList[_alarmIndex];
            var text = AlarmText(current);

            if (string.IsNullOrWhiteSpace(text))
                current = _alarmList.FirstOrDefault(a => !string.IsNullOrWhiteSpace(AlarmText(a)));

            if (current == null)
            {
                ShowNoAlarm();
                return;
            }

            text = AlarmText(current);

            var anyText = _alarmList.Any(a => !string.IsNullOrWhiteSpace(AlarmText(a)));
            var bg = anyText ? Colors.Red : Color.FromArgb("#4C5661");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblAlarmMsg.Text = text;
                AlarmTicker.BackgroundColor = bg;
                AlarmTicker.BorderColor = bg;
            });
        }



        private void OnAlarmTapped(object sender, EventArgs e)
        {
            if (_alarmList == null || _alarmList.Count == 0) return;

            var start = _alarmIndex;
            do
            {
                _alarmIndex = (_alarmIndex + 1) % _alarmList.Count;
                if (!string.IsNullOrWhiteSpace(AlarmText(_alarmList[_alarmIndex])))
                    break;
            }
            while (_alarmIndex != start);

            UpdateAlarmTickerVisual();
        }


        // plc time
        private void UpdatePlcTime(string time)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblPlcTime.Text = $"{time}";
            });
        }

        //product progress
        private async Task<(int, int)> GetProdProcessing()
        {
            try
            {
                var prodProcessed = await _opcuaService.LoadProdProcessedAsync();
                if (prodProcessed != null)
                {
                    var item = prodProcessed.First();
                    return (item.Count, item.AccCount);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Loading ProdProcessing method " + e);
            }
            return (0, 0);
        }

        private void CurrentDateAndTime()
        {
            DateTime current = DateTime.Now;
            string date = current.ToString(" / dd.MM.yyyy");
            lblDateTime.Text = date;
        }

        // %completed
        private async Task LoadAndDisplayProgressbar()
        {
            var (_, palletUnit, _, _) = await GetPalletInformationAsyc();

            int maxCount = int.TryParse(palletUnit, out var parsed) ? parsed : 0;
            var prodProcessed = await _opcuaService.LoadProdProcessedAsync();
            var (_, defectCount, _) = await _opcuaService.GetProdDefect();
            var (_, measuredCount, _) = await _opcuaService.GetProdMeasured();

            if (prodProcessed != null)
            {
                foreach (var item in prodProcessed)
                {
                    int processed = (int)Math.Max(0, item.Count);
                    int safeCapacity = (int)Math.Max(1, maxCount); // keep 1, palletunit can be null

                    double progress = (double)System.Math.Min(processed, safeCapacity) / safeCapacity;
                    int progressPercent = (int)(progress * 100);

                    //tolarance pregressbar
                    int red = (int)Math.Max(0, defectCount);
                    int green = (int)Math.Max(0, measuredCount);
                    int remaining = (int)Math.Max(0, maxCount - processed);

                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        lblCompleted.Text = $"{progressPercent}";
                        progressBar.Progress = progress;
                        await progressBar.ProgressTo(progress, 250, Easing.Linear);

                        lblMeasuredCount.Text = measuredCount.ToString();
                        lblDefectCount.Text = defectCount.ToString();

                        ToleranceBar.SetValues(red: red, green: green, remaining: remaining, totalCapacity: maxCount);
                    });
                }
            }
        }


        private async Task<(string, string, int, float)> GetPalletInformationAsyc()
        {
            var palletDetails = await _opcuaService.LoadPalletInfoAsync();
            if (palletDetails != null)
            {
                var item = palletDetails.LastOrDefault();
                if (item != null && item?.Value >= 1)
                    return (item.Name, item.Unit, item.ID, item.Value);

                return ("Unavailable", "0", 0, 0);
            }
            return (" Unavailable", "0", 0, 0);
        }

        private async Task LoadAndDisplayPalletInfo()
        {
            (string palletName, string palletUnit, int palletId, float palletValue) = await GetPalletInformationAsyc();
            string adapterName = _opcuaService.PalletName(palletValue);
            MainThread.BeginInvokeOnMainThread(() =>
            {
                lblPalletName.Text = $"Adapter : {adapterName}";
                lblPalletCapacity.Text = $" Smart Pallet Capacity : {palletUnit}";
            });
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

        private void SetUnitModeCurrent(int mode)
        {
            (string unitMode, Color clr) = _opcuaService.MachineUnitMode(mode);
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

        private async Task LoadAndDisplayStateCurrentTime()
        {
            await DisplayExecutionTime();
        }

        private async Task DisplayExecutionTime()
        {
            var unitMode = await _opcuaService.LoadUnitMode();
            var stateCurrentMode = 4;
            (string mode, Color modeClr) = _opcuaService.MachineUnitMode(unitMode);
            if (stateCurrentMode == 4)
            {
                var seconds = await _opcuaService.LoadStateCurrentTime(unitMode - 1, stateCurrentMode);
                TimeSpan time = TimeSpan.FromSeconds(seconds);
                string formattedTime = time.ToString(@"hh\:mm\:ss");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    lblExecutionTime.Text = formattedTime;
                    lblExecutionTime.TextColor = modeClr;
                });
            }
        }


        private void OnHamburgerTapped(object sender, EventArgs e)
        {
            // Open the Shell flyout
            if (Application.Current?.MainPage is Shell shell)
            {
                shell.FlyoutIsPresented = true;
            }
        }

        // pop up info
        void WireHoleTaps()
        {
            foreach (var kv in _map)
            {
                var slot = kv.Key;
                foreach (var img in kv.Value)
                {
                    img.GestureRecognizers.Clear();
                    img.GestureRecognizers.Add(new TapGestureRecognizer
                    {
                        Command = new Command(() =>
                        {
                            var msg = GetSlotMessage(slot);
                            ShowHoleBubble(img, msg); 
                        })
                    });
                }
            }
        }


        // app startup initialize time
        string GetSlotMessage(int slot)
        {
            if (!_slotValues.TryGetValue(slot, out var val))
                return $"Slot {slot}: Reading...";
            if (slot >= 32 && val > 0)
                return $"Slot: {slot} Defect";
            return val switch
            {
                -1 => $"Slot: {slot} Measuring...",
                0 => $"Slot: {slot} Empty",
                _ => $"Slot: {slot} Measured"
            };
        }

        // Show a small bubble near the tapped hole
        async void ShowHoleBubble(Image img, string text)
        {
            if (PalletOverlay == null || HoleInfoBubble == null) return;

            HoleInfoLabel.Text = text;

            // Center of the image relative to overlay
            var center = GetCenterRelativeTo(img, PalletOverlay);

            // Offset bubble a bit above-right of the hole
            const double xOffset = 18;
            const double yOffset = -28;

            // Make bubble visible and measure it
            HoleInfoBubble.IsVisible = true;
            HoleInfoBubble.Opacity = 0;
            HoleInfoBubble.ForceLayout();
            await Task.Yield();

            double bubbleWidth = HoleInfoBubble.Width > 0 ? HoleInfoBubble.Width : 220;
            double bubbleHeight = HoleInfoBubble.Height > 0 ? HoleInfoBubble.Height : 34;

            double targetX = center.X + xOffset;
            double targetY = center.Y + yOffset - bubbleHeight;

            // Keep inside the overlay bounds
            double maxX = Math.Max(0, PalletOverlay.Width - bubbleWidth - 4);
            double maxY = Math.Max(0, PalletOverlay.Height - bubbleHeight - 4);
            targetX = Math.Max(4, System.Math.Min(targetX, maxX));
            targetY = Math.Max(4, System.Math.Min(targetY, maxY));

            AbsoluteLayout.SetLayoutBounds(HoleInfoBubble, new Rect(targetX, targetY, bubbleWidth, bubbleHeight));

            await HoleInfoBubble.FadeTo(1, 120);
            await Task.Delay(1200);
            await HoleInfoBubble.FadeTo(0, 150);
            HoleInfoBubble.IsVisible = false;
        }

        // Geometry helpers
        Point GetCenterRelativeTo(VisualElement child, VisualElement container)
        {
            var p = GetPositionRelativeTo(child, container);
            return new Point(p.X + child.Width / 2, p.Y + child.Height / 2);
        }

        Point GetPositionRelativeTo(VisualElement child, VisualElement ancestor)
        {
            double x = child.X, y = child.Y;
            Element parent = child.Parent;

            // accumulate offsets up the tree
            while (parent is VisualElement ve && ve != ancestor)
            {
                x += ve.X;
                y += ve.Y;
                parent = ve.Parent;
            }

            // account for ScrollView scroll offset
            var scroller = FindAncestor<ScrollView>(child);
            if (scroller != null)
            {
                x -= scroller.ScrollX;
                y -= scroller.ScrollY;
            }

            return new Point(x, y);
        }

        T? FindAncestor<T>(Element start) where T : Element
        {
            var p = start.Parent;
            while (p != null && p is not T)
                p = p.Parent;
            return p as T;
        }

        private async void OnAlarmButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(AlarmContentPage));
        }

    }


}//end class
