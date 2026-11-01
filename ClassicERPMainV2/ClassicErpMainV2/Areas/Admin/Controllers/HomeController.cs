using ClassicErpMainV2.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace ClassicErpMainV2.Areas.Admin.Controllers
{
    [CustomAuthorize]
    public class HomeController : Controller
    {
        // GET: Admin/Home
        public ActionResult Index()
        {
            return View();
        }

        // GET: Admin/Home/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Admin/Home/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/Home/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Admin/Home/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Admin/Home/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Admin/Home/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Admin/Home/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // POST: /PushNotification/Receive
        [HttpPost]
        public ActionResult Receive()
        {
            try
            {
                // Read the raw JSON string from the HTTP request body
                string jsonPayload;
                using (var reader = new StreamReader(Request.InputStream))
                {
                    jsonPayload = reader.ReadToEnd();
                }

                if (string.IsNullOrEmpty(jsonPayload))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Empty Payload");
                }


                // Return HTTP 200 OK success status to Tally
                return Json(new { status = "Success", message = "Notification received" });
            }
            catch (System.Exception ex)
            {
                // Log exception
                System.Diagnostics.Trace.TraceError("Error processing Tally Push Notification: " + ex.Message);
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
