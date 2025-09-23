using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcUa_Connecter.Services
{
    public class UaRootNodeInfo
    {
        public string DisplayName;
        public string BrowseName;
        public string NodeId;
        List<UaRootNodeInfo> ChildNodes;
    }

    public class UaChildNodes
    {
        public string DisplayName;
        public string BrowseName;
        public string NodeId;
        List<UaChildNodes> SubChildNodes;
    }
}
