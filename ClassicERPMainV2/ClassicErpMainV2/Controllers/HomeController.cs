using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.DynamicData;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace ClassicErpMainV2.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            using (var sqlDb = new DatabaseContext())
            {
                var sqlProducts = sqlDb.MODULE.ToList();
            }
            return View();
        }

        public ActionResult GetMenu()
        {
            return View("_NavMenu");
            //return PartialView("_Menu");
        }

        //[Authorize]
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            var IsAuthenticated = User.Identity.IsAuthenticated;
            //List<Claim> claims = new List<Claim>
            //{
            //    new Claim("Name", "PhoneNumber")
            //};
            //var claim = new Claim("PhoneNumber", "Password");
            //var identity = new ClaimsIdentity(new[] { claim }, "BasicAuthentication"); // this uses basic auth
            //var principal = new ClaimsPrincipal(identity);
            //HttpContext.User = principal;
            //bool result = HttpContext.User.Identity.IsAuthenticated;
            return View();
        }

        public async Task<ActionResult> getData(string Param1, string q, int _page = 1, int _limit = 10)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "http://103.234.116.64:90/inv_api/api/HO_T_SAMPLE_DRAW/GetByPage?search=" + q + "&page=" + _page + "&pageSize=" + _limit);
            request.Headers.Add("accept", "*/*");
            request.Headers.Add("x-database", "FACT_JSTIL2027");
            request.Headers.Add("x-module", "BOUGHTLEAF");
            request.Headers.Add("x-dbtype", "ORACLE");
            request.Headers.Add("UserName", "CORETEAM");
            request.Headers.Add("Password", "CORE55%%");
            request.Headers.Add("x-docyear", "2026");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var results = JsonConvert.DeserializeObject<Root>(json);
            var responseDta = new
            {
                Data = results.Value.Results,
                Total = results.Value.TotalCount
            };
            return Json(responseDta, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SubmitSelection(string selectedDocNo)
        {
            if (!string.IsNullOrEmpty(selectedDocNo))
            {
                // Do something with the selected DocNo (e.g., fetch details, save to database)
                ViewBag.Message = "Selected DocNo: " + selectedDocNo;
            }

            return Content("Submit Selection");
        }
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

    }

    public class Root
    {
        [JsonProperty("value")]
        public Value Value { get; set; }
    }

    public class Value
    {
        [JsonProperty("results")]
        public List<Result> Results { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("currentPage")]
        public int CurrentPage { get; set; }

        [JsonProperty("pageSize")]
        public int PageSize { get; set; }

        [JsonProperty("pageCount")]
        public int PageCount { get; set; }

        [JsonProperty("rowCount")]
        public int RowCount { get; set; }

        [JsonProperty("firstRowOnPage")]
        public int FirstRowOnPage { get; set; }

        [JsonProperty("lastRowOnPage")]
        public int LastRowOnPage { get; set; }
    }

    public class Result
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("uniT_NAME")]
        public string UnitName { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        [JsonProperty("gloca")]
        public string Gloca { get; set; }

        [JsonProperty("docno")]
        public string DocNo { get; set; }

        [JsonProperty("docdt")]
        public DateTime DocDate { get; set; }

        [JsonProperty("salE_CENTRE")]
        public string SaleCentre { get; set; }

        [JsonProperty("salE_TYPE")]
        public string SaleType { get; set; }

        [JsonProperty("broker")]
        public string Broker { get; set; }

        [JsonProperty("salE_CENTRE_NAME")]
        public string SaleCentreName { get; set; }

        [JsonProperty("saletypE_NAME")]
        public string SaleTypeName { get; set; }

        [JsonProperty("warehousE_NAME")]
        public string WarehouseName { get; set; }

        [JsonProperty("brokeR_NAME")]
        public string BrokerName { get; set; }

        [JsonProperty("party")]
        public string Party { get; set; }

        [JsonProperty("doC_YEAR")]
        public string DocYear { get; set; }

        [JsonProperty("partY_NAME")]
        public string PartyName { get; set; }

        [JsonProperty("lockedByUserId")]
        public string LockedByUserId { get; set; }

        [JsonProperty("lockedUntil")]
        public DateTime? LockedUntil { get; set; }

        [JsonProperty("isLock")]
        public bool IsLock { get; set; }
    }


    public class PagedResult<T>
    {
        // The dataset array expected by your JS plugin
        public IEnumerable<T> Data { get; set; }

        // Maps directly to your plugin's 'pageCountField'
        public int Total { get; set; }

        // Optional metadata if needed
        public int Page { get; set; }
        public int Limit { get; set; }
    }
}