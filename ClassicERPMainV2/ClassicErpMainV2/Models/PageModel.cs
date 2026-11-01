using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassicErpMainV2.Models
{
    public class PageModel
    {
        public string search {  get; set; }
        public int page { get; set; }
        public int pageSize { get; set; }
    }
}