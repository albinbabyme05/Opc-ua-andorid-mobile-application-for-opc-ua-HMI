using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcUa_Connecter.Models
{
    public class OpcNodeData
    {
        public string NodeId { get; set; }
        public string DisplayName   { get; set; }
        public string BrowserName {  get; set; }    
        public string Value {  get; set; }

        public List<OpcNodeData> Nodes;

    }
}
