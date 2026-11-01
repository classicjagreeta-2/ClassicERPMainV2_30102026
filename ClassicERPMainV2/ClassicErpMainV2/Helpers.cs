using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.IO;
using System.Security.Cryptography;

namespace ClassicErpMainV2
{
    public class Helpers
    {
        public static class UserInfo
        {
            private static string _userid;
            private static string _username;
            private static string _dbname;
            private static string _dbtype;
            private static string _token;
            private static string _moduleCode;
            private static string _loca;
            private static DateTime? _lastLoginDt;
            public static string Loca
            {
                get { return _loca; }
                set { _loca = value; }
            }
            public static string ModuleCode
            {
                get { return _moduleCode; }
                set { _moduleCode = value; }
            }
            public static string getUserId
            {
                get { return _userid; }
                set { _userid = value; }
            }
            public static string getUserName
            {
                get { return _username; }
                set { _username = value; }
            }
            public static DateTime? lastLoginDt
            {
                get { return _lastLoginDt; }
                set { _lastLoginDt = value; }
            }
            public static string getToken
            {
                get { return _token; }
                set { _token = value; }
            }
            public static string getDbName
            {
                get { return _dbname; }
                set { _dbname = value; }
            }
            public static string getDbType
            {
                get { return _dbtype; }
                set { _dbtype = value; }
            }
        }

        public static class Cryptography {
            public static string Encrypt(string key, string plainText)
            {
                byte[] iv = new byte[16];
                byte[] array;

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key);
                    aes.IV = iv;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                            {
                                streamWriter.Write(plainText);
                            }

                            array = memoryStream.ToArray();
                        }
                    }
                }

                return Convert.ToBase64String(array);
            }
            public static string Decrypt(string key, string cipherText)
            {
                byte[] iv = new byte[16];
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key);
                    aes.IV = iv;
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }
    }
}
