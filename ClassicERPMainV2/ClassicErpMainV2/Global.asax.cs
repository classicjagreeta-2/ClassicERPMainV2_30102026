using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;


namespace HRMS
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
        protected void Application_BeginRequest()
        {
            string user = HttpContext.Current.Request.QueryString["MId"];
            var url = HttpContext.Current.Request.Url;
            string subFolder = ConfigurationManager.AppSettings["AppSubfolder"] ?? "";
            if (!string.IsNullOrEmpty(subFolder))
            {
                if (url.Host.Contains("localhost") && url.AbsolutePath.StartsWith("/" + subFolder))
                {
                    string newUrl = url.AbsoluteUri.Replace("/" + subFolder, "");
                    HttpContext.Current.Response.Redirect(newUrl, true);
                }
            }
        }

        protected void Application_Error()
        {
            Exception ex = Server.GetLastError();

            if (ex == null)
                return;

            // Get the actual underlying exception
            Exception actualException = ex;

            while (actualException.InnerException != null)
            {
                actualException = actualException.InnerException;
            }

            var oracleEx = actualException as OracleException;

            string errorType = oracleEx != null
                ? "ORACLE ERROR"
                : "APPLICATION ERROR";

            string errorCode = oracleEx != null
                ? oracleEx.Number.ToString()
                : "N/A";

            string log = string.Format(
                                        @"Date       : {0}
                                            Error Type : {1}
                                            Error Code : {2}
                                            URL        : {3}
                                            Message    : {4}
                                            StackTrace : {5}
                                            --------------------------------------------------
                                        ",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                errorType,
                errorCode,
                Request.Url,
                actualException.Message,
                ex.StackTrace
            );

            try
            {
                string logFolder = Server.MapPath("~/App_Data/Logs");

                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);

                string logFile = Path.Combine(
                    logFolder,
                    "ApplicationError_" + DateTime.Now.ToString("yyyyMMdd") + ".txt"
                );

                File.AppendAllText(logFile, log, Encoding.UTF8);
            }
            catch
            {
                // Do not allow logging failures to cause another exception.
            }

            // Clear the unhandled error
            Server.ClearError();

            // Return a generic error instead of exposing database details
            Response.Clear();
            Response.StatusCode = 500;
            Response.TrySkipIisCustomErrors = true;

            if (new HttpRequestWrapper(Request).IsAjaxRequest())
            {
                Response.ContentType = "application/json";
                Response.Write(
                    "{\"success\":false,\"message\":\"An unexpected error occurred.\"}"
                );
            }
            else
            {
                Response.Redirect("~/Error/Index", false);
            }

            Context.ApplicationInstance.CompleteRequest();
        }
    }
}
