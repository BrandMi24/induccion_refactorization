using System;
using System.Web;
using System.Web.Mvc;
using induccion_refactorization.Helpers;
using induccion_refactorization.Models;

namespace induccion_refactorization.Filters
{
    /// <summary>
    /// Capa adicional de permisos finos (Leer/Crear/Editar/Eliminar) por sección,
    /// evaluada JUNTO a (no en lugar de) [RoleAuthorize]. Al ser un tipo de
    /// atributo distinto, ASP.NET MVC evalúa ambos de forma independiente.
    /// Usage: [RequierePermiso("GestionContenido", Accion.Crear)]
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class RequierePermisoAttribute : AuthorizeAttribute
    {
        private readonly string _permisoClave;
        private readonly Accion _accion;

        public RequierePermisoAttribute(string permisoClave, Accion accion)
        {
            _permisoClave = permisoClave;
            _accion = accion;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            if (!httpContext.Request.IsAuthenticated)
            {
                return false;
            }

            if (httpContext.Session == null || httpContext.Session["RolID"] == null || httpContext.Session["UsuarioID"] == null)
            {
                return false;
            }

            int rolId;
            int usuarioId;
            try
            {
                rolId = (int)httpContext.Session["RolID"];
                usuarioId = (int)httpContext.Session["UsuarioID"];
            }
            catch
            {
                return false;
            }

            using (var db = new CaptacionDbContext())
            {
                return PermissionHelper.TieneAcceso(db, usuarioId, rolId, _permisoClave, _accion);
            }
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (filterContext.HttpContext.Request.IsAuthenticated)
            {
                filterContext.Result = new ViewResult
                {
                    ViewName = "Unauthorized",
                    ViewData = new ViewDataDictionary
                    {
                        ["Message"] = "No tiene permisos para realizar esta acción."
                    }
                };
            }
            else
            {
                base.HandleUnauthorizedRequest(filterContext);
            }
        }
    }
}
