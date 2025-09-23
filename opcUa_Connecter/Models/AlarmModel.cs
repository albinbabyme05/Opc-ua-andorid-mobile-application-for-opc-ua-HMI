using opcUa_Connecter.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcUa_Connecter.Models
{
    public class AlarmModel
    {
        
        public int ID { get; set; }
        public int Value { get; set; }
        public string Message { get; set; }
        public int Category { get; set; }

        public DateTime[] DateTime { get; set; }
        public DateTime[] AckDateTime { get; set; }

    }
}
