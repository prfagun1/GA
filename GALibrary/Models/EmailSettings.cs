using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GALibrary.Models
{
    public class EmailSettings
    {
        public String Server { get; set; }
        public int Port { get; set; }
        public String Username { get; set; }
        public String Password { get; set; }
        public String From { get; set; }
    }
}
