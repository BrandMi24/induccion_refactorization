using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using induccion_refactorization.Helpers;
using induccion_refactorization.Models;

namespace induccion_refactorization
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // Cuando "Recordarme" mantiene viva la cookie de Forms Authentication más
        // allá de la sesión de ASP.NET (p. ej. el usuario cerró el navegador y lo
        // volvió a abrir, o el App Pool se reciclió), el usuario llega autenticado
        // pero con Session vacía. Sin este paso, RoleAuthorizeAttribute lo trataría
        // como "sin permisos" (Session["RolID"] == null) en vez de dejarlo pasar.
        // Aquí se reconstruye la Session a partir de los datos guardados en el
        // ticket cifrado, usando la misma lógica que un login normal
        // (SessionHelper.PopulateSession), así que no se duplica en dos lugares.
        protected void Application_PostAcquireRequestState(object sender, EventArgs e)
        {
            var context = HttpContext.Current;
            if (context?.Session == null)
            {
                return;
            }

            if (context.Session["RolID"] != null)
            {
                return;
            }

            if (!(context.User?.Identity is FormsIdentity identity) || !identity.IsAuthenticated)
            {
                return;
            }

            var parts = identity.Ticket.UserData?.Split('|');
            if (parts == null || parts.Length == 0 || !int.TryParse(parts[0], out int usuarioId))
            {
                return;
            }

            using (var db = new CaptacionDbContext())
            {
                var user = db.Usuarios.FirstOrDefault(u => u.UsuarioID == usuarioId && u.Activo);
                if (user == null)
                {
                    // El usuario fue desactivado o eliminado desde que se emitió la
                    // cookie: se cierra la sesión en vez de dejarlo con una identidad
                    // fantasma.
                    FormsAuthentication.SignOut();
                    return;
                }

                SessionHelper.PopulateSession(new HttpSessionStateWrapper(context.Session), user);
            }
        }
    }
}
