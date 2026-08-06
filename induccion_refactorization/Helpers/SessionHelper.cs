using System.Web;
using induccion_refactorization.Models;

namespace induccion_refactorization.Helpers
{
    /// <summary>
    /// Centraliza qué se guarda en Session al iniciar sesión, para que la misma
    /// lógica se pueda reutilizar cuando se restaura la sesión a partir de la
    /// cookie de "Recordarme" (ver Global.asax.cs) sin duplicarla.
    /// </summary>
    public static class SessionHelper
    {
        public static void PopulateSession(HttpSessionStateBase session, Usuario user)
        {
            session["UsuarioID"] = user.UsuarioID;
            session["RolID"] = user.RolID;
            session["NombreCompleto"] = user.NombreCompleto;
            session["Email"] = user.CorreoElectronico;

            // Ya no existe una tabla Aspirantes aparte: quién es aspirante se
            // determina por RolID, y "AspiranteID" es directamente su UsuarioID.
            // El Folio es el mismo NombreUsuario (ver Fase 10).
            if (user.RolID == 4)
            {
                session["AspiranteID"] = user.UsuarioID;
                session["Folio"] = user.NombreUsuario;
            }
        }
    }
}
