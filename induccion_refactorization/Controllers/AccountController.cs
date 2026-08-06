using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using induccion_refactorization.Helpers;
using induccion_refactorization.Models;
using induccion_refactorization.ViewModels;

namespace induccion_refactorization.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private CaptacionDbContext db = new CaptacionDbContext();

        // GET: /Account/Login
        public ActionResult Login(string returnUrl)
        {
            // If user is already authenticated, redirect to appropriate dashboard
            if (Request.IsAuthenticated && Session["RolID"] != null)
            {
                return RedirectToDashboard((int)Session["RolID"]);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Query CaptacionDB Usuarios table with role information
                var user = db.Usuarios
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.CorreoElectronico == model.Email && u.Activo);

                if (user == null)
                {
                    ModelState.AddModelError("", "Email no registrado o usuario inactivo.");
                    return View(model);
                }

                // Validate password. Existing rows may still hold a legacy plain-text value;
                // those are verified directly and transparently upgraded to a PBKDF2 hash on
                // successful login so no separate data migration is required.
                bool passwordValid;
                if (PasswordHasher.IsHashed(user.Contrasena))
                {
                    passwordValid = PasswordHasher.Verify(model.Password, user.Contrasena);
                }
                else
                {
                    passwordValid = user.Contrasena == model.Password;
                    if (passwordValid)
                    {
                        user.Contrasena = PasswordHasher.Hash(model.Password);
                    }
                }

                if (!passwordValid)
                {
                    ModelState.AddModelError("", "Contraseña incorrecta.");
                    return View(model);
                }

                // El rol Director ya no tiene acceso al sistema; se rechaza aquí, antes de
                // crear sesión o cookie de autenticación.
                if (user.RolID == 2)
                {
                    ModelState.AddModelError("", "Tu acceso ha sido deshabilitado. Intenta con otro usuario o contacta al administrador.");
                    return View(model);
                }

                // "Recordarme" hace que la cookie de autenticación sobreviva a cerrar el
                // navegador (30 días); si no se marca, es una cookie de sesión normal que
                // se descarta al cerrar el navegador y expira a las 8 horas de todos modos.
                var expiration = model.RememberMe ? DateTime.Now.AddDays(30) : DateTime.Now.AddHours(8);

                // Create Forms Authentication ticket
                var authTicket = new FormsAuthenticationTicket(
                    version: 1,
                    name: user.CorreoElectronico,
                    issueDate: DateTime.Now,
                    expiration: expiration,
                    isPersistent: model.RememberMe,
                    userData: $"{user.UsuarioID}|{user.RolID}|{user.NombreCompleto}"
                );

                string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                {
                    HttpOnly = true,
                    Secure = FormsAuthentication.RequireSSL
                };
                if (model.RememberMe)
                {
                    // Solo si se marcó "Recordarme" se le da una fecha de expiración a la
                    // cookie en sí; de lo contrario debe quedar como cookie de sesión
                    // (sin Expires) para que el navegador la borre al cerrarse.
                    authCookie.Expires = expiration;
                }
                Response.Cookies.Add(authCookie);

                // Store core session data
                SessionHelper.PopulateSession(Session, user);

                // Record last access time
                user.UltimoAcceso = DateTime.Now;
                db.SaveChanges();

                // Load role-specific data and redirect
                return RedirectByRole(user, returnUrl);
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                // Capture detailed Entity Framework validation errors
                var errorMessages = dbEx.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);
                
                var fullErrorMessage = string.Join("; ", errorMessages);
                ModelState.AddModelError("", $"Error de validación: {fullErrorMessage}");
                return View(model);
            }
            catch (System.Data.SqlClient.SqlException sqlEx)
            {
                // Capture SQL Server connection/query errors
                ModelState.AddModelError("", $"Error de base de datos: {sqlEx.Message} (Número: {sqlEx.Number})");
                return View(model);
            }
            catch (Exception ex)
            {
                // Capture all other exceptions with full details
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                ModelState.AddModelError("", $"Error técnico: {ex.Message} {innerMessage}");
                return View(model);
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            // Clear Forms Authentication
            FormsAuthentication.SignOut();

            // Clear all session data
            Session.Clear();
            Session.Abandon();

            // Remove authentication cookie
            if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
            {
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName)
                {
                    Expires = DateTime.Now.AddDays(-1)
                };
                Response.Cookies.Add(authCookie);
            }

            // Redirect to home page
            return RedirectToAction("Index", "Home");
        }

        #region Helper Methods

        private ActionResult RedirectByRole(Usuario user, string returnUrl)
        {
            switch (user.RolID)
            {
                case 1: // Administrador
                    return RedirectToAction("Index", "Admin");

                case 3: // Coordinador
                case 5: // Maestro
                    return RedirectToAction("Index", "Coordinador");

                case 4: // Aspirante
                    // Los datos de sesión propios del aspirante (AspiranteID, Folio) ya
                    // los deja listos SessionHelper.PopulateSession de arriba.

                    // Check for dummy/placeholder email (legacy bug fix)
                    if (user.CorreoElectronico.Contains("@example.com") || user.CorreoElectronico.Contains("@dummy.com"))
                    {
                        TempData["Warning"] = "Por favor, actualice su correo electrónico.";
                        return RedirectToAction("EditarEmail", "Aspirante");
                    }
                    return RedirectToAction("Index", "Aspirante");

                default:
                    // Rol sin panel todavía (p. ej. no reconocido, o un rol nuevo sin
                    // controlador propio aún): se cierra la sesión recién creada en vez de
                    // dejarla apuntando a ningún lado.
                    FormsAuthentication.SignOut();
                    Session.Clear();
                    Session.Abandon();
                    TempData["Error"] = "Tu rol no tiene un panel disponible todavía. Contacta al administrador.";
                    return RedirectToAction("Login");
            }
        }

        private ActionResult RedirectToDashboard(int rolID)
        {
            switch (rolID)
            {
                case 1:
                    return RedirectToAction("Index", "Admin");
                case 3:
                case 5:
                    return RedirectToAction("Index", "Coordinador");
                case 4:
                    return RedirectToAction("Index", "Aspirante");
                default:
                    // Rol sin panel válido (p. ej. una sesión vieja de un usuario Director,
                    // RolID 2): se cierra la sesión aquí para no caer en un ciclo de
                    // redirecciones entre Login y Home.
                    FormsAuthentication.SignOut();
                    Session.Clear();
                    Session.Abandon();
                    TempData["Error"] = "Tu acceso ha sido deshabilitado. Intenta con otro usuario o contacta al administrador.";
                    return RedirectToAction("Login");
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
