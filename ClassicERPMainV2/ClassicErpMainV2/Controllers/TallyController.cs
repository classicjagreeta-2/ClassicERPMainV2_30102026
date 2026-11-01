using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Net.Http;

namespace ClassicErpMainV2.Controllers
{
    public class TallyController : Controller
    {
        private const string Secret = "my-secret-123";

        // last 100 notifications, kept in memory for the test page
        private static readonly ConcurrentQueue<string> Recent = new ConcurrentQueue<string>();

        public ActionResult GetUserIp()
        {
            string ipAddress = string.Empty;

            // This calls a public service to get your machine's actual external public IP while testing locally
            using (var client = new System.Net.Http.HttpClient())
            {
                ipAddress = client.GetStringAsync("https://api.ipify.org").Result.Trim();
            }

            return Json(new { UserIp = ipAddress }, JsonRequestBehavior.AllowGet);
        }

        // ---------- 1. Quick browser check (GET) ----------
        // http://192.168.1.50/Tally/Ping
        public ActionResult Ping()
        {
            return Content("<RESPONSE>PONG " + DateTime.Now.ToString("HH:mm:ss") + "</RESPONSE>", "text/xml");
        }

        // ---------- 2. Receiver for Tally (POST) ----------
        // http://192.168.1.50/Tally/Voucher?key=my-secret-123

        [HttpPost]
        public ContentResult Voucher(string key, string action, string type, string no, string date)
        {
            if (key != "my-secret-123")
                return Content("{\"status\":\"denied\"}", "application/json");

            if (string.IsNullOrEmpty(type))
                return Content("{\"status\":\"ok\"}", "application/json");

            string body;
            Request.InputStream.Position = 0;
            using (var reader = new StreamReader(Request.InputStream, Request.ContentEncoding))
            {
                body = reader.ReadToEnd();
            }

            System.Xml.Linq.XDocument doc = null;
            for (int i = 0; i < 5; i++)
            {
                System.Threading.Thread.Sleep(1000);
                doc = FetchVoucherByDetails(type, no, date);
                if (doc != null && doc.Descendants("VOUCHER").Any()) break;
            }

            // keep until the real JSON shape is confirmed
            System.IO.File.WriteAllText(Server.MapPath("~/App_Data/tally_last.json"), body);

            // TODO: fetch full voucher from Tally (port 9000) using type + no + date

            return Content("{\"status\":\"ok\"}", "application/json");
        }
        public static string BuildVoucherRegisterRequest(string tFrom, string tTo, string tVoucherNo, string tVoucherType)
        {
            return $@"<ENVELOPE>
                        <HEADER>
                            <VERSION>1</VERSION>
                            <TALLYREQUEST>Export</TALLYREQUEST>
                            <TYPE>Data</TYPE>
                            <ID>Voucher Register</ID>
                        </HEADER>
                        <BODY>
                            <DESC>
                                <STATICVARIABLES>
                                    <SVFROMDATE>{tFrom}</SVFROMDATE>
                                    <SVTODATE>{tTo}</SVTODATE>
                                    <VOUCHERTYPENAME>{tVoucherType}</VOUCHERTYPENAME>
                                    <SVVOUCHERNUMBER>{tVoucherNo}</SVVOUCHERNUMBER>
                                    <EXPLODEALLLEVELS>YES</EXPLODEALLLEVELS>
                                    <SVEXPORTFORMAT>$$SysName:XML</SVEXPORTFORMAT>
                                </STATICVARIABLES>
                            </DESC>
                        </BODY>
                    </ENVELOPE>";
        }
        private static System.Xml.Linq.XDocument FetchVoucherByDetails(string voucherType, string voucherNumber, string voucherDate)
        {
            System.Xml.Linq.XDocument xDoc = null;

            // Construct formula matching Number, Type, and Date
            string formula = "$VoucherNumber = \"" + voucherNumber.Replace("\"", "") +
                             "\" AND $VoucherTypeName = \"" + voucherType.Replace("\"", "") +
                             "\" AND $Date = \"" + voucherDate + "\"";

            string req =
            "<ENVELOPE><HEADER><VERSION>1</VERSION><TALLYREQUEST>Export</TALLYREQUEST><TYPE>Collection</TYPE><ID>VNTFilterVoucher</ID></HEADER>" +
            "<BODY><DESC><STATICVARIABLES><SVEXPORTFORMAT>$$SysName:XML</SVEXPORTFORMAT></STATICVARIABLES>" +
            "<TDL><TDLMESSAGE>" +
            "<COLLECTION NAME=\"VNTFilterVoucher\" ISMODIFY=\"No\"><TYPE>Voucher</TYPE>" +
            "<FETCH>Date, VoucherNumber, VoucherTypeName, PartyLedgerName, Narration, Reference, GUID, MasterID, AlterID, AllLedgerEntries.*, AllInventoryEntries.*</FETCH>" +
            "<FILTERS>VNTFilterFormula</FILTERS></COLLECTION>" +
            "<SYSTEM TYPE=\"Formulae\" NAME=\"VNTFilterFormula\">" + System.Security.SecurityElement.Escape(formula) + "</SYSTEM>" +
            "</TDLMESSAGE></TDL></DESC></BODY></ENVELOPE>";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var asas = BuildVoucherRegisterRequest(voucherDate, voucherDate, voucherNumber, voucherType);
                    var content = new StringContent(BuildVoucherRegisterRequest(voucherDate, voucherDate, voucherNumber, voucherType), Encoding.UTF8, "text/xml");

                    // Post request to Tally Prime XML server port
                    HttpResponseMessage response = client.PostAsync("http://localhost:9050", content).GetAwaiter().GetResult();
                    var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    // Sanitize XML control characters returned by Tally
                    string cleanXml = System.Text.RegularExpressions.Regex.Replace(result, @"[^\u0009\u000A\u000D\u0020-\uD7FF\uE000-\uFFFD]", string.Empty);

                    var settings = new System.Xml.XmlReaderSettings()
                    {
                        CheckCharacters = false // Ignores invalid control characters
                    };

                    using (var reader = System.Xml.XmlReader.Create(new System.IO.StringReader(cleanXml), settings))
                    {
                        xDoc = System.Xml.Linq.XDocument.Load(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("FetchVoucher Error: " + ex.Message);
            }

            return xDoc;
        }

        private static System.Xml.Linq.XDocument FetchVoucher(string type, string no)
        {
            System.Xml.Linq.XDocument xDoc = null;

            string formula = "$VoucherNumber = \"" + no.Replace("\"", "") + "\" AND $VoucherTypeName = \"" + type.Replace("\"", "") + "\"";
            string req =
            "<ENVELOPE><HEADER><VERSION>1</VERSION><TALLYREQUEST>Export</TALLYREQUEST><TYPE>Collection</TYPE><ID>VNTOneVoucher</ID></HEADER>" +
            "<BODY><DESC><STATICVARIABLES><SVEXPORTFORMAT>$$SysName:XML</SVEXPORTFORMAT></STATICVARIABLES>" +
            "<TDL><TDLMESSAGE>" +
            "<COLLECTION NAME=\"VNTOneVoucher\" ISMODIFY=\"No\"><TYPE>Voucher</TYPE>" +
            "<FETCH>Date, VoucherNumber, VoucherTypeName, PartyLedgerName, Narration, Reference, GUID, MasterID, AlterID, AllLedgerEntries.*, AllInventoryEntries.*</FETCH>" +
            "<FILTERS>VNTOneF</FILTERS></COLLECTION>" +
            "<SYSTEM TYPE=\"Formulae\" NAME=\"VNTOneF\">" + System.Security.SecurityElement.Escape(formula) + "</SYSTEM>" +
            "</TDLMESSAGE></TDL></DESC></BODY></ENVELOPE>";

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var content = new StringContent(req, Encoding.UTF8, "text/xml");

                    // 1. Fixed: Pass full URI string instead of raw integer port 9050
                    HttpResponseMessage response = client.PostAsync("http://localhost:9050", content).GetAwaiter().GetResult();
                    var result = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    // 2. Sanitize XML control characters
                    string cleanXml = System.Text.RegularExpressions.Regex.Replace(result, @"[^\u0009\u000A\u000D\u0020-\uD7FF\uE000-\uFFFD]", string.Empty);

                    var settings = new System.Xml.XmlReaderSettings()
                    {
                        CheckCharacters = false // Ignores invalid control characters
                    };

                    // 3. Fixed: Pass 'cleanXml' instead of raw 'result' into the StringReader
                    using (var reader = System.Xml.XmlReader.Create(new System.IO.StringReader(cleanXml), settings))
                    {
                        xDoc = System.Xml.Linq.XDocument.Load(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("FetchVoucher Error: " + ex.Message);
            }

            // 4. Fixed: Return the loaded XDocument directly to match the method signature
            return xDoc;
        }

        [HttpPost]
        public ActionResult Voucher1(string key)
        {
            if (!string.Equals(key, Secret, StringComparison.Ordinal))
            {
                Response.StatusCode = 401;
                return XmlReply("Unauthorized");
            }

            string body;
            Request.InputStream.Position = 0;
            using (var reader = new StreamReader(Request.InputStream, Request.ContentEncoding))
            {
                body = reader.ReadToEnd();
            }
            //var doc = System.Xml.Linq.XDocument.Parse(body);
            //var n = doc.Descendants("VNTNOTIFY").FirstOrDefault();
            try
            {
                // strip illegal XML control characters Tally can emit
                body = Regex.Replace(body, "&#([0-9]|1[0-9]|2[0-9]|3[01]);", "");

                XDocument x = XDocument.Parse(body);

                string action = Val(x, "ACTION");
                string guid = Val(x, "GUID");
                string no = Val(x, "VOUCHERNUMBER");
                string type = Val(x, "VOUCHERTYPENAME");
                string date = Val(x, "DATE");
                string party = Val(x, "PARTYLEDGERNAME");
                string amount = Val(x, "AMOUNT");

                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + action + " | " + type +
                              " | " + no + " | " + date + " | " + party + " | " + amount + " | " + guid;

                AddRecent(line);
                WriteFile(line);

                // TODO: save to database / fetch the full voucher from Tally by GUID
            }
            catch (Exception ex)
            {
                // keep the raw body so you can see what Tally really sent
                string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | PARSE ERROR: " + ex.Message +
                              " | RAW: " + body;
                AddRecent(line);
                WriteFile(line);
            }
            var ddt = XmlReply("OK");
            // always 200 + XML so Tally doesn't report a failed action
            return ddt;
        }

        // ---------- 3. View what has arrived ----------
        // http://192.168.1.50/Tally/Log
        public ActionResult Log()
        {
            string html = "<html><body style='font-family:Consolas;font-size:13px'>" +
                          "<h3>Tally notifications (newest last)</h3><pre>" +
                          Server.HtmlEncode(string.Join("\n", Recent.ToArray())) +
                          "</pre></body></html>";
            return Content(html, "text/html");
        }

        // ---------- 4. Self-test: posts a fake voucher to the receiver ----------
        // http://localhost/Tally/SelfTest
        public ActionResult SelfTest()
        {
            string url = Request.Url.GetLeftPart(UriPartial.Authority) + "/Tally/Voucher?key=" + Secret;
            string xml = "<ENVELOPE><ACTION>CREATED</ACTION><GUID>test-guid-123</GUID>" +
                         "<VOUCHERNUMBER>S-001</VOUCHERNUMBER><VOUCHERTYPENAME>Sales</VOUCHERTYPENAME>" +
                         "<DATE>20261022</DATE><PARTYLEDGERNAME>Test Party</PARTYLEDGERNAME>" +
                         "<AMOUNT>1000</AMOUNT></ENVELOPE>";

            using (var wc = new System.Net.WebClient())
            {
                wc.Headers[System.Net.HttpRequestHeader.ContentType] = "text/xml";
                string reply = wc.UploadString(url, "POST", xml);
                return Content("Reply from receiver: " + reply + "\n\nNow open /Tally/Log", "text/plain");
            }
        }

        // ---------- helpers ----------
        private static string Val(XDocument x, string tag)
        {
            XElement e = x.Descendants(tag).FirstOrDefault();
            return e == null ? null : e.Value.Trim();
        }

        private static ContentResult XmlReply(string text)
        {
            return new ContentResult
            {
                Content = "<RESPONSE>" + System.Security.SecurityElement.Escape(text) + "</RESPONSE>",
                ContentType = "text/xml",
                ContentEncoding = Encoding.UTF8
            };
        }

        private static void AddRecent(string line)
        {
            Recent.Enqueue(line);
            string dump;
            while (Recent.Count > 100) Recent.TryDequeue(out dump);
        }

        private static readonly object FileLock = new object();

        private void WriteFile(string line)
        {
            try
            {
                string path = Server.MapPath("~/App_Data/tally-log.txt");
                lock (FileLock)
                {
                    System.IO.File.AppendAllText(path, line + Environment.NewLine);
                }
            }
            catch { /* logging must never break the reply */ }
        }
    }

}