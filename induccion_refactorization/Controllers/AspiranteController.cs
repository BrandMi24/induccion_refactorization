using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using induccion_refactorization.Filters;
using induccion_refactorization.Helpers;
using induccion_refactorization.Models;
using induccion_refactorization.ViewModels;

namespace induccion_refactorization.Controllers
{
    [RoleAuthorize(4)] // Only Aspirante (RolID = 4)
    public class AspiranteController : Controller
    {
        private CaptacionDbContext db = new CaptacionDbContext();

        // GET: /Aspirante/Index
        public ActionResult Index()
        {
            var model = new AspiranteDashboardViewModel
            {
                NombreCompleto = Session["NombreCompleto"] as string,
                Email = Session["Email"] as string,
                Matricula = Session["Matricula"] as string,
                Folio = Session["Folio"] as string
            };

            try
            {
                int? aspiranteId = Session["AspiranteID"] as int?;

                if (aspiranteId == null)
                {
                    ViewBag.Error = "No se pudo cargar su perfil de aspirante. Contacte al administrador.";
                    return View(model);
                }

                // Get all assigned subjects with progress information
                var aspirante = db.Aspirantes
                    .Include(a => a.Usuario)
                    .Include(a => a.Ind_ProgresoAspirantes.Select(p => p.Ind_Unidad.Ind_Materia))
                    .FirstOrDefault(a => a.AspiranteID == aspiranteId);

                if (aspirante == null)
                {
                    ViewBag.Error = "Perfil de aspirante no encontrado.";
                    return View(model);
                }

                // Group progress by materia and calculate completion percentage
                model.MateriasProgreso = aspirante.Ind_ProgresoAspirantes
                    .Where(p => p.Ind_Unidad != null && p.Ind_Unidad.Ind_Materia != null)
                    .GroupBy(p => p.Ind_Unidad.Ind_Materia)
                    .Select(g => new MateriaProgresoViewModel
                    {
                        Materia = g.Key,
                        TotalUnidades = g.Count(),
                        UnidadesCompletadas = g.Count(p => p.Estado == "Calificado"),
                        PromedioCalificacion = g.Where(p => p.Calificacion.HasValue).Average(p => p.Calificacion),
                        ProgresoAspirantes = g.ToList()
                    })
                    .ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al cargar datos: {ex.Message} {ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message}";
                return View(model);
            }
        }

        // GET: /Aspirante/MateriaDetails/5
        public ActionResult MateriaDetails(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "ID de materia no válido.";
                return RedirectToAction("Index");
            }

            try
            {
                int? aspiranteId = Session["AspiranteID"] as int?;
                if (aspiranteId == null)
                {
                    TempData["Error"] = "Sesión inválida.";
                    return RedirectToAction("Index");
                }

                var materia = db.Ind_Materias
                    .Include(m => m.Ind_Unidades.Select(u => u.Ind_Materiales))
                    .Include(m => m.Ind_Unidades.Select(u => u.Ind_ProgresoAspirantes))
                    .Include(m => m.Ind_Unidades.Select(u => u.Ind_Entregables))
                    .FirstOrDefault(m => m.MateriaID == id);

                if (materia == null)
                {
                    TempData["Error"] = "Materia no encontrada.";
                    return RedirectToAction("Index");
                }

                // Get progress records for this aspirante and materia
                var progresoRecords = db.Ind_ProgresoAspirante
                    .Where(p => p.AspiranteID == aspiranteId &&
                                p.Ind_Unidad.MateriaID == id)
                    .ToList();

                // Get this aspirante's submissions for entregables in this materia
                var entregableIds = materia.Ind_Unidades
                    .SelectMany(u => u.Ind_Entregables.Where(e => e.Activo))
                    .Select(e => e.EntregableID)
                    .ToList();

                var submisiones = db.Ind_Submisiones
                    .Where(s => s.AspiranteID == aspiranteId && entregableIds.Contains(s.EntregableID))
                    .ToList();

                ViewBag.ProgresoRecords = progresoRecords;
                ViewBag.Submisiones = submisiones;
                ViewBag.MateriaNombre = materia.Nombre;
                ViewBag.MateriaDescripcion = materia.Descripcion;
                ViewBag.AspiranteID = aspiranteId;

                return View(materia);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al cargar detalles: {ex.Message} {ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: /Aspirante/MarcarEntregado/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MarcarEntregado(int progresoId, int materiaId)
        {
            int? aspiranteId = Session["AspiranteID"] as int?;
            if (aspiranteId == null)
            {
                TempData["Error"] = "Sesión inválida.";
                return RedirectToAction("Index");
            }

            try
            {
                var progreso = db.Ind_ProgresoAspirante
                    .FirstOrDefault(p => p.ProgresoID == progresoId && p.AspiranteID == aspiranteId);

                if (progreso == null)
                {
                    TempData["Error"] = "Registro de progreso no encontrado.";
                    return RedirectToAction("MateriaDetails", new { id = materiaId });
                }

                if (progreso.Estado != "Asignado")
                {
                    TempData["Error"] = "Esta unidad ya fue entregada o calificada.";
                    return RedirectToAction("MateriaDetails", new { id = materiaId });
                }

                progreso.Estado = "Entregado";
                progreso.FechaEnvio = DateTime.Now;
                db.SaveChanges();

                TempData["Success"] = "Unidad marcada como entregada. Tu coordinador la revisará pronto.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
            }

            return RedirectToAction("MateriaDetails", new { id = materiaId });
        }

        // GET: /Aspirante/UploadEntregable/5
        public ActionResult UploadEntregable(int? id)
        {
            if (id == null)
            {
                TempData["Error"] = "Entregable no válido.";
                return RedirectToAction("Index");
            }

            var entregable = db.Ind_Entregables
                .Include(e => e.Ind_Unidad.Ind_Materia)
                .FirstOrDefault(e => e.EntregableID == id && e.Activo);

            if (entregable == null)
            {
                TempData["Error"] = "Entregable no encontrado.";
                return RedirectToAction("Index");
            }

            int? aspiranteId = Session["AspiranteID"] as int?;
            if (aspiranteId == null)
            {
                TempData["Error"] = "Sesión inválida.";
                return RedirectToAction("Index");
            }

            var submisionExistente = db.Ind_Submisiones
                .FirstOrDefault(s => s.AspiranteID == aspiranteId && s.EntregableID == id);

            ViewBag.Entregable = entregable;
            ViewBag.SubmisionExistente = submisionExistente;

            return View();
        }

        // POST: /Aspirante/UploadEntregable/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadEntregable(int id, HttpPostedFileBase archivo)
        {
            int? aspiranteId = Session["AspiranteID"] as int?;
            if (aspiranteId == null)
            {
                TempData["Error"] = "Sesión inválida.";
                return RedirectToAction("Index");
            }

            var entregable = db.Ind_Entregables.FirstOrDefault(e => e.EntregableID == id && e.Activo);
            if (entregable == null)
            {
                TempData["Error"] = "Entregable no encontrado.";
                return RedirectToAction("Index");
            }

            if (!FileUploadValidator.IsValid(archivo, out string errorMessage))
            {
                TempData["Error"] = errorMessage;
                return RedirectToAction("UploadEntregable", new { id });
            }

            try
            {
                var uploadsFolder = Server.MapPath($"~/App_Data/Uploads/{aspiranteId}/{id}/");
                Directory.CreateDirectory(uploadsFolder);

                var safeFileName = $"{DateTime.Now:yyyyMMddHHmmss}_{Path.GetFileName(archivo.FileName)}";
                var fullPath = Path.Combine(uploadsFolder, safeFileName);
                archivo.SaveAs(fullPath);

                var rutaRelativa = $"~/App_Data/Uploads/{aspiranteId}/{id}/{safeFileName}";

                var submision = db.Ind_Submisiones
                    .FirstOrDefault(s => s.AspiranteID == aspiranteId && s.EntregableID == id);

                if (submision == null)
                {
                    submision = new Ind_Submision
                    {
                        AspiranteID = aspiranteId.Value,
                        EntregableID = id,
                        RutaArchivo = rutaRelativa,
                        FechaEnvio = DateTime.Now,
                        Estado = "Pendiente"
                    };
                    db.Ind_Submisiones.Add(submision);
                }
                else
                {
                    // Re-submission: reset to pending for a fresh review
                    submision.RutaArchivo = rutaRelativa;
                    submision.FechaEnvio = DateTime.Now;
                    submision.Estado = "Pendiente";
                    submision.Calificacion = null;
                    submision.ComentarioRevisor = null;
                    submision.UsuarioRevisorID = null;
                    submision.FechaRevision = null;
                }

                db.SaveChanges();

                TempData["Success"] = $"Archivo para '{entregable.Titulo}' enviado exitosamente.";

                var unidad = db.Ind_Unidades.Find(entregable.UnidadID);
                return RedirectToAction("MateriaDetails", new { id = unidad?.MateriaID });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al subir el archivo: {ex.Message}";
                return RedirectToAction("UploadEntregable", new { id });
            }
        }

        // GET: /Aspirante/DownloadSubmission/5
        public ActionResult DownloadSubmission(int id)
        {
            int? aspiranteId = Session["AspiranteID"] as int?;
            if (aspiranteId == null)
            {
                TempData["Error"] = "Sesión inválida.";
                return RedirectToAction("Index");
            }

            var submision = db.Ind_Submisiones
                .FirstOrDefault(s => s.SubmisionID == id && s.AspiranteID == aspiranteId);

            if (submision == null)
            {
                TempData["Error"] = "Archivo no encontrado.";
                return RedirectToAction("Index");
            }

            var fullPath = Server.MapPath(submision.RutaArchivo);
            if (!System.IO.File.Exists(fullPath))
            {
                TempData["Error"] = "El archivo ya no está disponible en el servidor.";
                return RedirectToAction("Index");
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
