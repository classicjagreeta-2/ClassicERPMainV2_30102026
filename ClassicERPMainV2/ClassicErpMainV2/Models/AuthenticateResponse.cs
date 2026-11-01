using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ClassicErpMainV2.Models
{
    public class AuthenticateResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime  LastLoginDt { get; set; }
        public double ExpiresMin { get; set; }
        public TimeSpan ExpiresTime { get; set; }


        public AuthenticateResponse(User user, string token, string refreshToken)
        {
            Id = user.Id;
            FirstName = user.FirstName;
            LastName = user.LastName;
            Username = user.Username;
            AccessToken = token;
            RefreshToken = refreshToken;
            LastLoginDt = user.LastLoginDt.Value;
        }
    }
}