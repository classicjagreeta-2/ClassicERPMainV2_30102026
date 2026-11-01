using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassicErpMainV2.Models
{
    public class USER_JAG
    {
        public virtual int ID { get; set; }
        public virtual string LOCA { get; set; }
        public virtual string USER_ORA { get; set; }
        public virtual string PASSWORD { get; set; }
        public virtual string ACCESS_STR { get; set; }
        public virtual string P1 { get; set; }
        public virtual string USER_ENT { get; set; }
        public virtual DateTime? ENTDT { get; set; }
        public virtual string TIME_ORA { get; set; }
        public virtual string USER_NEW { get; set; }
        public virtual DateTime? ENTDT_NEW { get; set; }
        public virtual string TIME_NEW { get; set; }
        public virtual string DTAG { get; set; }
        public virtual string MACHINE_NO { get; set; }
        public virtual string GRP { get; set; }
        public virtual string USER_NAME { get; set; }
        public virtual string USER_PRIV { get; set; }
        public virtual DateTime? LASTLOGGEDON { get; set; }
        public virtual Nullable<long> TIMESLOGGEDON { get; set; }
        public virtual long? TIMESPWDCHANGED { get; set; }
        public virtual Nullable<int> PWDCYCLES { get; set; }
        public virtual DateTime? USERCREATED { get; set; }
        public virtual string ACTIVE_TAG { get; set; }
        public virtual string REMARKS { get; set; }
        public virtual string COMMENTS { get; set; }
        public virtual DateTime? LOCKDATE { get; set; }
        public virtual DateTime? UNLOCK_DATE { get; set; }
        public virtual DateTime? UPTO_DATE { get; set; }
        public virtual string MAIL_USER_NAME { get; set; }
        public virtual string MAIL_PASSWORD { get; set; }
        public virtual string SMTP_SERVER { get; set; }
        public virtual string SMTP_PORT { get; set; }
        public virtual string SMTP_SSL { get; set; }
    }
}