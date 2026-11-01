using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ClassicErpMainV2.Models
{
    [Table("MODULE")]
    public class MODULE
    {
        public string MODULE_NAME { get; set; }
        public string MENU_TABLE { get; set; }
        public string CONTROL_TABLE { get; set; }
        public string MENU_SORT_BY { get; set; }
        public string PERMISSION { get; set; }
        public string MODULE_CODE { get; set; }
        public string USRACS_TABLE { get; set; }
        public decimal ID { get; set; }
        public string LOCA { get; set; }
        public string TECHNOLOGY { get; set; }
        public string UNIT_TABLE { get; set; }
        public string UNIT_FIELD { get; set; }

    }
}