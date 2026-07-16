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

                // Create Forms Authentication ticket
                var authTicket = new FormsAuthenticationTicket(
                    version: 1,
                    name: user.CorreoElectronico,
                    issueDate: DateTime.Now,
                    expiration: DateTime.Now.AddHours(8),
                    isPersistent: model.RememberMe,
                    userData: $"{user.UsuarioID}|{user.RolID}|{user.NombreCompleto}"
                );

                string encryptedTicket = FormsAuthentication.Encrypt(authTicket);
                var authCookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket)
                {
                    HttpOnly = true,
                    Secure = FormsAuthentication.RequireSSL,
                    Expires = authTicket.Expiration
                };
                Response.Cookies.Add(authCookie);

                // Store core session data
                Session["UsuarioID"] = user.UsuarioID;
                Session["RolID"] = user.RolID;
                Session["NombreCompleto"] = user.NombreCompleto;
                Session["Email"] = user.CorreoElectronico;

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

                case 2: // Director
                    return RedirectToAction("Index", "Director");

                case 3: // Coordinador
                    return RedirectToAction("Index", "Coordinador");

                case 4: // Aspirante
                    // Load aspirante-specific session data
                    var aspirante = db.Aspirantes
                        .FirstOrDefault(a => a.UsuarioID == user.UsuarioID);

                    if (aspirante != null)
                    {
                        Session["AspiranteID"] = aspirante.AspiranteID;
                        Session["Matricula"] = aspirante.Matricula;
                        Session["Folio"] = aspirante.Folio;

                        // Check for dummy/placeholder email (legacy bug fix)
                        if (user.CorreoElectronico.Contains("@example.com") || user.CorreoElectronico.Contains("@dummy.com"))
                        {
                            TempData["Warning"] = "Por favor, actualice su correo electrónico.";
                            return RedirectToAction("EditarEmail", "Aspirante");
                        }
                    }
                    return RedirectToAction("Index", "Aspirante");

                default:
                    // Unknown role - redirect to home
                    TempData["Error"] = "Rol no reconocido. Contacte al administrador.";
                    return RedirectToAction("Index", "Home");
            }
        }

        private ActionResult RedirectToDashboard(int rolID)
        {
            switch (rolID)
            {
                case 1:
                    return RedirectToAction("Index", "Admin");
                case 2:
                    return RedirectToAction("Index", "Director");
                case 3:
                    return RedirectToAction("Index", "Coordinador");
                case 4:
                    return RedirectToAction("Index", "Aspirante");
                default:
                    return RedirectToAction("Index", "Home");
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
