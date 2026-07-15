using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace induccion_refactorization.Filters
{
    /// <summary>
    /// Custom authorization filter for role-based access control.
    /// Usage: [RoleAuthorize(1, 3)] to allow only Administrador and Coordinador roles
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly int[] _allowedRoles;

        /// <summary>
        /// Initialize the attribute with allowed role IDs
        /// </summary>
        /// <param name="allowedRoles">One or more role IDs (1=Admin, 2=Director, 3=Coordinador, 4=Aspirante)</param>
        public RoleAuthorizeAttribute(params int[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            // First, check if user is authenticated
            if (!httpContext.Request.IsAuthenticated)
            {
                return false;
            }

            // Check if session exists and has RolID
            if (httpContext.Session == null || httpContext.Session["RolID"] == null)
            {
                return false;
            }

            // Get user's role ID from session
            int userRoleID;
            try
            {
                userRoleID = (int)httpContext.Session["RolID"];
            }
            catch
            {
                return false;
            }

            // Check if user's role is in the allowed roles list
            return _allowedRoles.Contains(userRoleID);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsAuthenticated)
            {
                // User is authenticated but doesn't have the right role
                filterContext.Result = new ViewResult
                {
                    ViewName = "Unauthorized",
                    ViewData = new ViewDataDictionary
                    {
                        ["Message"] = "No tiene permisos para acceder a esta sección del sistema."
                    }
                };

                // Alternatively, redirect to an error page
                // filterContext.Result = new RedirectToRouteResult(
                //     new RouteValueDictionary(new { controller = "Error", action = "Unauthorized" })
                // );
            }
            else
            {
                // User is not authenticated - redirect to login
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}
