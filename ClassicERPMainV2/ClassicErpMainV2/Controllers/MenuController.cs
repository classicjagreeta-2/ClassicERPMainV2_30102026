using ClassicErpMainV2.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ClassicErpMainV2.Controllers
{
    public class MenuController : Controller
    {
        // GET: Menu
        public async Task<ActionResult> Index()
        {
            var responce = await Services.GetAsync<JObject>(string.Format("api/Menu/GetAll?moduleCode={0}", Helpers.UserInfo.ModuleCode));
            Session["MenuList"] = responce.Data["manuDetails"];
            return View();
        }

        public async Task<ActionResult> GetMenu()
        {
            var responce = await Services.GetAsync<List<MODULE>>(string.Format("api/Module/GetAll", Helpers.UserInfo.getUserName));
            Session["MenuList"] = responce;
            return View();
        }
    }
}