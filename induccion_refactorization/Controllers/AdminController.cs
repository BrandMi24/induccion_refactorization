using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using induccion_refactorization.Filters;
using induccion_refactorization.Models;
using induccion_refactorization.ViewModels;

namespace induccion_refactorization.Controllers
{
    [RoleAuthorize(1)] // Only Administrador (RolID = 1)
    public class AdminController : Controller
    {
        private CaptacionDbContext db = new CaptacionDbContext();

        // GET: /Admin/Index
        public ActionResult Index()
        {
            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Email = Session["Email"];
            return View();
        }

        // GET: /Admin/GestionUsuarios
        public ActionResult GestionUsuarios(string search, int? rolId, bool? activo, int page = 1)
        {
            var query = db.Usuarios.Include(u => u.Role).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    u.Nombre.ToLower().Contains(term) ||
                    u.ApellidoPaterno.ToLower().Contains(term) ||
                    u.NombreUsuario.ToLower().Contains(term) ||
                    u.CorreoElectronico.ToLower().Contains(term));
            }

            if (rolId.HasValue)
            {
                query = query.Where(u => u.RolID == rolId.Value);
            }

            if (activo.HasValue)
            {
                query = query.Where(u => u.Activo == activo.Value);
            }

            query = query.OrderBy(u => u.UsuarioID);

            var result = PagedResult<Usuario>.Create(query, page, 10);

            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Search = search;
            ViewBag.RolId = rolId;
            ViewBag.Activo = activo;
            ViewBag.RolesFiltro = new SelectList(db.Roles, "RolID", "Nombre", rolId);

            return View(result);
        }

        // GET: /Admin/Permisos
        public ActionResult Permisos()
        {
            ViewBag.NombreCompleto = Session["NombreCompleto"];
            return View();
        }

        // GET: /Admin/CreateUsuario
        public ActionResult CreateUsuario()
        {
            ViewBag.RolID = new SelectList(db.Roles, "RolID", "Nombre");
            return View();
        }

        // POST: /Admin/CreateUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUsuario(Usuario usuario)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (db.Usuarios.Any(u => u.CorreoElectronico == usuario.CorreoElectronico))
                    {
                        ModelState.AddModelError("CorreoElectronico", "Ya existe un usuario con este correo electrónico.");
                    }
                    else
                    {
                        usuario.Activo = true;
                        usuario.FechaRegistro = DateTime.Now;
                        db.Usuarios.Add(usuario);
                        db.SaveChanges();

                        TempData["Success"] = $"Usuario '{usuario.NombreCompleto}' creado exitosamente.";
                        return RedirectToAction("GestionUsuarios");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                }
            }

            ViewBag.RolID = new SelectList(db.Roles, "RolID", "Nombre", usuario.RolID);
            return View(usuario);
        }

        // GET: /Admin/EditUsuario/5
        public ActionResult EditUsuario(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var usuario = db.Usuarios.Find(id);
            if (usuario == null)
            {
                return HttpNotFound();
            }

            ViewBag.RolID = new SelectList(db.Roles, "RolID", "Nombre", usuario.RolID);
            return View(usuario);
        }

        // POST: /Admin/EditUsuario/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditUsuario(Usuario usuario)
        {
            // Contrasena is intentionally left blank by the form unless the admin wants to change it;
            // the [Required] attribute would otherwise reject the empty posted value.
            ModelState.Remove("Contrasena");

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = db.Usuarios.Find(usuario.UsuarioID);
                    if (existing == null)
                    {
                        return HttpNotFound();
                    }

                    existing.Nombre = usuario.Nombre;
                    existing.ApellidoPaterno = usuario.ApellidoPaterno;
                    existing.ApellidoMaterno = usuario.ApellidoMaterno;
                    existing.NombreUsuario = usuario.NombreUsuario;
                    existing.CorreoElectronico = usuario.CorreoElectronico;
                    existing.RolID = usuario.RolID;

                    // Password is only changed if a new one was entered
                    if (!string.IsNullOrWhiteSpace(usuario.Contrasena))
                    {
                        existing.Contrasena = usuario.Contrasena;
                    }

                    db.SaveChanges();

                    TempData["Success"] = $"Usuario '{existing.NombreCompleto}' actualizado exitosamente.";
                    return RedirectToAction("GestionUsuarios");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
                }
            }

            ViewBag.RolID = new SelectList(db.Roles, "RolID", "Nombre", usuario.RolID);
            return View(usuario);
        }

        // POST: /Admin/ToggleActivoUsuario/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleActivoUsuario(int id)
        {
            try
            {
                var usuario = db.Usuarios.Find(id);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("GestionUsuarios");
                }

                usuario.Activo = !usuario.Activo;
                db.SaveChanges();

                TempData["Success"] = usuario.Activo
                    ? $"Usuario '{usuario.NombreCompleto}' reactivado exitosamente."
                    : $"Usuario '{usuario.NombreCompleto}' desactivado exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
            }

            return RedirectToAction("GestionUsuarios");
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
