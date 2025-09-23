using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcUa_Connecter.Services
{

    public class OpcBrowsingService
    {
        private Session _session;

        // create session whenever the data read
        public OpcBrowsingService(Session session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        public List<ReferenceDescription> BrowseAllChildNodes(NodeId nodeId)
        {
            try
            {
                if (_session == null || !_session.Connected)
                {
                    Console.WriteLine($"Browse Failed: OPC UA session is not connected.");
                    return new List<ReferenceDescription>();
                }

                var browser = new Browser(_session)
                {
                    BrowseDirection = BrowseDirection.Forward,
                    ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask = (int)NodeClass.Object | (int)NodeClass.Variable,
                    ResultMask = (uint)BrowseResultMask.All,
                };

                return browser.Browse(nodeId).ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Browse Failed for NodeId: {nodeId} \n error: {e}");
                return new List<ReferenceDescription>();
            }
        }


        //Browsenode by Name
        public ReferenceDescription? GetNodeByName(NodeId parentNodeId, string childName)
        {
            var data = BrowseAllChildNodes(parentNodeId).FirstOrDefault(x => x.DisplayName.Text.Equals(childName, StringComparison.OrdinalIgnoreCase));

            if (data == null)
            {
                Console.WriteLine($"null value occurs<<<<<<<<< '{childName}' >>>>>>>>>>>>>>>><<<<<<<<<<<<<<<<< '{parentNodeId}'<<<<<<<<<<<<<<<<<");
                Console.WriteLine($"Data =>>>>>>>>>> DisplayName: {data?.DisplayName.Text} <<<<<< id >>>>> {data?.BrowseName.Name} <<<<< >>> BrowseName: {data?.NodeId} <<<<<<");
                return null;
            }

            Console.WriteLine($"Data =>>>>>>>>>> DisplayName: {data.DisplayName.Text} <<<<<< id {data.NodeId}>>>>> {data.BrowseName.Name} <<<<< >>> BrowseName: {data.BrowseName.Name} <<<<<<");
            return data;
        }
        public bool IsMetadataNode(ReferenceDescription node)
        {
            var name = node.DisplayName.Text.ToLower();
            return name == "indexmax" || name == "indexmin" || name == "dimensions";
        }


        //end class
    }
}
