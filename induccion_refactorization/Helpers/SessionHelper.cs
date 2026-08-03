using System.Linq;
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
        public static void PopulateSession(HttpSessionStateBase session, Usuario user, CaptacionDbContext db)
        {
            session["UsuarioID"] = user.UsuarioID;
            session["RolID"] = user.RolID;
            session["NombreCompleto"] = user.NombreCompleto;
            session["Email"] = user.CorreoElectronico;

            if (user.RolID == 4)
            {
                var aspirante = db.Aspirantes.FirstOrDefault(a => a.UsuarioID == user.UsuarioID);
                if (aspirante != null)
                {
                    session["AspiranteID"] = aspirante.AspiranteID;
                    session["Matricula"] = aspirante.Matricula;
                    session["Folio"] = aspirante.Folio;
                }
            }
        }
    }
}
