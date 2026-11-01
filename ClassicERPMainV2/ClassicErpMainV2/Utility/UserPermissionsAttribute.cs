using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace ClassicErpMainV2.Utility
{
    public class UserPermissionsAttribute : AuthorizeAttribute
    {
        public IEnumerable<UserPermissions> PermissionsRequired { get; set; }

        public UserPermissionsAttribute()
        {
        }

        public UserPermissionsAttribute(params UserPermissions[] permissionsRequired)
        {
            PermissionsRequired = permissionsRequired;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var user = filterContext.HttpContext.User; // get user from DB

            //if (PermissionsRequired.All(x => user.Permissions.Any(y => x == y)))
            //{
            //    // all permissions are met
            //    base.OnAuthorization(filterContext);
            //}
            //else
            //{
            //    throw new UnauthorizedAccessException();
            //}

            base.OnAuthorization(filterContext);
        }
    }

    public class CustomAuthorizeAttribute : AuthorizeAttribute
    {
        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // Check if the user is not logged in
            if (!filterContext.HttpContext.User.Identity.IsAuthenticated)
            {
                // Detect if the requested URL is inside the Admin area
                var area = filterContext.RouteData.DataTokens["area"]?.ToString();

                string loginUrl;
                if (area != null && area.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    // Redirect to Admin Area login page
                    loginUrl = "/Admin/Account/Login";
                }
                else
                {
                    // Redirect to main/default login page
                    loginUrl = "/Auth/Login";
                }

                filterContext.Result = new RedirectResult(loginUrl);
            }
            else
            {
                // If the user IS logged in but lacks required roles/permissions, 
                // fallback to default behavior (usually showing a 403 Access Denied or redirect)
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}