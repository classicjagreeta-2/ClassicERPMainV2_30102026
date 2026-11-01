using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassicErpMainV2.Models
{
    public class ResponseApiModel<TData>
    {
        public string StatusCode { get; set; }
        public string Message { get; set; }
        public TData Data { get; set; }
        public bool IsSuccessStatusCode { get; set; }
    }
}