using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core_APIOracle.Models
{
    [Table("MODULE")]
    public class MODULE
    {
        [Key]
        public string MODULE_CODE { get; set; }
        public string MODULE_NAME { get; set; } = String.Empty;
        public string MENU_TABLE { get; set; } = String.Empty;
        public string CONTROL_TABLE { get; set; } = String.Empty;
        public string MENU_SORT_BY { get; set; } = String.Empty;
        public string PERMISSION { get; set; } = String.Empty;
        public string USRACS_TABLE { get; set; }
        public string LOCA { get; set; }
    }
}
