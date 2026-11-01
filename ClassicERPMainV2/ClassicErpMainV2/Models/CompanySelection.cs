using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Web;

namespace ClassicErpMainV2.Models
{
    public class CompanySelection
    {
        public class LocationSelection
        {
            [JsonProperty("location")]
            public string Location { get; set; }

            [JsonProperty("database")]
            public string Database { get; set; }

            [JsonProperty("financialYear")]
            public string FinancialYear { get; set; }
        }

        [JsonProperty("company")]
        public string Company { get; set; }

        [JsonProperty("locationSelection")]
        public List<LocationSelection> locationSelection { get; set; }


    }
}