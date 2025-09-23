using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMLGapp.Models
{
    public class UserModel
    {
        public string UUID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
       
        public string TwoFactorSecret { get; set; }
    }

}

