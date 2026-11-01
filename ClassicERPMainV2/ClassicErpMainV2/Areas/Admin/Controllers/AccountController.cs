using ClassicErpMainV2.Areas.Admin.Data;
using ClassicErpMainV2.Areas.Admin.Services;
using ClassicErpMainV2.Controllers;
using ClassicErpMainV2.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace ClassicErpMainV2.Areas.Admin.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserAccessRepository _repo;
        private readonly PasswordAuthentication _auth;
        public AccountController()
        {
            var connStr = System.Configuration.ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            _repo = new UserAccessRepository(connStr);
            _auth = new PasswordAuthentication(connStr);
        }

        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            Session.Clear();
            Session.Abandon();
            var checksysuser = _repo.CheckSysUser();
            if (checksysuser == null)
            {
                return RedirectToAction("SystemConfiguration",  new { area = "Admin" });
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        public ActionResult SystemConfiguration(string returnUrl)
        {
            return View();
        }

        [HttpPost]
        public ActionResult SaveSystemConfig(SystemConfigViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ErrorMessage = "Please correct the errors in the form before saving.";
                return View("SystemConfiguration", model);
            }

            try
            {
                SYSUSER sYSUSER = new SYSUSER()
                {
                    PASSWORD = CryptoHelper.Encode(model.MasterPassword, model.MasterUsername),
                    USER_CODE = model.MasterUsername,
                    USER_NAME = model.MasterFullName,
                    USER_PRIV = "Super User",
                    ACTIVE_TAG = "Y",
                };
                _repo.SaveSysUser(sYSUSER);

                TempData["SuccessMessage"] = "Administrator credentials updated successfully!";
                return RedirectToAction("Login");
            }
            catch (System.Exception ex)
            {
                ViewBag.ErrorMessage = "Error saving configuration: " + ex.Message;
                return View("SystemConfig", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(string username, string password, bool rememberMe = false, string returnUrl = null)
        {
            Session.Add("Username", username);
            Session.Add("Password", password);
            ViewBag.ErrorMsg = string.Empty;
            try
            {
                var model_User = new Model_User() { UserName = username, Password = password };
                var password_encode = CryptoHelper.Encode(password, username);
                var result = _repo.GetSysUser(username, password_encode);
                //var decode = CryptoHelper.Decode(result.PASSWORD, result.USER_CODE);

                if (result != null)
                {
                        FormsAuthentication.SetAuthCookie(username, rememberMe);
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        return RedirectToAction("Index", "Home", new { area = "Admin" });
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                    ViewBag.ErrorMsg = "Invalid username or password.";
                    ViewBag.ErrorMessage = "Invalid username or password.";
                    ViewBag.ReturnUrl = returnUrl;
                    return View();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }
    }

    public class Model_User
    {
        public string User_Name { get; set; }
        public string UserName { get; set; }
        public string LoginId { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string FKLoginId { get; set; }
        public int FkCompanyId { get; set; }
        public Nullable<int> UserType { get; set; }
        public bool? IsActive { get; set; }
        public int? GroupCategory { get; set; }
        public string LastLoginData { get; set; }
        public string DbType { get; set; }
        public string LOCA { get; set; }
        public string P1 { get; set; }
    }

    public class SYSUSER
    {
        public string USER_CODE { get; set; }
        public string PASSWORD { get; set; }
        public string USER_NAME { get; set; }
        public string USER_PRIV { get; set; }
        public DateTime? LASTLOGGEDON { get; set; }
        public decimal? TIMESLOGGEDON { get; set; }
        public decimal? TIMESPWDCHANGED { get; set; }
        public decimal? PWDCYCLES { get; set; }
        public DateTime? USERCREATED { get; set; }
        public string ACTIVE_TAG { get; set; }
    }

    public class SystemConfigViewModel
    {
        [Required(ErrorMessage = "Admin username is required")]
        [Display(Name = "Admin Username")]
        public string MasterUsername { get; set; }

        [Required(ErrorMessage = "Administrator full name is required")]
        [Display(Name = "Administrator Full Name")]
        public string MasterFullName { get; set; }

        [Required(ErrorMessage = "Admin password is required")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Password must be at least 8 characters long")]
        [DataType(DataType.Password)]
        public string MasterPassword { get; set; }

        [DataType(DataType.Password)]
        [System.ComponentModel.DataAnnotations.Compare("MasterPassword", ErrorMessage = "Passwords do not match")]
        public string MasterPasswordConfirm { get; set; }
    }
}
