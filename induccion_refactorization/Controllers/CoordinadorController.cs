using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using induccion_refactorization.Filters;
using induccion_refactorization.Models;

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
        public ActionResult RevisarProgreso()
        {
            var progresos = db.Ind_ProgresoAspirante
                .Include(p => p.Aspirante.Usuario)
                .Include(p => p.Ind_Unidad.Ind_Materia)
                .Where(p => p.Estado == "Entregado")
                .OrderBy(p => p.FechaEnvio)
                .ToList();

            ViewBag.NombreCompleto = Session["NombreCompleto"];
            return View(progresos);
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
        public ActionResult RevisarEntregas()
        {
            var submisiones = db.Ind_Submisiones
                .Include(s => s.Aspirante.Usuario)
                .Include(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia)
                .Where(s => s.Estado == "Pendiente")
                .OrderBy(s => s.FechaEnvio)
                .ToList();

            ViewBag.NombreCompleto = Session["NombreCompleto"];
            return View(submisiones);
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
            var submision = db.Ind_Submisiones.Find(id);
            if (submision == null)
            {
                TempData["Error"] = "Archivo no encontrado.";
                return RedirectToAction("RevisarEntregas");
            }

            var fullPath = Server.MapPath(submision.RutaArchivo);
            if (!System.IO.File.Exists(fullPath))
            {
                TempData["Error"] = "El archivo ya no está disponible en el servidor.";
                return RedirectToAction("RevisarEntregas");
            }

            return File(fullPath, "application/octet-stream", Path.GetFileName(fullPath));
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
