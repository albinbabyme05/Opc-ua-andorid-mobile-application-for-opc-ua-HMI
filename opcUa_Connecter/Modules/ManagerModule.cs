using Opc.Ua;
using Opc.Ua.Client;
using opcUa_Connecter.Models;
using opcUa_Connecter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace opcUa_Connecter.Modules
{
    public class ManagerModule
    {
        private NodeReader _reader;
        private NodeId _adminNode;
        private SubscriptionService _subscritption;
        private NodeId _statusNode;
        

        public ManagerModule(NodeReader reader, NodeId rootNode) 
        {
            _reader = reader;
            _adminNode = rootNode;
            _statusNode = rootNode;

        }

        public ReferenceDescription GetAlarmNodes()
        {
            return _reader.GetNodeByName(_adminNode, ".Alarm");
        }
        public ReferenceDescription GetAlarmHistoryNodes()
        {
            return _reader.GetNodeByName(_adminNode, "AlarmHistory");
        }
        public ReferenceDescription GetPLCDateTimeNodes()
        {
            return _reader.GetNodeByName(_adminNode, "PLCDateTime");
        }

        public ReferenceDescription GetProdProcessedNodes()
        {
            return _reader.GetNodeByName(_adminNode, "ProdProcessedCount");
        }
        public ReferenceDescription GetProdDefectNodes()
        {
            return _reader.GetNodeByName(_adminNode, "ProdDefectiveCount");
        }
        public ReferenceDescription GetProdConsumedNodes()
        {
            return _reader.GetNodeByName(_adminNode, "ProdConsumedCount");
        }

        public Task<List<AlarmModel>> GetAlarmAsync()
        {
            var node = _reader.GetNodeByName(_adminNode, "Alarm");
            return _reader.NodeReaderService<AlarmModel>(node);
        }
        public Task<List<AlarmHistoryModel>> GetAlarmHistoryAsync()
        {
            var node = _reader.GetNodeByName(_adminNode, "AlarmHistory");
            return _reader.NodeReaderService<AlarmHistoryModel>(node);
        }

        public Task<List<ProdProcessedModel>> GetProdProcessedAsync()
        {
            var node = _reader.GetNodeByName(_adminNode, "ProdProcessedCount");
            return _reader.NodeReaderService<ProdProcessedModel>(node);
        }

        public Task<List<ProdDefectCountModel>> GetProdDefectAsync()
        {
            var node = _reader.GetNodeByName(_adminNode, "ProdDefectiveCount");
            return _reader.NodeReaderService<ProdDefectCountModel>(node);
        }

        public Task<List<PlcDateTimeModel>> GetPLCDateTimeAsync()
        {
            var node = _reader.GetNodeByName(_adminNode, "PLCDateTime");
            return _reader.ReadPLCDateTimeAsync(node);  
        }
        public Task<List<ProdConsumedModel>> GetProdConsumedAsync()
        {
            var node = _reader.GetNodeByName(_adminNode, "ProdConsumedCount");
            return _reader.NodeReaderService<ProdConsumedModel>(node);
        }

        public Task<List<PalletInfoModel>> GetPalletInfoAsync()
        {
            var node = _reader.GetNodeByName(_statusNode, "Parameter");

            return _reader.NodeReaderService<PalletInfoModel>(node);
        }
        public Task<List<StopReasonModel>> GetStopReasonAsync()
        {
            var node = _reader.GetNodeByName(_statusNode, "StopReason");

            return _reader.NodeReaderService<StopReasonModel>(node);
        }

        public async Task<int> GetStateCurrentAsync()
        {
            NodeId currentStateNodeId = new NodeId("ns=4;s=|var|CODESYS Control Win V3 x64.Application.PackTag.CoraMeasure.Status.StateCurrent");

            var result = await _reader.ReadSingleNodeValue(currentStateNodeId);

            if (result is int value)
                return value;

            return -1;
        }
        public async Task<int> GetUnitModeCurrentAsync()
        {
            NodeId unitModeCurrentNodeId = new NodeId("ns=4;s=|var|CODESYS Control Win V3 x64.Application.PackTag.CoraMeasure.Status.UnitModeCurrent");

            var result = await _reader.ReadSingleNodeValue(unitModeCurrentNodeId);

            if (result is int value)
                return value;

            return -1;
        }

        //public Task<List<ProductDetailsModel>> GetProductDetailsAsync()
        //{
        //    var node = _reader.GetNodeByName(_statusNode, "Product");

        //    var result = _reader.NodeReaderService<ProductDetailsModel>(node);

        //    return result;
        //}





    }
}
