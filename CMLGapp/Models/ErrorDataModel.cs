using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMLGapp.Models
{
    public class ErrorDataModel
    {
        public int errorId { get; set; }
        public string errorName { get; set; }
        public int errorValue { get; set; }
        public int errorCategory { get; set; }
        public string solution { get; set; }
    }
}
