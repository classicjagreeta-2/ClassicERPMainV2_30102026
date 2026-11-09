using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using ClassicErpMainV2.Areas.UserAccessMvc.Data;
using ClassicErpMainV2.Areas.UserAccessMvc.Models;
using ClassicErpMainV2.Areas.UserAccessMvc.Services;
using ClassicErpMainV2.Models;
using Microsoft.Ajax.Utilities;
using Microsoft.VisualBasic;

namespace ClassicErpMainV2.Areas.UserAccessMvc.Controllers
{
    public class UserAccessController : Controller
    {
        // In a real app inject these (Ninject/Unity/etc.) rather than newing them up.
        private readonly UserAccessRepository _repo;
        private readonly IPasswordPolicyService _passwordPolicy;
        private readonly PasswordPolicy _policy; // load from MAST_PARAM at startup, was lookupVal(...)

        public UserAccessController()
        {
            var connStr = System.Configuration.ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            var schemaPrefix = "";
            var tablePrefix = "";
            _repo = new UserAccessRepository(connStr);
            _passwordPolicy = new PasswordPolicyService();
            _policy = new PasswordPolicy { MinLength = 6, MaxLength = 15, MinNumericChars = 1, MinAlphaChars = 1, MinSpecialChars = 0 };
        }

        // GET /UserAccess — was Form_Load
        public ActionResult Index(string userOra, bool isNew = false)
        {
            Session["IS_NEW_RECORD"] = isNew;

            if (!isNew && string.IsNullOrWhiteSpace(userOra))
                //userOra = _repo.LoadFirstUserOra(); 
                return RedirectToAction("List");

            var selectedUser = isNew
                ? new UserAccessRecord { IsNewRecord = true }
                : (string.IsNullOrWhiteSpace(userOra) ? null : _repo.LoadUserRecord(userOra, CurrentLocation()));
            if (!isNew)
            {
                var pass = Decode(selectedUser.Password, selectedUser.UserOra);
                selectedUser.Password = pass;
                selectedUser.PasswordConfirm = pass;
            }

            var vm = new UserAccessIndexViewModel
            {
                MenuTree = _repo.LoadMenuTree(),
                //AssignableUnits = _repo.LoadAssignableUnits(),
                Groups = _repo.LoadGroups(),
                Modules = _repo.LoadModules(), //VBMENU_FINANCE //USRACS_FINANCE
                VoucherRightParaEnabled = LookupVoucherRightParam(),
                PasswordPolicy = _policy,
                AuthServerLabel = Session["AuthServerName"]?.ToString(), // was lblauthserver.Caption in Form_Load
                SelectedUser = selectedUser ?? new UserAccessRecord { IsNewRecord = true },
                IsNew = isNew
            };
            return View(vm);
        }

        // GET /UserAccess/GetCompanies?module=... — first cascade step for the Module/Company/Year selectors
        [HttpGet]
        public JsonResult GetCompanies(string module)
        {
            var loadcompaniesformodule = _repo.LoadClassicControlForModule(module);
            var dta = loadcompaniesformodule.Where(k => k.Module == module).DistinctBy(l => l.NAME).Select(K => new { value = K.LOCA, text = K.NAME }).ToList();
            var getModules = _repo.LoadModules().Where(k => k.MODULE_CODE == module).FirstOrDefault();
            return Json(dta, JsonRequestBehavior.AllowGet);
        }

        // GET /UserAccess/GetYears?module=...&company=... — second cascade step
        [HttpGet]
        public JsonResult GetYears(string module, string company)
        {
            Session["Loca"] = company;
            var dta = _repo.LoadClassicControlForModule(module).Where(k => k.Module == module && k.NAME == company).Select(K => new { value = K.USR_ID, text = K.YR }).ToList();
            //_repo.LoadYearsForCompany(module, company)
            return Json(dta, JsonRequestBehavior.AllowGet);
        }

        // GET /UserAccess/LoadContextRights — the button beside Year: re-loads this user's
        // menu rights and voucher rights against whichever Module/Company/Year schema was
        // selected, without a full page reload.
        [HttpGet]
        public JsonResult GetVoucherRights(string userOra, string module, string company, string year)
        {
            if (!string.IsNullOrWhiteSpace(userOra) && !string.IsNullOrWhiteSpace(module) && !string.IsNullOrWhiteSpace(company) && !string.IsNullOrWhiteSpace(year))
            {
                try
                {
                    var getClassicControlList = _repo.LoadClassicControlForModule(module);
                    var getModules = _repo.LoadModules().FirstOrDefault(k => k.MODULE_CODE == module);
                    var getControl = getClassicControlList.FirstOrDefault(k => k.Module == module && k.LOCA == company && k.USR_ID == year);

                    Session["USER_ORA"] = userOra;
                    Session["MODULE_CODE"] = module;
                    Session["LOCA"] = company;
                    Session["SCHEMA"] = year;

                    if (getModules != null)
                    {
                        Session["MODULE_NAME"] = getModules.MODULE_NAME;
                        Session["MENU_TABLE"] = getModules.MENU_TABLE;
                        Session["USRACS_TABLE"] = getModules.USRACS_TABLE;
                        Session["UNIT_TABLE"] = getModules.UNIT_TABLE;
                        Session["UNIT_FIELD"] = getModules.UNIT_FIELD;
                    }
                    if (getControl != null)
                    {
                        Session["COMPANY"] = getControl.NAME;
                        Session["FINANCIAL_YEAR"] = getControl.YR;
                    }

                    _repo.SetContextPrefix(
                        //year,
                        getModules?.MENU_TABLE ?? "",
                        getModules?.USRACS_TABLE ?? "",
                        getModules?.UNIT_FIELD ?? "",
                        getModules?.UNIT_TABLE ?? "",
                        getModules?.MODULE_NAME ?? "",
                        module
                    );

                    var menuRights = _repo.LoadMenuTree();
                    var rights = _repo.LoadUserRights(userOra);
                    if (module == "FN" || module == "FIN" || module == "FINANCE")
                    {
                        var vouchers = _repo.LoadVoucherRights(userOra, company);
                        return Json(new { success = true, rights, vouchers, menuRights }, JsonRequestBehavior.AllowGet);
                    }
                    return Json(new { success = true, rights, menuRights }, JsonRequestBehavior.AllowGet);
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Could not load rights for that context: " + ex.Message },
                        JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new
            {
                success = false,
                message = "User, Module, Company and Year are required."
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetAssignedUnits(string userOra, string module, string company, string year)
        {
            try
            {
                List<UnitLink> unitList = _repo.LoadAssignableUnits();
                List<UnitLink> unitList1 = _repo.LoadUnitsForUser(userOra);
                //_repo._schemaPrefix = year;
                var Is_Assigned = false;
                if (unitList1.Count > 0)
                {
                    Is_Assigned = true;
                }
                else if(IsNewRecord() == "False")
                {
                    Is_Assigned = false;
                }
                unitList.ForEach(x =>
                  x.IsAssigned = (!Is_Assigned) ? true : unitList1.Any(y =>
                      string.Equals(
                          x.UnitCode?.Trim(),
                          y.UnitCode?.Trim(),
                          StringComparison.OrdinalIgnoreCase)));
                return Json(new { success = true, data = unitList }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET /UserAccess/List — was the plain grid the "Back to List" button returns to
        public ActionResult List()
        {
            var vm = new UserListViewModel { Users = _repo.LoadUserList() };
            return View(vm);
        }

        // GET /UserAccess/LoadUser?userOra=... — was Text1_Change / TREEUPDATE("U")
        [HttpGet]
        public JsonResult LoadUser(string userOra)
        {
            if (string.IsNullOrWhiteSpace(userOra))
                return Json(new { found = false }, JsonRequestBehavior.AllowGet);

            var rights = _repo.LoadUserRights(userOra);
            var units = _repo.LoadUnitsForUser(userOra);
            var vouchers = _repo.LoadVoucherRights(userOra, CurrentLocation());

            return Json(new
            {
                found = rights.Any() || units.Any(),
                rights,
                units,
                vouchers
            }, JsonRequestBehavior.AllowGet);
        }

        // GET /UserAccess/LoadGroup?groupId=... — was Text4_Change / TREEUPDATE("G")
        [HttpGet]
        public JsonResult LoadGroup(string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return Json(new { found = false }, JsonRequestBehavior.AllowGet);

            var rights = _repo.LoadGroupRights(groupId);
            return Json(new { found = rights.Any(), rights }, JsonRequestBehavior.AllowGet);
        }

        // POST /UserAccess/Save — was SAVE_Click
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult Save(UserAccessSaveRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Record.UserName))
                return Json(new { success = false, message = "Enter User Name." });

            bool isNew = request.IsNewRecord;

            if (isNew && _repo.UserExists(request.Record.UserOra))
                return Json(new { success = false, message = "User already exists." });

            // Password required on create; on edit an empty password means "keep existing" —
            // matches the legacy branch (FRMMODE="E" && Text2.Text == "") which re-prompted instead.
            if (isNew && string.IsNullOrEmpty(request.Record.Password))
                return Json(new { success = false, message = "Empty password not allowed, please retry." });

            if (!string.IsNullOrEmpty(request.Record.Password))
            {
                var check = _passwordPolicy.Validate(request.Record.Password, _policy);
                if (!check.IsValid)
                    return Json(new { success = false, message = check.ErrorMessage });

                if (request.Record.Password != request.Record.PasswordConfirm)
                    return Json(new { success = false, message = "Password confirmation failed. Please try again." });
            }

            // Duplicate-unit guard — was Command3_Click's manual scan through RSDTLVIEW.
            var dupUnit = request.Record.AssignedUnits
                .GroupBy(u => u.UnitCode).FirstOrDefault(g => g.Count() > 1);
            if (dupUnit != null)
                return Json(new { success = false, message = $"Duplicate unit entry: {dupUnit.Key}" });

            // Voucher-right guard — was the C=0 check gated by Voucher_Right_Para.
            if (LookupVoucherRightParam() && !request.Record.VoucherRights.Any(v => v.Selected))
                return Json(new { success = false, message = "Without a voucher type assigned it can't be saved." });

            string encodedPassword = string.IsNullOrEmpty(request.Record.Password)
                ? null // real impl: fetch & keep existing encoded value
                : Encode(request.Record.Password, request.Record.UserOra); // was ENCODE(Text2.Text, Text1.Text)

            try
            {
                string module = !string.IsNullOrWhiteSpace(request.Module) ? request.Module : Session["MODULE_CODE"]?.ToString() ?? "";
                string company = !string.IsNullOrWhiteSpace(request.Company) ? request.Company : Session["LOCA"]?.ToString() ?? "";
                string year = !string.IsNullOrWhiteSpace(request.Year) ? request.Year : Session["SCHEMA"]?.ToString() ?? "";

                if (!string.IsNullOrEmpty(module) && !string.IsNullOrEmpty(company) && !string.IsNullOrEmpty(year))
                {
                    var getClassicControlList = _repo.LoadClassicControlForModule(module);
                    var getModules = _repo.LoadModules().FirstOrDefault(k => k.MODULE_CODE == module);
                    var getControl = getClassicControlList.FirstOrDefault(k => k.Module == module && k.LOCA == company && k.USR_ID == year);

                    Session["USER_ORA"] = request.Record.UserOra;
                    Session["MODULE_CODE"] = module;
                    Session["LOCA"] = company;
                    Session["SCHEMA"] = year;

                    if (getModules != null)
                    {
                        Session["MODULE_NAME"] = getModules.MODULE_NAME;
                        Session["MENU_TABLE"] = getModules.MENU_TABLE;
                        Session["USRACS_TABLE"] = getModules.USRACS_TABLE;
                        Session["UNIT_TABLE"] = getModules.UNIT_TABLE;
                        Session["UNIT_FIELD"] = getModules.UNIT_FIELD;
                    }
                    if (getControl != null)
                    {
                        Session["COMPANY"] = getControl.NAME;
                        Session["FINANCIAL_YEAR"] = getControl.YR;
                    }

                    _repo.SetContextPrefix(
                        //year,
                        getModules?.MENU_TABLE ?? "",
                        getModules?.USRACS_TABLE ?? "",
                        getModules?.UNIT_FIELD ?? "",
                        getModules?.UNIT_TABLE ?? "",
                        getModules?.MODULE_NAME ?? "",
                        module
                    );
                }

                _repo.SaveUserAccess(request.Record, encodedPassword, CurrentUser(), company, year, !isNew);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

            return Json(new { success = true, message = "User access changed. Restart the package to take effect of change." });
        }

        // POST /UserAccess/Delete — was DELETE_Click
        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult Delete(string userOra)
        {
            try
            {
                _repo.DeleteUser(userOra, CurrentUser());
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // --- helpers that stood in for globals / external functions in the VB6 module ---

        public JsonResult CurrentSession(string is_new_record, string userOra, string module, string company, string year)
        {
            if (!string.IsNullOrEmpty(userOra) && !string.IsNullOrEmpty(module) && !string.IsNullOrEmpty(company) && !string.IsNullOrEmpty(year))
            {
                var getClassicControlList = _repo.LoadClassicControlForModule();
                var getModules = _repo.LoadModules().Where(k => k.MODULE_CODE == module).FirstOrDefault();
                var getControl = getClassicControlList.Where(k => k.Module == module && k.LOCA == company && k.USR_ID == year).FirstOrDefault();

                Session["IS_NEW_RECORD"] = is_new_record;
                Session["USER_ORA"] = userOra;
                Session["MODULE_CODE"] = module;
                Session["MODULE_NAME"] = getModules.MODULE_NAME;
                Session["COMPANY"] = getControl.NAME;
                Session["FINANCIAL_YEAR"] = getControl.YR;
                Session["SCHEMA"] = getControl.USR_ID;
                Session["LOCA"] = company;
                Session["MENU_TABLE"] = getModules.MENU_TABLE;
                Session["USRACS_TABLE"] = getModules.USRACS_TABLE;
                Session["UNIT_TABLE"] = getModules.UNIT_TABLE;
                var sasas = CurrentUnitTable();

                _repo.SetContextPrefix(CurrentSchema(), CurrentMenuTable(), CurrentUsracsTable(), CurrentUnitTable(), "", "");
                return Json(new { success = true, message = "success" }, JsonRequestBehavior.AllowGet);

            }

            return Json(new { success = false, message = "Empty Username, Module,Company,Year not allowed, please retry. " }, JsonRequestBehavior.AllowGet);
        }
        private bool LookupVoucherRightParam()
        {
            // was: lookupVal("select nvl(vou_type_right,'N') from mast_param") = 'Y'
            return Session["VoucherRightPara"]?.ToString() == "Y";
        }

        private string IsNewRecord() => Session["IS_NEW_RECORD"]?.ToString() ?? ""; // was MUSER
        private string CurrentUser() => Session["USER_ORA"]?.ToString() ?? ""; // was MUSER
        private string CurrentModuleCode() => Session["MODULE_CODE"]?.ToString() ?? ""; // was loca1
        private string CurrentCompany() => Session["COMPANY"]?.ToString() ?? ""; // was loca1
        private string CurrentFinancialYear() => Session["FINANCIAL_YEAR"]?.ToString() ?? ""; // was loca1
        private string CurrentLocation() => Session["LOCA"]?.ToString() ?? ""; // was loca1
        private string CurrentSchema() => Session["SCHEMA"]?.ToString() ?? ""; // was loca1
        private string CurrentMenuTable() => Session["MENU_TABLE"]?.ToString() ?? ""; // was loca1
        private string CurrentUsracsTable() => Session["USRACS_TABLE"]?.ToString() ?? ""; // was loca1
        private string CurrentUnitTable() => Session["UNIT_TABLE"]?.ToString() ?? ""; // was loca1

        public string Encode(string plainPassword, string userOra)
        {
            var Username = userOra;
            var PlainText = plainPassword;
            int MULT = 1;
            decimal SEED = 0;
            string EncryptedStr = "";
            decimal RAND2 = 0;
            decimal[] RAND1 = new decimal[10];

            // Ensure inputs are padded to 10 characters
            string Xtext = (PlainText ?? "").PadRight(10, ' ').Substring(0, 10);
            string Xuser = (Username ?? "").PadRight(10, ' ').Substring(0, 10);

            // 1. Generate SEED from Username
            for (int i = 0; i <= 9; i++)
            {
                int val = Strings.Asc(Xuser.Substring(i, 1)) - Strings.Asc('A');
                SEED = SEED + (val * MULT);
                MULT = MULT * 5;
            }

            // 2. Generate pseudo-random array RAND1
            for (int i = 0; i <= 9; i++)
            {
                RAND1[i] = Math.Round(SEED * 7137421);
                RAND1[i] = RAND1[i] + 21132487;
                RAND2 = RAND1[i] / 10000000;
                RAND1[i] = RAND1[i] - (Math.Floor(RAND2) * 10000000);
                RAND1[i] = Math.Floor(RAND1[i] / 100000);
                SEED = SEED + 1;
            }

            // 3. Perform Encryption (Add offset instead of subtracting)
            for (int i = 0; i <= 9; i++)
            {
                string char1 = Xtext.Substring(i, 1);
                int asciiVal = Strings.Asc(char1);

                // Add pseudo-random offset
                int encryptedAscii = asciiVal + Convert.ToInt32(RAND1[i]);

                char chr = Strings.Chr(encryptedAscii);
                EncryptedStr += chr;
            }
            return EncryptedStr;
        }

        public string Decode(string Access_Str, string Username)
        {
            int MULT = 1;
            decimal SEED = 0;
            string MWORD = "";
            decimal RAND2 = 0;
            decimal[] RAND1 = new decimal[10];

            string XACSTR = Access_Str;
            string Xuser = Username.PadRight(10, ' ');

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
                MWORD = "";
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
            }
            return MWORD;
        }

    }

    public class UserAccessIndexViewModel
    {
        public System.Collections.Generic.List<MenuNode> MenuTree { get; set; }
        public System.Collections.Generic.List<UnitLink> AssignableUnits { get; set; }
        public System.Collections.Generic.List<string> Groups { get; set; }
        public System.Collections.Generic.List<MODULE> Modules { get; set; }
        public bool VoucherRightParaEnabled { get; set; }
        public PasswordPolicy PasswordPolicy { get; set; }
        public string AuthServerLabel { get; set; }
        public UserAccessRecord SelectedUser { get; set; }
        public bool IsNew { get; set; }
    }

    public class UserAccessSaveRequest
    {
        public UserAccessRecord Record { get; set; }
        public bool IsNewRecord { get; set; }
        public string Module { get; set; }
        public string Company { get; set; }
        public string Year { get; set; }
    }

    public class UserListViewModel
    {
        public System.Collections.Generic.List<UserListItem> Users { get; set; }
    }
}
