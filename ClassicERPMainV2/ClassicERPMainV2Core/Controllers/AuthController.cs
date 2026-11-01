using Microsoft.AspNetCore.Mvc;

namespace ClassicERPMainV2Core.Controllers
{
    public class AuthController : Controller
    {
        public IActionResult Login()
        {
            return View();
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
}
