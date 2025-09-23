using Opc.Ua;
using Opc.Ua.Client;
using opcUa_Connecter.Models;
using opcUa_Connecter.Utilitis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace opcUa_Connecter.Services
{
    public class NodeReader
    {
        private Session _session;
        private OpcBrowsingService _browsingService;


        public NodeReader(Session session)
        {
            _session = session;
            _browsingService = new OpcBrowsingService(session);
        }

        //get specific node by name
        public ReferenceDescription GetNodeByName(NodeId nodeId, string name)
        {
            return _browsingService.GetNodeByName(nodeId, name);
        }

        public async Task<List<T>> NodeReaderService<T>(ReferenceDescription nodeDescription) where T : new()
        {
            var resultList = new List<T>();
            if (nodeDescription == null)
                return resultList;

            var parentNodeId = (NodeId)nodeDescription.NodeId;
            var allChildren = _browsingService.BrowseAllChildNodes(parentNodeId);

            // Step 1: Try to detect if this is an indexed array structure using IndexMax
            int indexMax = -1;
            var indexMaxNode = allChildren.FirstOrDefault(n => n.DisplayName.Text.Equals("IndexMax", StringComparison.OrdinalIgnoreCase));

            if (indexMaxNode != null)
            {
                var value = await ReadValuesOfNodeAsync((NodeId)indexMaxNode.NodeId);
                if (value != null && int.TryParse(value.ToString(), out int parsed))
                    indexMax = parsed;
            }

            // Step 2: Determine the real data nodes
            List<ReferenceDescription> dataNodes;

            if (indexMax >= 0)
            {
                // Case 1: Indexed structure (like ProdConsumedCount[0]...[N])
                dataNodes = allChildren
                    .Where(c => Regex.IsMatch(c.DisplayName.Text, @"\[\d+\]")) // only indexed nodes
                    .Take(indexMax + 1)
                    .ToList();
            }
            else
            {
                // Case 2: Non-indexed (like ProdProcessedCount)
                dataNodes = allChildren
                    .Where(c => !_browsingService.IsMetadataNode(c)) // exclude IndexMax, IndexMin, Dimensions
                    .ToList();

                // If no children found, try using parent directly as data holder
                if (dataNodes.Count == 0)
                    dataNodes.Add(nodeDescription);
            }
            
            // Step 3: Parse all data nodes
            foreach (var node in dataNodes)
            {
                try
                {
                    var nodeId = (NodeId)node.NodeId;
                    var properties = _browsingService.BrowseAllChildNodes(nodeId);
                    T obj = new T();
                    var objProps = typeof(T).GetProperties();

                    foreach (var prop in properties)
                    {
                        var propName = prop.DisplayName.Text.ToLower();
                        var matchingProperty = objProps.FirstOrDefault(p => p.Name.ToLower() == propName);
                        if (matchingProperty == null) continue;

                        object value = await ReadValuesOfNodeAsync((NodeId)prop.NodeId);

                        if (value != null && matchingProperty.PropertyType.IsAssignableFrom(value.GetType()))
                        {
                            matchingProperty.SetValue(obj, value);
                        }
                        else if (matchingProperty.PropertyType == typeof(DateTime[]))
                        {
                            matchingProperty.SetValue(obj, await ReadDateTimeAsync((NodeId)prop.NodeId));
                        }
                    }

                    resultList.Add(obj);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing node: {node.DisplayName.Text} -> {ex.Message}");
                }
            }

            return resultList;
        }


        private async Task<object?> ReadValuesOfNodeAsync(NodeId nodeId)
        {
            try
            {
                var value = await Task.Run(() => _session.ReadValue(nodeId));
                return value.Value;
            }
            catch {
                return null;
            }
        }

        //read value of singlenode and itsvalue
        public async Task<object?> ReadSingleNodeValue(NodeId nodeId)
        {
            try
            {
                var nodeValue = await _session.ReadValueAsync(nodeId);
                return nodeValue?.Value;
            }
            catch
            {
                return null;
            }
        }



        private async Task<DateTime[]> ReadDateTimeAsync(NodeId dtNode)
        {
            var parts = new int[7]; // year, month, day, hour, minute, second, millisecond
            var children = _browsingService.BrowseAllChildNodes(dtNode);
            var pattern = new Regex(@"\[(\d+)\]");

            foreach (var childNode in children)
            {
                var match = pattern.Match(childNode.DisplayName.Text);
                if (!match.Success) continue;

                if (int.TryParse(match.Groups[1].Value, out int idx) && idx >= 0 && idx < 7)
                {
                    var val = await _session.ReadValueAsync((NodeId)childNode.NodeId);
                    if (int.TryParse(val.Value?.ToString(), out int intVal))
                        parts[idx] = intVal;
                }
            }

            try
            {
                DateTime utcTime =  new DateTime(parts[0], parts[1], parts[2], parts[3], parts[4], parts[5], parts[6], DateTimeKind.Utc);

                string deTime = "W. Europe Standard Time";
                TimeZoneInfo deTz = TimeZoneInfo.FindSystemTimeZoneById(deTime);
                DateTime localtime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, deTz);
                return new []{ localtime};
            }
            catch
            {
                //Console.WriteLine($"{dtNode}catch this time");
                return Array.Empty<DateTime>(); 
            }
        }

        public async Task<List<PlcDateTimeModel>> ReadPLCDateTimeAsync(ReferenceDescription nodeDescription)
        {
            var resultList = new List<PlcDateTimeModel>();
            if (nodeDescription == null)
                return resultList;

            var nodeId = (NodeId)nodeDescription.NodeId;

            try
            {
                var value = await _session.ReadValueAsync(nodeId);
                var parts = (value.Value as int[]);

                if (parts == null || parts.Length < 7)
                {
                    Console.WriteLine("[ERROR] Could not read PLCDateTime array correctly.");
                    return resultList;
                }
                DateTime utcTime = new DateTime(parts[0], parts[1], parts[2], parts[3], parts[4], parts[5], parts[6], DateTimeKind.Utc);
                string deTime = "W. Europe Standard Time";
                TimeZoneInfo deTz = TimeZoneInfo.FindSystemTimeZoneById(deTime);
                DateTime localtime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, deTz);
                //Console.WriteLine($"running this time =>> "+ localtime);
                resultList.Add(new PlcDateTimeModel { TimeStamp = localtime });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception reading PLCDateTime: {ex.Message}");
                resultList.Add(new PlcDateTimeModel { TimeStamp = DateTime.Now });
            }

            return resultList;
        }



        //end Class
    }
}
