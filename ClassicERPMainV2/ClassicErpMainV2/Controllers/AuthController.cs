using ClassicErpMainV2.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Security.Claims;
using NLog;
using System.Web.Configuration;
using System.Text;
using Microsoft.VisualBasic;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace ClassicErpMainV2.Controllers
{
    public class AuthController : Controller
    {
        Logger logger = LogManager.GetCurrentClassLogger();
        // GET: Login
        public ActionResult Login()
        {
            return View();
        }
        public async Task<ActionResult> Auth(Model_User model_User)
        {
            Session.Add("Username", model_User.UserName);
            Session.Add("Password", model_User.Password);
            ViewBag.ErrorMsg = string.Empty;
            List<USER_JAG> user_jag = new List<USER_JAG>();
            try
            {
                var results = await Services.GetAsync<COMPANY_LICENSE_FULL>($"/Users/SaasUserLimitCheck?userName={model_User.UserName}");

                if (results.IsSuccessStatusCode && !results.Data.Success)
                {
                    results.Message = results.Data.Message;
                    ModelState.AddModelError("CustomError", results.Message);
                    ViewBag.ErrorMsg = results.Message;
                    return Json(results); //View("Login");
                }

                using (var sqlDb = new DatabaseContext())
                {
                    user_jag = sqlDb.USER_JAG.Where(k => k.USER_ORA == model_User.UserName).ToList();
                }

                var Return = await Services.PostAsync<UserModels>("/Users/authenticate", model_User);
                //var Return = Authenticate(model_User.UserName, model_User.Password, user_jag);

                if (Return != null)
                {
                    if (Return.IsSuccessStatusCode)
                    {
                        var uuuu = await Services.PostAsync<UserModels>($"/Users/SaasUserSessionCreate?userName={model_User.UserName}", null);

                        Helpers.UserInfo.getUserName = model_User.UserName;
                        Session["IsAuthenticate"] = model_User.UserName;
                        // 2. REQUIRED FOR [Authorize]: Issue the Forms Authentication Cookie
                        FormsAuthentication.SetAuthCookie(model_User.UserName, createPersistentCookie: false);
                        return Json(new
                        {
                            result = "Redirect",
                            url = Url.Action("SelectModule", "Module")
                        });
                    }
                    else
                    {
                        ModelState.AddModelError("CustomError", Return.Message);
                        ViewBag.ErrorMsg = Return.Message;
                        return Json(Return); //View("Login");
                    }
                }
                else
                {
                    logger.Warn("Error occured in Home controller Index Action");
                    return Json(new { result = "Redirect", url = Url.Action("Login") });
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public AuthenticateResponse Authenticate(string Username, string Password, List<USER_JAG> user_jag)
        {
            Session.Add("Username", Username);
            Session.Add("Password", Password);
            int MULT = 1;
            decimal SEED = 0;
            string MWORD = "";
            decimal RAND2 = 0;
            decimal[] RAND1 = new decimal[10];

            if (user_jag == null) return null;
            if (user_jag.Count == 0) return null;
            var user = new User();
            try
            {
                user = user_jag.Select(k => new User
                {
                    FirstName = k.USER_NAME,
                    Id = k.ID,
                    Username = k.USER_ORA,
                    Password = k.PASSWORD,
                    Access_Str = k.ACCESS_STR,
                    LastLoginDt = k.LASTLOGGEDON,
                    //Expires = GenerateExpiresTime()
                }).FirstOrDefault();
                Session.Add("LastLoginDt", user.LastLoginDt);

                if (user.Password == Password)
                {

                }
                else
                {
                    string XACSTR = user.Access_Str;
                    string Xuser = user.Username.PadRight(10, ' ');

                    for (int i = 0; i <= 9; i++)
                    {
                        int val = Strings.Asc(Xuser.Substring(i, 1)) - Strings.Asc('A');
                        SEED = SEED + (val * MULT);
                        MULT = MULT * 5;
                    }

                    for (int i = 0; i <= 9; i++)
                    {
                        RAND1[i] = Math.Round(SEED * 7137421);
                        RAND1[i] = RAND1[i] + 21132487;
                        RAND2 = RAND1[i] / 10000000;
                        RAND1[i] = RAND1[i] - (Math.Floor(RAND2) * 10000000);
                        RAND1[i] = Math.Floor(RAND1[i] / 100000);
                        SEED = SEED + 1;
                    }

                    try
                    {
                        for (int i = 0; i <= 9; i++)
                        {
                            string char1 = XACSTR.Substring(i, 1);
                            var ass = Strings.Asc(char1);
                            int dd = Strings.Asc(char1);
                            decimal val = dd - RAND1[i];
                            int ff = Convert.ToInt32(val);
                            char chr = Strings.Chr(ff);
                            MWORD = MWORD + chr;
                        }
                    }
                    catch (Exception ex)
                    {
                        var enc = CodePagesEncodingProvider.Instance.GetEncoding(1252);
                        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                        for (int i = 0; i <= 9; i++)
                        {
                            string char1 = XACSTR.Substring(i, 1);
                            var ass = Strings.Asc(char1);
                            int dd = Strings.Asc(char1);
                            decimal val = dd - RAND1[i];
                            int ff = Convert.ToInt32(val);
                            char chr = Strings.Chr(ff);
                            MWORD = MWORD + chr;
                        }
                    }

                    if (!string.IsNullOrEmpty(MWORD))
                    {
                        // for Trim the value
                        MWORD = MWORD.Trim();
                        user.Password = MWORD;
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }


            if (Password != MWORD)
            {
                return null;
            }

            return new AuthenticateResponse(user, "token", "refreshToken");
        }

        [HttpPost]
        public async Task<JsonResult> UserIdleNotification()
        {
            var username = Helpers.UserInfo.getUserName;
            var uuuu = await Services.PostAsync<UserModels>($"/Users/SaasUserSessionRemove?userName={username}", null);
            return Json(new { success = true, message = "User identity not found." });
        }
    }

    public class UserModels
    {
        public int id { get; set; }
        public string firstName { get; set; }
        public object lastName { get; set; }
        public string username { get; set; }
        public string accessToken { get; set; }
        public string refreshToken { get; set; }
    }
    public class COMPANY_LICENSE_FULL
    {
        public bool Success { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

}


