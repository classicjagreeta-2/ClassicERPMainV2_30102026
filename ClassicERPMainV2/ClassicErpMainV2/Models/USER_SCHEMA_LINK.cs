using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassicErpMainV2.Models
{
    public class USER_SCHEMA_LINK
    {
        public virtual int ID { get; set; }
        public virtual string LOCA { get; set; }
        public virtual string UNIT { get; set; }
        public virtual string USER_ORA { get; set; }
        public virtual string USR_ID { get; set; }
        public virtual string MODULE_NAME { get; set; }
        public virtual string USER_ENT { get; set; }
        public virtual DateTime? ENTDT { get; set; }
        public virtual string TIME_ORA { get; set; }
        public virtual string USER_NEW { get; set; }
        public virtual DateTime? ENTDT_NEW { get; set; }
        public virtual string TIME_NEW { get; set; }
        public virtual string DTAG { get; set; }
        public virtual string MACHINE_NO { get; set; }
        public virtual int? API_ALLOW { get; set; }
        public virtual string MODULE_CODE { get; set; }
        public virtual string TECHNOLOGY { get; set; }
    }
}