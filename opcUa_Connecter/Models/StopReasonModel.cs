using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace opcUa_Connecter.Models
{
    public class StopReasonModel
    {
        public DateTime[] AckDateTime { get; set; }
        public int Category { get; set; }
        public int Value { get; set; }
        public string Message { get; set; }
        public int ID { get; set; }
        public DateTime[] DateTime { get; set; }
    }
}
