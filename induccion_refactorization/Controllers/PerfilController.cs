using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using induccion_refactorization.Models;

namespace induccion_refactorization.Controllers
{
    [Authorize]
    public class PerfilController : Controller
    {
        private CaptacionDbContext db = new CaptacionDbContext();

        // GET: /Perfil/Index
        public ActionResult Index()
        {
            int? usuarioId = Session["UsuarioID"] as int?;
            if (usuarioId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var usuario = db.Usuarios.Include(u => u.Role).FirstOrDefault(u => u.UsuarioID == usuarioId);
            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(usuario);
        }

        // GET: /Perfil/Edit
        public ActionResult Edit()
        {
            int? usuarioId = Session["UsuarioID"] as int?;
            if (usuarioId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var usuario = db.Usuarios.Find(usuarioId);
            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(usuario);
        }

        // POST: /Perfil/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string nombre, string apellidoPaterno, string apellidoMaterno, string telefono, string nuevaContrasena)
        {
            int? usuarioId = Session["UsuarioID"] as int?;
            if (usuarioId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var usuario = db.Usuarios.Find(usuarioId);
            if (usuario == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(apellidoPaterno))
            {
                ModelState.AddModelError("", "El nombre y el apellido paterno son obligatorios.");
                return View(usuario);
            }

            try
            {
                usuario.Nombre = nombre;
                usuario.ApellidoPaterno = apellidoPaterno;
                usuario.ApellidoMaterno = apellidoMaterno;
                usuario.Telefono = telefono;

                if (!string.IsNullOrWhiteSpace(nuevaContrasena))
                {
                    usuario.Contrasena = nuevaContrasena;
                }

                db.SaveChanges();

                Session["NombreCompleto"] = usuario.NombreCompleto;

                TempData["Success"] = "Tu perfil se actualizó exitosamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
                return View(usuario);
            }
        }

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
