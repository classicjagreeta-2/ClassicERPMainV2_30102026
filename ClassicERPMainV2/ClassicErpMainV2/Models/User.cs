using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassicErpMainV2.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Access_Str { get; set; }
        public string Database { get; set; }
        public string DatabasePassword { get; set; }
        public string DataType { get; set; }
        public string Module { get; set; }
        public DateTime? LastLoginDt { get; set; }
        public DateTime? Expires { get; set; }
    }
}