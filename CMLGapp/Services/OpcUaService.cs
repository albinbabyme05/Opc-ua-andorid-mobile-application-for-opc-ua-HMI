using CMLGapp.Models;
using Opc.Ua;
using Opc.Ua.Client;
using opcUa_Connecter.Models;
using opcUa_Connecter.Modules;
using opcUa_Connecter.Services;
using Plugin.LocalNotification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CMLGapp.Services
{
    public class OpcUaService
    {
        private static OpcUaService _instance;
        public static OpcUaService Instance => _instance ??= new OpcUaService();

        private Session _session;
        private ManagerModule _manager;
        private ManagerModule _statusManager;
        private SubscriptionService _subscriptionService;
        private NodeReader _reader;
        private ServerConnection _client;

        private readonly NodeId _adminNodeId = "ns=4;s=|var|CODESYS Control Win V3 x64.Application.PackTag.CoraMeasure.Admin";
        private readonly NodeId _statusNodeId = "ns=4;s=|var|CODESYS Control Win V3 x64.Application.PackTag.CoraMeasure.Status";
        //private readonly string _endpoint = "opc.tcp://192.168.1.127:4840";
        private readonly string _endpoint = "opc.tcp://10.0.39.14:4840";

        private CancellationTokenSource _reconnectCts;

        public bool IsConnected => _client?.IsConnected == true;

        // true online, false offline
        public event Action<bool> ConnectionChanged;

        private volatile bool _wired;


        private OpcUaService()
        {
            _client = new ServerConnection();
            _client.ConnectionChanged += OnClientConnectionChanged;
        }

        private void WireManagers()
        {
            if (_session == null || !_session.Connected) return;

            _reader = new NodeReader(_session);
            _manager = new ManagerModule(_reader, _adminNodeId);
            _statusManager = new ManagerModule(_reader, _statusNodeId);

            if (_subscriptionService == null)
                _subscriptionService = new SubscriptionService(_session);
            else
                _subscriptionService.UpdateSession(_session);
        }

        private void OnClientConnectionChanged(bool online)
        {
            if (online)
            {
                _session = _client.Session;
                if (!_wired && _session?.Connected == true)
                {
                    WireManagers();
                    _wired = true;
                }
            }
            else
            {
                _wired = false;

                //try reconenecting
                StartAutoReconnect();
            }
            ConnectionChanged?.Invoke(online);
        }

        public async Task<bool> StartAppAsync()
        {
            try
            {
                if (IsConnected) return true;
                await _client.ConnectAsync(_endpoint).ConfigureAwait(false);
                _session = _client.Session;
                WireManagers();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("StartAppAsync: could not connect: " + e.Message);
                // allow UI to run offline.
                ConnectionChanged?.Invoke(false);
                return false;
            }
        }

        /// <summary>Manual reconnect attempt (used by banner/button).</summary>
        public async Task<bool> TryReconnectAsync(TimeSpan? timeout = null)
        {
            _reconnectCts?.Cancel();
            // default to 8 seconds
            var actualTimeout = timeout ?? TimeSpan.FromSeconds(8);
            using var cts = new CancellationTokenSource(actualTimeout);

            var ok = await _client.TryReconnectLoopAsync(
                _endpoint,
                // delay between attempts
                TimeSpan.FromSeconds(1),
                cts.Token
            );

            // resume background loop
            StartAutoReconnect();

            return ok;
        }

        /// <summary>Background auto-reconnect loop while app runs.</summary>
        public void StartAutoReconnect(TimeSpan? interval = null)
        {
            _reconnectCts?.Cancel();
            _reconnectCts = new CancellationTokenSource();
            var delay = interval ?? TimeSpan.FromSeconds(5);

            _ = Task.Run(async () =>
            {
                var token = _reconnectCts.Token;
                while (!token.IsCancellationRequested)
                {
                    if (!IsConnected)
                    {
                        try
                        {
                            await _client.ConnectAsync(_endpoint, token).ConfigureAwait(false);
                            _session = _client.Session;
                            WireManagers();
                            ConnectionChanged?.Invoke(true);
                        }
                        catch
                        {
                            // wait
                        }
                    }
                    try { await Task.Delay(delay, token).ConfigureAwait(false); } catch { /* cancelled */ }
                }
            });
        }

        public void StopAutoReconnect() => _reconnectCts?.Cancel();

        //Monitor PLC DateTime
        public void MonitorPlcDateTime(Action<string> OnTimeChanges)
        {
            var nodeDescription = _reader.GetNodeByName(_adminNodeId, "PLCDateTime");
            if (nodeDescription != null)
            {
                NodeId plcNodeId = (NodeId)nodeDescription.NodeId;
                _subscriptionService.StartMonitoring(plcNodeId, nodeDescription.DisplayName.Text, (value) =>
                {
                    var timeStamp = value.SourceTimestamp;
                    string deTime = "W. Europe Standard Time";
                    TimeZoneInfo deTz = TimeZoneInfo.FindSystemTimeZoneById(deTime);
                    var localTime = (TimeZoneInfo.ConvertTimeFromUtc(timeStamp, deTz)).ToString("HH:mm:ss");
                    OnTimeChanges?.Invoke(localTime);

                });
            }
        }

        //Monitor state current Time
        public async void MonitorStateCurrentTimeNode(Action<string> OnTimeChanged)
        {
            var unitMode = await LoadUnitMode();
            var stateCurrentMode = await LoadStateCurrent();

            string nodeName = $"StateCurrentTime[{unitMode},{stateCurrentMode}]";
            var nodeDescription = _reader.GetNodeByName(_adminNodeId, nodeName);

            if (nodeDescription != null)
            {
                NodeId nodeId = (NodeId)nodeDescription.NodeId;

                _subscriptionService.StartMonitoring(nodeId, nodeName, (value) =>
                {
                    if (value?.Value is int seconds)
                    {
                        TimeSpan time = TimeSpan.FromSeconds(seconds);
                        string formatted = time.ToString(@"hh\:mm\:ss");
                        OnTimeChanged?.Invoke(formatted);
                    }
                });
            }
            else
            {
                Console.WriteLine($"Node not found: {nodeName}");
            }
        }

        //monitoring single value node
        public void MonitorSingleNodeValue(string nodeName, Action<int> onStateChanged)
        {
            NodeId currentStateNodeId = new NodeId($"{_statusNodeId}.{nodeName}");

            _subscriptionService.StartMonitoring(currentStateNodeId, nodeName, (value) =>
            {
                if (value?.Value is int state)
                {
                    onStateChanged?.Invoke(state);
                }
            });
        }

        // Monitoring all admin node based on the nodename
        public void MonitorNodes(string nodeName, Action<string> OnTimeValueChange)
        {
            var nodeDesc = _reader.GetNodeByName(_adminNodeId, nodeName);
            if (nodeDesc != null)
            {
                NodeId nodeId = (NodeId)nodeDesc.NodeId;
                _subscriptionService.StartMonitoring(nodeId, nodeDesc.DisplayName.Text, (value) =>
                {
                    if (value != null && value.Value != null)
                    {
                        OnTimeValueChange?.Invoke(value.Value.ToString());
                    }
                });
            }
        }

        // Monitoring all status node based on the nodename
        public void MonitorStatusNodes(string nodeName, Action<string> OnTimeValueChange)
        {
            var nodeDesc = _reader.GetNodeByName(_statusNodeId, nodeName);
            if (nodeDesc != null)
            {
                NodeId nodeId = (NodeId)nodeDesc.NodeId;
                _subscriptionService.StartMonitoring(nodeId, nodeDesc.DisplayName.Text, (value) =>
                {
                    if (value != null && value.Value != null)
                    {
                        OnTimeValueChange?.Invoke(value.Value.ToString());
                    }
                });
            }
        }

        /// <summary>
        /// Monitor a single Status.Product[index].Ingredients[0].IngredientID
        /// </summary>
        public void MonitorProductIngredientId(int productIndex, Action<int> onChanged, int samplingMs = 500)
        {
            if (_subscriptionService == null) return;

            //Product Node id
            var nodeId = new NodeId($"{_statusNodeId}.Product[{productIndex}].Ingredients[0].IngredientID");

            string display = $"Product[{productIndex}].Ingredients[0].IngredientID";

            _subscriptionService.StartMonitoring(nodeId, display, (dv) =>
            {
                if (dv?.Value == null) return;
                if (int.TryParse(dv.Value.ToString(), out var v))
                {
                    //Console.WriteLine($"[MONITOR] {display} => {v}");
                    onChanged?.Invoke(v);
                }
                else
                {
                    //Console.WriteLine($"[MONITOR] {display} => (non-int: {dv.Value})");
                }
            }, samplingMs);
        }


        /// <summary>
        /// Monitor ALL Product[0]..Product[37] Ingredients[0].IngredientID
        /// and return (index, value) on each change.
        /// </summary>
        public void MonitorAllProductIngredientIds(Action<int, int> onChanged, int samplingMs = 500)
        {
            for (int i = 0; i <= 37; i++)
            {
                int idx = i;
                MonitorProductIngredientId(idx, v => onChanged(idx, v), samplingMs);
            }
        }

        public async Task<List<AlarmHistoryModel>> LoadAlarmHistoryAsync() {return await _manager.GetAlarmHistoryAsync();}

        public async Task<List<AlarmModel>> LoadAlarmAsync() { return await _manager.GetAlarmAsync(); }

        // alarm without async method
        public Task<List<AlarmModel>> LoadAlarmDesc() { return _manager.GetAlarmAsync(); }

        //productprocessed count details
        public async Task<List<ProdProcessedModel>> LoadProdProcessedAsync() { return await _manager.GetProdProcessedAsync(); }

        //product measured
        public async Task<List<ProdConsumedModel>> LoadProdConsumedAsync() { return await _manager.GetProdConsumedAsync(); }

        //product defect
        public async Task<List<ProdDefectCountModel>> LoadProdDefectAsync() { return await _manager.GetProdDefectAsync(); }

        //pallet info List
        public async Task<List<PalletInfoModel>> LoadPalletInfoAsync() { return await _statusManager.GetPalletInfoAsync(); }

        //stop reason
        public async Task<(int, int, string, DateTime[])> LoadStopReasonAsync()
        {
            var stopReason = await _manager.GetStopReasonAsync();
            if (stopReason != null)
            {
                var items = stopReason.FirstOrDefault();
                return (items.Category, items.Value, items.Message, items.DateTime);
            }
            return (0, 0, null, null);
        }
        //state current(machine status)

        public async Task<int> LoadStateCurrent()
        {

            var value = await _statusManager.GetStateCurrentAsync();
            if (value is int stateValue)
                return stateValue;
            return -1;
        }

        public async Task<int> LoadUnitMode()
        {

            var value = await _statusManager.GetUnitModeCurrentAsync();
            if (value is int unitModeValue)
                return unitModeValue;
            return -1;
        }

        public async Task<int> LoadStateCurrentTime(int unitMode, int stateCurrent)
        {
            NodeId stateCurrentTimeNodeId = new NodeId($"{_adminNodeId}.StateCurrentTime[{unitMode},{stateCurrent}]");
            var result = await _reader.ReadSingleNodeValue(stateCurrentTimeNodeId);
            //Console.WriteLine($">>>>>>>>>>>>>>>>>>>>{result}<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
            if (result is int value)
                return value;

            return -1;
        }
        public async Task<int> LoadExecutionTime(int unitMode, int stateCurrent)
        {
            NodeId stateCurrentTimeNodeId = new NodeId($"ns=4;s=|var|CODESYS Control Win V3 x64.Application.PackTag.CoraMeasure.Admin.StateCurrentTime[{unitMode},{stateCurrent}]");
            var result = await _reader.ReadSingleNodeValue(stateCurrentTimeNodeId);
            //Console.WriteLine($">>>>>>>>>>>  execution  <<<<<<<<<<<<<<<>>>>>> {result}  <<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<");
            if (result is int value)
                return value;

            return -1;
        }

        // pallet ui monitoring
        public void MonitorIngredient(int productIndex, Action<int> onChanged, int samplingMs = 500)
        {
            if (_reader == null || _subscriptionService == null) return;

            string nodePath = $"Product[{productIndex}].Ingredients[0].IngredientID";
            var nodeDesc = _reader.GetNodeByName(_statusNodeId, nodePath);
            if (nodeDesc == null) return;

            var nodeId = (NodeId)nodeDesc.NodeId;
            _subscriptionService.StartMonitoring(nodeId, nodePath, dv =>
            {
                if (dv?.Value == null) return;
                if (int.TryParse(dv.Value.ToString(), out var v)) onChanged(v);
            }, samplingMs);
        }


        //product processed count as return
        public async Task<(int, int)> GetProdProcessing()
        {
            try
            {
                var prodProcessed = await LoadProdProcessedAsync();
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
        // proddefect 
        public async Task<(int, int, int)> GetProdDefect()
        {
            try
            {
                var prodDefect = await LoadProdDefectAsync();
                if (prodDefect != null)
                {
                    var item = prodDefect.First();
                    return (item.ID, item.Count, item.AccCount);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Loading GetProdDefect() method " + e);
            }
            return (0, 0, 0);
        }
        public async Task<(int, int, int)> GetProdMeasured()
        {
            try
            {
                var prodMeasured = await LoadProdConsumedAsync();
                if (prodMeasured != null)
                {
                    var item = prodMeasured.First();
                    return (item.ID, item.Count, item.AccCount);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Loading GetProdMeasured() method " + e);
            }
            return (0, 0, 0);
        }
        public async void WriteDataToTextFile()
        {
            var state = await _statusManager.GetStateCurrentAsync();
            if (state == 1 || state == 0 || state == 5 || state == 6)
            {
                (int prodProcessedCount, int prodProcessedAccCount) = await GetProdProcessing();
                (int prodDefectID, int prodDefectCount, int prodDefectAccCount) = await GetProdDefect();
                (int prodMeasuredID, int prodMeasuredCount, int prodMeasuredAccCount) = await GetProdMeasured();

                DateTime current = DateTime.Now;
                string date = current.ToString("dd:MM:yyyy");
                string time = current.ToString("HH:mm:ss");

                //defect file
                var defectFile = Path.Combine(FileSystem.AppDataDirectory, "DefectDataBank.csv");
                if (!File.Exists(defectFile))
                    await File.AppendAllTextAsync(defectFile, "Item,Count,AccCount,Date,Time\n");
                string defectsDetails = $"{prodDefectID}, {prodDefectCount}, {prodDefectAccCount}, {date}, {time}\n";
                await File.AppendAllTextAsync(defectFile, defectsDetails);

                // measured

                var measureFile = Path.Combine(FileSystem.AppDataDirectory, "MeasuredDataBank.csv");
                if (!File.Exists(measureFile))
                    await File.AppendAllTextAsync(defectFile, "Item,Count,AccCount,Date,Time\n");
                string measuresDetails = $"{prodMeasuredID}, {prodMeasuredCount}, {prodMeasuredAccCount}, {date}, {time}\n";
                await File.AppendAllTextAsync(measureFile, measuresDetails);
                Console.WriteLine("Data written to the file");

            }
        }

        //machine state

        public (string, string, Color) MachineStateCurrent(int status)
        {
            string txtMachineStatus;
            string svgSource;
            Color statusColor;

            switch (status)
            {
                case 0:
                    txtMachineStatus = "Stopped";
                    svgSource = "redstop.svg";
                    statusColor = Colors.Red;
                    break;
                case 1:
                    txtMachineStatus = "Resetting";
                    svgSource = "blue.png";
                    statusColor = Colors.Blue;
                    break;
                case 2:
                    txtMachineStatus = "Idle";
                    svgSource = "blue.png";
                    statusColor = Colors.Orange;
                    break;
                case 3:
                    txtMachineStatus = "Starting";
                    svgSource = "orange.png";
                    statusColor = Colors.Orange;
                    break;
                case 4:
                    txtMachineStatus = "Executing";
                    statusColor = Colors.Green;
                    svgSource = "green.png";
                    break;
                case 5:
                    txtMachineStatus = "Holding";
                    svgSource = "blue.png";
                    statusColor = Colors.Orange;
                    break;
                case 6:
                    txtMachineStatus = "Hold";
                    svgSource = "yellow.png";
                    statusColor = Colors.Yellow;
                    break;
                case 7:
                    txtMachineStatus = "UnHolding";
                    svgSource = "yellow.png";
                    statusColor = Colors.Yellow;
                    break;
                case 8:
                    txtMachineStatus = "Suspending";
                    svgSource = "redabort.png";
                    statusColor = Colors.Red;
                    break;
                case 9:
                    txtMachineStatus = "Suspened";
                    svgSource = "redstop.png";
                    statusColor = Colors.Red;
                    break;
                case 10:
                    txtMachineStatus = "UnSuspending";
                    svgSource = "redabort.svg";
                    statusColor = Colors.Red;
                    break;
                case 11:
                    txtMachineStatus = "Completing";
                    svgSource = "greentick.png";
                    statusColor = Colors.Green;
                    break;
                case 12:
                    txtMachineStatus = "Completed";
                    svgSource = "greentick.png";
                    statusColor = Colors.Green;
                    break;
                case 13:
                    txtMachineStatus = "Stopping";
                    svgSource = "redabort.png";
                    statusColor = Colors.Red;
                    break;
                case 14:
                    txtMachineStatus = "Aborting";
                    svgSource = "redabort.png";
                    statusColor = Colors.Red;
                    break;
                case 15:
                    txtMachineStatus = "Aborted";
                    svgSource = "redabort.png";
                    statusColor = Colors.Red;
                    break;
                case 16:
                    txtMachineStatus = "Clearing";
                    svgSource = "redabort.png";
                    statusColor = Colors.Red;
                    break;
                default:
                    txtMachineStatus = "Unknown";
                    svgSource = "blueunknown";
                    statusColor = Colors.Blue;
                    break;
            }

            return (txtMachineStatus, svgSource, statusColor);
        }


        // return machine unit mode
        public (string, Color) MachineUnitMode(int mode)
        {
            string txtMachineMode;
            Color modeColor;
            switch (mode)
            {
                case 0:
                    txtMachineMode = "Automatic Mode";
                    modeColor = Colors.Green;
                    break;
                case 1:
                    txtMachineMode = "Semi-Automatic Mode";
                    modeColor = Colors.Green;
                    break;
                case 2:
                    txtMachineMode = "Service/Manual Mode";
                    modeColor = Colors.Orange;
                    break;
                default:
                    txtMachineMode = "Unknown";
                    modeColor = Colors.Red;
                    break;
            }

            return (txtMachineMode, modeColor);
        }

        public async Task LoadandDisplayExecutionTime()
        {
            var unitMode = await LoadUnitMode();
            var stateCurrentMode = await LoadStateCurrent();
        }

        //pallet name
        public string PalletName(float value)
        {
            string adapterName;

            switch (value)
            {
                case 0:
                    adapterName = "No Pallet";
                    break;
                case 1:
                    adapterName = "HSK63";
                    break;
                case 2:
                    adapterName = "Capto_C6";
                    break;
                case 3:
                    adapterName = "HSK40";
                    break;
                case 4:
                    adapterName = "Spare01";
                    break;
                case 5:
                    adapterName = "HSK125";
                    break;
                case 6:
                    adapterName = "Capto_C4";
                    break;
                case 7:
                    adapterName = "HSK63";
                    break;
                case 8:
                    adapterName = "Spare02";
                    break;
                case 9:
                    adapterName = "SK30";

                    break;
                case 10:
                    adapterName = "Capto_C5";
                    break;
                case 11:
                    adapterName = "HSK100";
                    break;
                case 12:
                    adapterName = "Capto_C8";
                    break;
                case 13:
                    adapterName = "HSK50";
                    break;
                case 14:
                    adapterName = "SK50";
                    break;
                default:
                    adapterName = "Unknown";
                    break;
            }
            return adapterName;
        }

        //get pallte details
        public async Task<(string, string, int, float)> GetPalletInformationAsyc()
        {
            var palletDetails = await LoadPalletInfoAsync();

            if (palletDetails != null)
            {
                var item = palletDetails.LastOrDefault();

                if (item != null && item?.Value >= 1)
                {
                    return (item.Name, item.Unit, item.ID, item.Value);
                }

                return ("No pallet available", "0", 0, 0);
            }
            return ("No pallet available", "0", 0, 0);
        }

        //end class
    }
}
