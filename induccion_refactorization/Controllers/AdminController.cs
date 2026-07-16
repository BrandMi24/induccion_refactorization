using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using induccion_refactorization.Filters;
using induccion_refactorization.Helpers;
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
        public ActionResult GestionUsuarios(string search, int? rolId, bool? activo, string sortBy, string sortDir, int page = 1, int pageSize = 10)
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

            bool descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            switch (sortBy)
            {
                case "Nombre":
                    query = descending ? query.OrderByDescending(u => u.Nombre) : query.OrderBy(u => u.Nombre);
                    break;
                case "Correo":
                    query = descending ? query.OrderByDescending(u => u.CorreoElectronico) : query.OrderBy(u => u.CorreoElectronico);
                    break;
                case "Rol":
                    query = descending ? query.OrderByDescending(u => u.Role.Nombre) : query.OrderBy(u => u.Role.Nombre);
                    break;
                case "Estado":
                    query = descending ? query.OrderByDescending(u => u.Activo) : query.OrderBy(u => u.Activo);
                    break;
                default:
                    query = query.OrderBy(u => u.UsuarioID);
                    break;
            }

            var result = PagedResult<Usuario>.Create(query, page, pageSize);

            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Search = search;
            ViewBag.RolId = rolId;
            ViewBag.Activo = activo;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDir = sortDir;
            ViewBag.RolesFiltro = new SelectList(db.Roles, "RolID", "Nombre", rolId);

            return View(result);
        }

        // GET: /Admin/Reportes
        public ActionResult Reportes()
        {
            ViewBag.NombreCompleto = Session["NombreCompleto"];

            var model = new ReportesViewModel
            {
                TotalUsuarios = db.Usuarios.Count(),
                TotalAdministradores = db.Usuarios.Count(u => u.RolID == 1 && u.Activo),
                TotalDirectores = db.Usuarios.Count(u => u.RolID == 2 && u.Activo),
                TotalCoordinadores = db.Usuarios.Count(u => u.RolID == 3 && u.Activo),
                TotalAspirantes = db.Usuarios.Count(u => u.RolID == 4 && u.Activo),
                TotalMaterias = db.Ind_Materias.Count(m => m.Activo),
                TotalUnidades = db.Ind_Unidades.Count(),
                TotalMateriales = db.Ind_Materiales.Count(),
                TotalEntregables = db.Ind_Entregables.Count(e => e.Activo)
            };

            var submisiones = db.Ind_Submisiones.ToList();
            model.EntregasPendientes = submisiones.Count(s => s.Estado == "Pendiente");
            model.EntregasRevisadas = submisiones.Count(s => s.Estado == "Revisado");
            model.EntregasRechazadas = submisiones.Count(s => s.Estado == "Rechazado");
            var calificadasEntregas = submisiones.Where(s => s.Calificacion.HasValue).ToList();
            model.PromedioEntregables = calificadasEntregas.Any() ? calificadasEntregas.Average(s => s.Calificacion.Value) : (decimal?)null;

            var progresos = db.Ind_ProgresoAspirante.ToList();
            model.UnidadesAsignadas = progresos.Count(p => p.Estado == "Asignado");
            model.UnidadesEntregadas = progresos.Count(p => p.Estado == "Entregado");
            model.UnidadesCalificadas = progresos.Count(p => p.Estado == "Calificado");
            var calificadasProgreso = progresos.Where(p => p.Calificacion.HasValue).ToList();
            model.PromedioUnidades = calificadasProgreso.Any() ? calificadasProgreso.Average(p => p.Calificacion.Value) : (decimal?)null;

            model.MateriasConProgreso = db.Ind_Materias
                .Include(m => m.Ind_Unidades.Select(u => u.Ind_ProgresoAspirantes))
                .Where(m => m.Activo)
                .ToList()
                .Select(m =>
                {
                    var prog = m.Ind_Unidades.SelectMany(u => u.Ind_ProgresoAspirantes).ToList();
                    return new MateriaProgresoResumen
                    {
                        Nombre = m.Nombre,
                        Total = prog.Count,
                        Completados = prog.Count(p => p.Estado == "Calificado")
                    };
                })
                .ToList();

            return View(model);
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
                        usuario.Contrasena = PasswordHasher.Hash(usuario.Contrasena);
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
                        existing.Contrasena = PasswordHasher.Hash(usuario.Contrasena);
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

        // GET: /Admin/GestionPeriodos
        public ActionResult GestionPeriodos()
        {
            ViewBag.NombreCompleto = Session["NombreCompleto"];

            var periodos = db.Periodos
                .OrderByDescending(p => p.FechaInicio)
                .ToList();

            return View(periodos);
        }

        // GET: /Admin/CreatePeriodo
        public ActionResult CreatePeriodo()
        {
            return View(new Periodo { Activo = true });
        }

        // POST: /Admin/CreatePeriodo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreatePeriodo(Periodo periodo)
        {
            ModelState.Remove("Ind_Materias");

            if (periodo.FechaFin < periodo.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha de fin no puede ser anterior a la fecha de inicio.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    db.Periodos.Add(periodo);
                    db.SaveChanges();

                    TempData["Success"] = "Periodo creado exitosamente.";
                    return RedirectToAction("GestionPeriodos");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                }
            }

            return View(periodo);
        }

        // GET: /Admin/EditPeriodo/5
        public ActionResult EditPeriodo(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var periodo = db.Periodos.Find(id);
            if (periodo == null)
            {
                return HttpNotFound();
            }

            return View(periodo);
        }

        // POST: /Admin/EditPeriodo/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditPeriodo(Periodo periodo)
        {
            ModelState.Remove("Ind_Materias");

            if (periodo.FechaFin < periodo.FechaInicio)
            {
                ModelState.AddModelError("FechaFin", "La fecha de fin no puede ser anterior a la fecha de inicio.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = db.Periodos.Find(periodo.PeriodoID);
                    if (existing == null)
                    {
                        return HttpNotFound();
                    }

                    existing.FechaInicio = periodo.FechaInicio;
                    existing.FechaFin = periodo.FechaFin;
                    existing.Activo = periodo.Activo;

                    db.SaveChanges();

                    TempData["Success"] = "Periodo actualizado exitosamente.";
                    return RedirectToAction("GestionPeriodos");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
                }
            }

            return View(periodo);
        }

        // POST: /Admin/TogglePeriodo/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TogglePeriodo(int id)
        {
            try
            {
                var periodo = db.Periodos.Find(id);
                if (periodo == null)
                {
                    TempData["Error"] = "Periodo no encontrado.";
                    return RedirectToAction("GestionPeriodos");
                }

                periodo.Activo = !periodo.Activo;
                db.SaveChanges();

                TempData["Success"] = periodo.Activo
                    ? "Periodo reactivado exitosamente."
                    : "Periodo desactivado exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
            }

            return RedirectToAction("GestionPeriodos");
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
