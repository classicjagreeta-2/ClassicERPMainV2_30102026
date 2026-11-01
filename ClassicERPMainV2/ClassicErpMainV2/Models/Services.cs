using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using System.Web.UI.WebControls;
using System.Threading.Tasks;
using Microsoft.Ajax.Utilities;
using System.Net.Http.Json;
using System.Xml.Linq;
using System.Net;
using System.Web.Helpers;
using NLog;
using System.Threading;

namespace ClassicErpMainV2.Models
{

    public static class Services
    {

        private static HttpClient _client { get; set; }
        private static string _userName = null;
        private static string _token = null;
        private static string _dbName = null;
        private static string _baseDomain = null;
        private static string _dbType = null;
        static Services()
        {
            _baseDomain = ConfigurationManager.AppSettings["SubDomain"];
            _dbType = ConfigurationManager.AppSettings["DatabaseType"];
            _client = new HttpClient();          
            _client.BaseAddress = new Uri(ConfigurationManager.AppSettings["BaseApiUrl"]);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static async Task<ResponseApiModel<TData>> PostAsync<TData>(string url, object model, Type type = null)
        {
            ResponseApiModel<TData> tdata = new ResponseApiModel<TData>();
            //JsonContent content1 = JsonContent.Create(model);
            HttpContent content;
            if (model != null)
            {
                content = JsonContent.Create(model);
            }
            else
            {
                content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");
            }
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("x-module", "Finance");

            if (!string.IsNullOrEmpty(Utility.SessionHelper.GetUser().getToken))
            {
                _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Utility.SessionHelper.GetUser().getToken);
            }
            if (!string.IsNullOrEmpty(Utility.SessionHelper.GetUser().getDbName))
            {
                _client.DefaultRequestHeaders.Add("x-database", Utility.SessionHelper.GetUser().getDbName);
            }
            if (!string.IsNullOrEmpty(_dbType))
            {
                _client.DefaultRequestHeaders.Add("x-dbtype", _dbType);
            }
            if (!String.IsNullOrEmpty(_baseDomain))
            {
                url = _baseDomain + url;
            }


            var rest = await _client.PostAsync(url, content);
            tdata =await ReadAsString<TData>(rest);
            return tdata;
        }

        public static async Task<ResponseApiModel<TData>> GetAsync<TData>(string url)
        {
            ResponseApiModel<TData> tdata = new ResponseApiModel<TData>();
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("x-module", "Finance");
            var username =Utility.SessionHelper.GetUser().getUserId;
            if (!string.IsNullOrEmpty(Utility.SessionHelper.GetUser().getToken))
            {
                _client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Utility.SessionHelper.GetUser().getToken);
            }
            if (!string.IsNullOrEmpty(Utility.SessionHelper.GetUser().getDbName))
            {
                _client.DefaultRequestHeaders.Add("x-database", Utility.SessionHelper.GetUser().getDbName);
            }
            if (!string.IsNullOrEmpty(_dbType))
            {
                _client.DefaultRequestHeaders.Add("x-dbtype", _dbType);
            }
            if (!String.IsNullOrEmpty(_baseDomain))
            {
                url = _baseDomain + url;
            }
            var rest = await _client.GetAsync(url);
            tdata =await ReadAsString<TData>(rest);
            return tdata;
        }

        private static async Task<ResponseApiModel<TData>> ReadAsString<TData>(HttpResponseMessage rest)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            ResponseApiModel<TData> tdata = new ResponseApiModel<TData>();
            try
            {
                if (rest.IsSuccessStatusCode)
                {
                    string responseBody =await rest.Content.ReadAsStringAsync();
                    //logger.Warn("rest LogError: - " + responseBody + ". Data : " + rest);
                    tdata.Data = JsonConvert.DeserializeObject<TData>(responseBody);
                    tdata.StatusCode = rest.StatusCode.ToString();
                    tdata.IsSuccessStatusCode = rest.IsSuccessStatusCode;
                }
                else
                {
                    string responseBody =await rest.Content.ReadAsStringAsync();
                    logger.Error("rest LogError: - " + responseBody + ". Data : " + rest);
                    //var res1 = JsonConvert.DeserializeObject<UnauthorizedModel>(responseBody);
                    if (!string.IsNullOrEmpty(responseBody))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(responseBody.ToString(), @"ORA-\d+:.*");
                        tdata.Message = match.Success ? match.Value.Replace("(", "").Replace(")", "") : responseBody;
                    }
                    //var res = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    //tdata.IsSuccessStatusCode = rest.IsSuccessStatusCode;
                    //var res = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    tdata.IsSuccessStatusCode = rest.IsSuccessStatusCode;
                    
                        //tdata.Message = res.message;
                    tdata.StatusCode = rest.StatusCode.ToString();
                }
            }
            catch (Exception ex)
            {
                logger.Error("rest exception LogError: - " + ex.Message + ". Data : " + rest);
                throw;
            }

            return tdata;
        }
    }

    public class UnauthorizedModel
    {
        public string message { get; set; }
    }
}