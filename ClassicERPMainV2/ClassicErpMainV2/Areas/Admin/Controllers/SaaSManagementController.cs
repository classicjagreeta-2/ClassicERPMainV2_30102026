using Antlr.Runtime.Misc;
using ClassicErpMainV2.Areas.Admin.Repositories;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ClassicErpMainV2.Areas.Admin.Controllers
{
    [Authorize]
    public class SaaSManagementController : Controller
    {
        private readonly CompanyLicenceRepository _licenceRepo;
        private readonly UserLicenceRepository _sessionRepo;

        public SaaSManagementController()
        {
            string connStr = ConfigurationManager.ConnectionStrings["OracleDbContext"].ConnectionString;
            _licenceRepo = new CompanyLicenceRepository(connStr);
            _sessionRepo = new UserLicenceRepository(connStr);
        }

        // GET: /SaaSAdmin/
        // Renders the main SPA Dashboard UI view
        public ActionResult Index()
        {
            return View();
        }

        // =========================================================================
        // SECTION 1: COMPANY LICENSES API ENDPOINTS
        // =========================================================================

        // GET: /SaaSAdmin/GetLicences
        [HttpGet]
        public ActionResult GetLicences()
        {
            try
            {
                var list = _licenceRepo.GetAll();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: /SaaSAdmin/GetLicenceById/5
        [HttpGet]
        public ActionResult GetLicenceById(int id)
        {
            var item = _licenceRepo.GetById(id);
            if (item == null) return HttpNotFound();
            return Json(item, JsonRequestBehavior.AllowGet);
        }

        // POST: /SaaSAdmin/SaveLicence
        // Handles both CREATE (ID = 0) and UPDATE (ID > 0)
        [HttpPost]
        public ActionResult SaveLicence(SAAS_COMPANY_LICENCE model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return new HttpStatusCodeResult(400, "Invalid payload model");

                if (model.CREATED_DATE == default(DateTime))
                    model.CREATED_DATE = DateTime.Now;

                if (model.ID > 0)
                {
                    bool updated = _licenceRepo.Update(model);
                    return Json(new { success = updated, message = updated ? "Licence updated successfully" : "Record not found" });
                }
                else
                {
                    _licenceRepo.Insert(model);
                    return Json(new { success = true, message = "Licence created successfully" });
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /SaaSAdmin/DeleteLicence/5
        [HttpPost]
        public ActionResult DeleteLicence(int id)
        {
            try
            {
                bool deleted = _licenceRepo.Delete(id);
                return Json(new { success = deleted, message = deleted ? "Licence deleted" : "Record not found" });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /SaaSAdmin/ToggleLicenceActive/5
        [HttpPost]
        public ActionResult ToggleLicenceActive(int id)
        {
            try
            {
                var licence = _licenceRepo.GetById(id);
                if (licence == null) return HttpNotFound();

                licence.IS_ACTIVE = (licence.IS_ACTIVE == "Y") ? "N" : "Y";
                bool updated = _licenceRepo.Update(licence);
                return Json(new { success = updated, isActive = licence.IS_ACTIVE });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =========================================================================
        // SECTION 2: USER LOGIN SESSIONS API ENDPOINTS
        // =========================================================================

        // GET: /SaaSAdmin/GetSessions
        [HttpGet]
        public ActionResult GetSessions()
        {
            try
            {
                var list = _sessionRepo.GetAll();
                return Json(list, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: /SaaSAdmin/GetSessionById/5
        [HttpGet]
        public ActionResult GetSessionById(int id)
        {
            var item = _sessionRepo.GetById(id);
            if (item == null) return HttpNotFound();
            return Json(item, JsonRequestBehavior.AllowGet);
        }

        // POST: /SaaSAdmin/SaveSession
        // Handles both CREATE (ID = 0) and UPDATE (ID > 0)
        [HttpPost]
        public ActionResult SaveSession(SAAS_USER_LOGIN_SESSION model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return new HttpStatusCodeResult(400, "Invalid payload model");

                if (model.LOGIN_TIME == default(DateTime))
                    model.LOGIN_TIME = DateTime.Now;

                if (model.LAST_ACTIVITY == default(DateTime))
                    model.LAST_ACTIVITY = DateTime.Now;

                if (model.ID > 0)
                {
                    bool updated = _sessionRepo.Update(model);
                    return Json(new { success = updated, message = updated ? "Session updated" : "Record not found" });
                }
                else
                {
                    _sessionRepo.Insert(model);
                    return Json(new { success = true, message = "Session logged" });
                }
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /SaaSAdmin/ForceLogoutSession/5
        [HttpPost]
        public ActionResult ForceLogoutSession(int id)
        {
            try
            {
                var session = _sessionRepo.GetById(id);
                if (session == null) return HttpNotFound();

                session.IS_ONLINE = "N";
                session.LOGOUT_TIME = DateTime.Now;

                bool updated = _sessionRepo.Update(session);
                return Json(new { success = updated, message = "User forced offline successfully" });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /SaaSAdmin/DeleteSession/5
        [HttpPost]
        public ActionResult DeleteSession(int id)
        {
            try
            {
                bool deleted = _sessionRepo.Delete(id);
                return Json(new { success = deleted, message = deleted ? "Session log deleted" : "Record not found" });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = ex.Message });
            }
        }
    }

}