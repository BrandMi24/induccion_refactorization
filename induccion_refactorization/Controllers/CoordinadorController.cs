using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using induccion_refactorization.Filters;
using induccion_refactorization.Models;
using induccion_refactorization.ViewModels;

namespace induccion_refactorization.Controllers
{
    [RoleAuthorize(3)] // Only Coordinador (RolID = 3)
    public class CoordinadorController : Controller
    {
        private CaptacionDbContext db = new CaptacionDbContext();

        // GET: /Coordinador/Index
        public ActionResult Index()
        {
            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Email = Session["Email"];

            var submisiones = db.Ind_Submisiones.ToList();
            ViewBag.PendientesCount = submisiones.Count(s => s.Estado == "Pendiente");
            ViewBag.RevisadasCount = submisiones.Count(s => s.Estado == "Revisado");
            ViewBag.RechazadasCount = submisiones.Count(s => s.Estado == "Rechazado");
            ViewBag.TotalMaterias = db.Ind_Materias.Count(m => m.Activo);
            ViewBag.ProgresoPendienteCount = db.Ind_ProgresoAspirante.Count(p => p.Estado == "Entregado");

            return View();
        }

        // GET: /Coordinador/RevisarProgreso
        public ActionResult RevisarProgreso(string search, int? materiaId, string sortBy, string sortDir, int page = 1, int pageSize = 10)
        {
            var query = db.Ind_ProgresoAspirante
                .Include(p => p.Aspirante.Usuario)
                .Include(p => p.Ind_Unidad.Ind_Materia)
                .Where(p => p.Estado == "Entregado");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Aspirante.Usuario.Nombre.ToLower().Contains(term) ||
                    p.Aspirante.Usuario.ApellidoPaterno.ToLower().Contains(term) ||
                    p.Ind_Unidad.Nombre.ToLower().Contains(term));
            }

            if (materiaId.HasValue)
            {
                query = query.Where(p => p.Ind_Unidad.MateriaID == materiaId.Value);
            }

            bool descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            switch (sortBy)
            {
                case "Aspirante":
                    query = descending ? query.OrderByDescending(p => p.Aspirante.Usuario.Nombre) : query.OrderBy(p => p.Aspirante.Usuario.Nombre);
                    break;
                case "Materia":
                    query = descending ? query.OrderByDescending(p => p.Ind_Unidad.Ind_Materia.Nombre) : query.OrderBy(p => p.Ind_Unidad.Ind_Materia.Nombre);
                    break;
                case "Unidad":
                    query = descending ? query.OrderByDescending(p => p.Ind_Unidad.Nombre) : query.OrderBy(p => p.Ind_Unidad.Nombre);
                    break;
                case "Fecha":
                    query = descending ? query.OrderByDescending(p => p.FechaEnvio) : query.OrderBy(p => p.FechaEnvio);
                    break;
                default:
                    query = query.OrderBy(p => p.FechaEnvio);
                    break;
            }

            var result = PagedResult<Ind_ProgresoAspirante>.Create(query, page, pageSize);

            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Search = search;
            ViewBag.MateriaId = materiaId;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDir = sortDir;
            ViewBag.MateriasFiltro = new SelectList(db.Ind_Materias.Where(m => m.Activo), "MateriaID", "Nombre", materiaId);

            return View(result);
        }

        // GET: /Coordinador/CalificarProgreso/5
        public ActionResult CalificarProgreso(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var progreso = db.Ind_ProgresoAspirante
                .Include(p => p.Aspirante.Usuario)
                .Include(p => p.Ind_Unidad.Ind_Materia)
                .FirstOrDefault(p => p.ProgresoID == id);

            if (progreso == null)
            {
                return HttpNotFound();
            }

            return View(progreso);
        }

        // POST: /Coordinador/CalificarProgreso/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CalificarProgreso(int progresoId, decimal calificacion, string comentariosEvaluador)
        {
            try
            {
                var progreso = db.Ind_ProgresoAspirante.Find(progresoId);
                if (progreso == null)
                {
                    TempData["Error"] = "Registro de progreso no encontrado.";
                    return RedirectToAction("RevisarProgreso");
                }

                progreso.Estado = "Calificado";
                progreso.Calificacion = calificacion;
                progreso.ComentariosEvaluador = comentariosEvaluador;
                progreso.UsuarioCalificadorID = Session["UsuarioID"] as int?;

                db.SaveChanges();

                TempData["Success"] = "Unidad calificada exitosamente.";
                return RedirectToAction("RevisarProgreso");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al calificar: {ex.Message}";
                return RedirectToAction("CalificarProgreso", new { id = progresoId });
            }
        }

        // GET: /Coordinador/RevisarEntregas
        public ActionResult RevisarEntregas(string search, int? materiaId, string sortBy, string sortDir, int page = 1, int pageSize = 10)
        {
            var query = db.Ind_Submisiones
                .Include(s => s.Aspirante.Usuario)
                .Include(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia)
                .Where(s => s.Estado == "Pendiente");

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(s =>
                    s.Aspirante.Usuario.Nombre.ToLower().Contains(term) ||
                    s.Aspirante.Usuario.ApellidoPaterno.ToLower().Contains(term) ||
                    s.Ind_Entregable.Titulo.ToLower().Contains(term));
            }

            if (materiaId.HasValue)
            {
                query = query.Where(s => s.Ind_Entregable.Ind_Unidad.MateriaID == materiaId.Value);
            }

            bool descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            switch (sortBy)
            {
                case "Aspirante":
                    query = descending ? query.OrderByDescending(s => s.Aspirante.Usuario.Nombre) : query.OrderBy(s => s.Aspirante.Usuario.Nombre);
                    break;
                case "Materia":
                    query = descending ? query.OrderByDescending(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia.Nombre) : query.OrderBy(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia.Nombre);
                    break;
                case "Entregable":
                    query = descending ? query.OrderByDescending(s => s.Ind_Entregable.Titulo) : query.OrderBy(s => s.Ind_Entregable.Titulo);
                    break;
                case "Fecha":
                    query = descending ? query.OrderByDescending(s => s.FechaEnvio) : query.OrderBy(s => s.FechaEnvio);
                    break;
                default:
                    query = query.OrderBy(s => s.FechaEnvio);
                    break;
            }

            var result = PagedResult<Ind_Submision>.Create(query, page, pageSize);

            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Search = search;
            ViewBag.MateriaId = materiaId;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDir = sortDir;
            ViewBag.MateriasFiltro = new SelectList(db.Ind_Materias.Where(m => m.Activo), "MateriaID", "Nombre", materiaId);

            return View(result);
        }

        // GET: /Coordinador/Calificar/5
        public ActionResult Calificar(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var submision = db.Ind_Submisiones
                .Include(s => s.Aspirante.Usuario)
                .Include(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia)
                .FirstOrDefault(s => s.SubmisionID == id);

            if (submision == null)
            {
                return HttpNotFound();
            }

            return View(submision);
        }

        // POST: /Coordinador/Calificar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Calificar(int submisionId, string estado, decimal? calificacion, string comentarioRevisor)
        {
            try
            {
                var submision = db.Ind_Submisiones.Find(submisionId);
                if (submision == null)
                {
                    TempData["Error"] = "Entrega no encontrada.";
                    return RedirectToAction("RevisarEntregas");
                }

                if (estado != "Revisado" && estado != "Rechazado")
                {
                    TempData["Error"] = "Estado de calificación no válido.";
                    return RedirectToAction("Calificar", new { id = submisionId });
                }

                if (estado == "Revisado" && !calificacion.HasValue)
                {
                    TempData["Error"] = "Debe asignar una calificación para aprobar la entrega.";
                    return RedirectToAction("Calificar", new { id = submisionId });
                }

                submision.Estado = estado;
                submision.Calificacion = estado == "Revisado" ? calificacion : null;
                submision.ComentarioRevisor = comentarioRevisor;
                submision.UsuarioRevisorID = Session["UsuarioID"] as int?;
                submision.FechaRevision = DateTime.Now;

                db.SaveChanges();

                TempData["Success"] = "Entrega calificada exitosamente.";
                return RedirectToAction("RevisarEntregas");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al calificar: {ex.Message}";
                return RedirectToAction("Calificar", new { id = submisionId });
            }
        }

        // GET: /Coordinador/DownloadSubmission/5
        public ActionResult DownloadSubmission(int id)
        {
            var submision = db.Ind_Submisiones
                .Include(s => s.Documento)
                .FirstOrDefault(s => s.SubmisionID == id);
            if (submision == null)
            {
                TempData["Error"] = "Archivo no encontrado.";
                return RedirectToAction("RevisarEntregas");
            }

            var rutaArchivo = submision.Documento?.RutaAlmacenamiento ?? submision.RutaArchivo;
            var nombreDescarga = submision.Documento?.NombreOriginal ?? Path.GetFileName(rutaArchivo);
            var tipoMime = submision.Documento?.TipoMIME ?? "application/octet-stream";

            var fullPath = Server.MapPath(rutaArchivo);
            if (!System.IO.File.Exists(fullPath))
            {
                TempData["Error"] = "El archivo ya no está disponible en el servidor.";
                return RedirectToAction("RevisarEntregas");
            }

            return File(fullPath, tipoMime, nombreDescarga);
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
