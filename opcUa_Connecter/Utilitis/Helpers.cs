using Opc.Ua;
using Opc.Ua.Client;
using opcUa_Connecter.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcUa_Connecter.Utilitis
{
    public class Helpers
    {
        private Session _session;
        private OpcBrowsingService _browsingService;

        public Helpers(Session session)
        {
            _session = session;
            _browsingService = new OpcBrowsingService(session);
        }

        public static int ReadInt(Session session, NodeId nodeId)
        {
            return (int)session.ReadValue(nodeId).Value;
        }
        public static double ReadDouble(Session session, NodeId nodeId) => (double)session.ReadValue(nodeId).Value;
        public static string ReadString(Session session, NodeId nodeId) => (string)session.ReadValue(nodeId).Value;


        
    }
}
