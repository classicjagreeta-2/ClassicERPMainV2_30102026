using ClassicErpMainV2.Models;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Xml.Linq;
using static ClassicErpMainV2.Models.CompanySelection;

namespace ClassicErpMainV2.Controllers
{
    public class ModuleController : Controller
    {
        Logger logger = LogManager.GetCurrentClassLogger();

        public async Task<ActionResult> SelectModule()
        {
            List<MODULE> user_jag = new List<MODULE>();
            try
            {
                using (var db = new DatabaseContext())
                {
                    var _user_schema_link = db.USER_SCHEMA_LINK.Where(k => k.USER_ORA == Helpers.UserInfo.getUserName).ToList();
                    var _module = db.MODULE.ToList();
                    //var _join = _module.Join(_user_schema_link, x => x.MODULE_CODE, y => y.MODULE_CODE, (x, y) => x).Distinct().ToList();
                    var _join = _module.Join(_user_schema_link, x => x.MODULE_NAME, y => y.MODULE_NAME, (x, y) => x).Distinct().ToList();
                    var VB6 = _join.Where(k => k.TECHNOLOGY != ("MVC")).ToList();
                    var MVC = _join.Where(k => k.TECHNOLOGY == ("MVC")).ToList();
                    List<string> technology = new List<string>() { "VB6", "MVC" };
                    var ddta = _join.ToList().DistinctBy(k => k.MODULE_NAME).ToList();
                    if (_join.Count == 0)
                    {
                        return RedirectToAction("Login", "Auth");
                    }
                    return View(_join);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<ActionResult> RedirectToModule(string code, string controlschema, string menuschema, string sortby, string mname)
        {
            var username = Session["Username"];
            var password = Session["Password"];
            var LastLoginDt = Session["LastLoginDt"];
            string file = Server.MapPath("~/Config.json");
            string Json = System.IO.File.ReadAllText(file);
            var configJson = JsonConvert.DeserializeObject<List<RootRedirection>>(Json);
            var res = configJson.Where(k => k.modulecode.Contains(code.ToUpper())).FirstOrDefault();
            var modulecode = res.modulecode.Where(j => j == code).FirstOrDefault();
            var isSubDomain = res.isSubDomain;
            string URL = "";
            if (res != null)
            {
                //var plainTextBytes = Encoding.UTF8.GetBytes(code);
                var module = EncryptionHelper.Encrypt(code); //Convert.ToBase64String(plainTextBytes);
                //var _db = EncryptionHelper.Encrypt(Helpers.UserInfo.getDbName); //Convert.ToBase64String(Encoding.UTF8.GetBytes(Helpers.UserInfo.getDbName));
                if (isSubDomain == true)
                {
                    //URL = res.hosturl + modulecode + res.redirection + Helpers.UserInfo.getToken + string.Format("&module={0}", module);
                    var hosturl = $"{res.hosturl}{code}";
                    string json = string.Format(res.redirection, hosturl, code, controlschema, menuschema, sortby, mname, username, LastLoginDt, password);
                    URL = json;

                }
                else
                {
                    //URL = res.hosturl + string.Format("/Module/CompanySelectionByPass?token={0}&module={1}&loca={2}", Helpers.UserInfo.getToken, module, _db);
                    //URL = res.hosturl + res.redirection + string.Format("?token={0}&module={1}&loca={2}", Helpers.UserInfo.getToken, encode, _db);
                    //URL = string.Format("{0}/{1}?code={2}&controlschema={3}&menuschema={4}&sortby={5}&mname={6}&UserName={7}&LastLoginDt={8}", res.hosturl, res.redirection, code, controlschema, menuschema, sortby, mname, username, LastLoginDt);
                    string json = string.Format(res.redirection, res.hosturl, code, controlschema, menuschema, sortby, mname, username, LastLoginDt, password);
                    URL = json;
                }
            }
            else
            {
                URL = "/";
            }
            return Redirect(URL);
        }
        public async Task<ActionResult> CompanySelection(string code, string controlschema, string menuschema, string sortby, string mname)
        {
            Helpers.UserInfo.ModuleCode = code;
            var responce = await Services.GetAsync<List<CompanySelection>>(string.Format("api/Company/GetByModule?module={0}", code));
            SelectListModel selectListModel = new SelectListModel();
            if ((!responce.IsSuccessStatusCode))
            {
                logger.Warn("Error occured in Api" + responce.StatusCode);
                return RedirectToAction("Login", "Auth");
            }
            else
            {
                var data = responce.Data.ToList();
                selectListModel.CompanyList = data.Select(x => new SelectListItem { Text = x.Company.ToString(), Value = x.Company.ToString() });
                selectListModel.LocationList = data.Select(x => new SelectListItem { Text = "", Value = "" });
                selectListModel.FinancialYearList = data.Select(x => new SelectListItem { Text = "", Value = "" });
            }
            return View(selectListModel);
        }

        [HttpPost]
        public async Task<ActionResult> CompanySelection(string CompanyName, string LocationName, string FinancialYear)
        {
            var modulecode = Helpers.UserInfo.ModuleCode;
            SelectListModel model = new SelectListModel();
            if (!string.IsNullOrEmpty(CompanyName))
            {
                var responce = await Services.GetAsync<List<CompanySelection>>(string.Format("api/Company/GetByModule?module={0}", modulecode));
                var data = responce.Data.ToList();
                model.CompanyList = data.Select(x => new SelectListItem { Text = x.Company.ToString(), Value = x.Company.ToString() });
                if (!string.IsNullOrEmpty(CompanyName))
                {
                    var locationsList = data.Where(l => l.Company == CompanyName).SelectMany(k => k.locationSelection);
                    var distinctLocation = locationsList.Select(l => l.Location).Distinct();
                    model.LocationList = distinctLocation.Select(l => new SelectListItem { Text = l, Value = l });
                    model.FinancialYearList = data.Select(x => new SelectListItem { Text = "", Value = "" });
                    if (!string.IsNullOrEmpty(LocationName))
                    {
                        var fyear = locationsList.Select(k => k.FinancialYear);
                        model.FinancialYearList = fyear.Select(l => new SelectListItem { Text = l, Value = l });
                    }

                    if (!string.IsNullOrEmpty(FinancialYear))
                    {
                        var database = locationsList.Where(l => l.FinancialYear == FinancialYear).Select(k => k.Database).FirstOrDefault();
                        Helpers.UserInfo.getDbName = database;
                    }
                }
            }
            else
            {
                //model.CompanyList = new SelectListItem { Text = "", Value = "" };
                //model.LocationList = data.Select(x => new SelectListItem { Text = "", Value = "" });
                //model.FinancialYearList = data.Select(x => new SelectListItem { Text = "", Value = "" });
            }
            return View(model);
        }
    }

    public class ModuleHierarchy
    {
        public string ModuleName { get; set; }
        public List<MODULE> Module { get; set; } = new List<MODULE>();
    }
    public class SelectListModel
    {
        //public SelectListModel()
        //{
        //    this.CompanyList = new IEnumerable<SelectListItem>();
        //    this.LocationList = new IEnumerable<SelectListItem>();
        //    this.FinancialYearList = new IEnumerable<SelectListItem>();
        //}

        public IEnumerable<SelectListItem> CompanyList { get; set; }
        public IEnumerable<SelectListItem> LocationList { get; set; }
        public IEnumerable<SelectListItem> FinancialYearList { get; set; }

        public string CompanyName { get; set; }
        public string LocationName { get; set; }
        public string FinancialYear { get; set; }
    }

    public class RootRedirection
    {
        public string hosturl { get; set; }
        public string redirection { get; set; }
        public bool? isSubDomain { get; set; }
        public List<string> modulecode { get; set; }
    }






    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SubCategory
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
    }
}