using System.Web.Mvc;

namespace ClassicErpMainV2.Areas.UserAccessMvc
{
    public class UserAccessMvcAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "UserAccessMvc";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "UserAccessMvc_default",
                "UserAccessMvc/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}