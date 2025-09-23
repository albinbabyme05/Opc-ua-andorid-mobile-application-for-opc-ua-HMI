using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMLGapp.Models
{
    public class ProdProcessedChartModel
    {
        public string ItemName { get; set; }
        public string ProcessedDate { get; set; }
        public string ProcessedTime { get; set; }
        public int ProcessedCount { get; set; }
        public int ProcessedAccCount { get; set; }
    }
}
