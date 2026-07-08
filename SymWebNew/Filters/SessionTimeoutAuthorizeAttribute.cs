using System;
using System.Web;
using System.Web.Mvc;

namespace SymWebUI.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public class SessionTimeoutAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // Skip session check for login page and other public pages
            var path = httpContext.Request.Path.ToLower();
            if (path.Contains("/home/login") || path.Contains("/home/index") || 
                path.Contains("/home/forgotpassword") || path.Contains("/home/contactus") ||
                path.Contains("/home/aboutus") || path.Contains("/home/client") ||
                path.Contains("/account/login") || path.Contains("/account/register"))
            {
                return true;
            }

            var authorized = base.AuthorizeCore(httpContext);
            if (!authorized)
            {
                return false;
            }

            // Check if session is still valid
            var sessionUser = httpContext.Session["User"];
            var sessionFullName = httpContext.Session["FullName"];

            // If session data is null or empty, session has expired
            if (sessionUser == null || string.IsNullOrEmpty(sessionUser.ToString()) ||
                sessionFullName == null || string.IsNullOrEmpty(sessionFullName.ToString()))
            {
                return false;
            }

            return true;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            // If the user is authenticated but session expired, redirect to login
            if (filterContext.HttpContext.Request.IsAuthenticated)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Home" },
                        { "action", "Index" },
                        { "area", "" }
                    });
            }
            else
            {
                // If not authenticated, use default behavior
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}
