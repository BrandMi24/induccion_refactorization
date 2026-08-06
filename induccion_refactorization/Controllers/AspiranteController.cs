using System;
using System.Collections.Generic;
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

        private const int CursosPreviewCount = 2;

        // GET: /Aspirante/Index
        [RequierePermiso("MiEspacio", Accion.Leer)]
        public ActionResult Index()
        {
            var model = new AspiranteDashboardViewModel
            {
                NombreCompleto = Session["NombreCompleto"] as string,
                Email = Session["Email"] as string,
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

                // Full list feeds the "ver más" modal; the page itself only shows a preview.
                model.MateriasProgreso = GetMateriasProgreso(aspiranteId.Value);
                ViewBag.CursosPreviewCount = CursosPreviewCount;

                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al cargar datos: {ex.Message} {ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message}";
                return View(model);
            }
        }

        private List<MateriaProgresoViewModel> GetMateriasProgreso(int aspiranteId)
        {
            var progresos = db.Ind_ProgresoAspirante
                .Include(p => p.Ind_Unidad.Ind_Materia)
                .Where(p => p.AspiranteID == aspiranteId)
                .ToList();

            // MateriaID de cada entregable con un devuelto pendiente de resubir, para
            // marcar esos cursos con "acción requerida" en el panel principal.
            var materiaIdsConDevueltos = new HashSet<int>(db.Ind_Submisiones
                .Where(s => s.AspiranteID == aspiranteId && s.Estado == "Rechazado")
                .Select(s => s.Ind_Entregable.Ind_Unidad.MateriaID));

            // MateriaID de las materias cuya pantalla de felicitación ya fue vista
            // (y descartada permanentemente) por este aspirante.
            var materiaIdsFelicitacionVista = new HashSet<int>(db.Ind_FelicitacionesVistas
                .Where(f => f.AspiranteID == aspiranteId)
                .Select(f => f.MateriaID));

            return progresos
                .Where(p => p.Ind_Unidad != null && p.Ind_Unidad.Ind_Materia != null)
                .GroupBy(p => p.Ind_Unidad.Ind_Materia)
                .Select(g =>
                {
                    var totalUnidades = g.Count();
                    var unidadesCompletadas = g.Count(p => p.Estado == "Revisado");
                    var completada = totalUnidades > 0 && unidadesCompletadas == totalUnidades;
                    return new MateriaProgresoViewModel
                    {
                        Materia = g.Key,
                        TotalUnidades = totalUnidades,
                        UnidadesCompletadas = unidadesCompletadas,
                        ProgresoAspirantes = g.ToList(),
                        TieneAccionRequerida = materiaIdsConDevueltos.Contains(g.Key.MateriaID),
                        TieneFelicitacionPendiente = completada && !materiaIdsFelicitacionVista.Contains(g.Key.MateriaID)
                    };
                })
                // Primero los cursos que necesitan atención (acción requerida), luego
                // los que se acaban de completar y todavía no se les mostró/descartó
                // la pantalla de felicitación, y por último el resto por nombre.
                .OrderByDescending(m => m.TieneAccionRequerida)
                .ThenByDescending(m => m.TieneFelicitacionPendiente)
                .ThenBy(m => m.Materia.Nombre)
                .ToList();
        }

        // GET: /Aspirante/MateriaDetails/5
        [RequierePermiso("MiEspacio", Accion.Leer)]
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
                ViewBag.FelicitacionYaVista = db.Ind_FelicitacionesVistas
                    .Any(f => f.AspiranteID == aspiranteId && f.MateriaID == id);

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
        [RequierePermiso("MiEspacio", Accion.Editar)]
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
                    TempData["Error"] = "Esta unidad ya fue entregada o revisada.";
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

        // POST: /Aspirante/MarcarFelicitacionVista/5
        // Descarta permanentemente la pantalla de "¡Felicidades!" de una materia
        // para que no vuelva a aparecer cada vez que el aspirante visite su
        // contenido (solo se llama cuando marcan la casilla "No volver a mostrar").
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("MiEspacio", Accion.Editar)]
        public ActionResult MarcarFelicitacionVista(int materiaId, bool noMostrarDeNuevo = false)
        {
            int? aspiranteId = Session["AspiranteID"] as int?;
            if (aspiranteId != null && noMostrarDeNuevo &&
                !db.Ind_FelicitacionesVistas.Any(f => f.AspiranteID == aspiranteId && f.MateriaID == materiaId))
            {
                db.Ind_FelicitacionesVistas.Add(new Ind_FelicitacionVista
                {
                    AspiranteID = aspiranteId.Value,
                    MateriaID = materiaId,
                    FechaVista = DateTime.Now
                });
                db.SaveChanges();
            }

            return RedirectToAction("MateriaDetails", new { id = materiaId });
        }

        // POST: /Aspirante/UploadEntregable/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("SubirEntregables", Accion.Crear)]
        public ActionResult UploadEntregable(int id, HttpPostedFileBase archivo)
        {
            int? aspiranteId = Session["AspiranteID"] as int?;
            if (aspiranteId == null)
            {
                TempData["Error"] = "Sesión inválida.";
                return RedirectToAction("Index");
            }

            var entregable = db.Ind_Entregables.Include(e => e.Ind_Unidad).FirstOrDefault(e => e.EntregableID == id && e.Activo);
            if (entregable == null)
            {
                TempData["Error"] = "Entregable no encontrado.";
                return RedirectToAction("Index");
            }

            if (!FileUploadValidator.IsValid(archivo, out string errorMessage))
            {
                TempData["Error"] = errorMessage;
                return RedirectToAction("MateriaDetails", new { id = entregable.Ind_Unidad.MateriaID });
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

                // Record the file in the shared Documentos table (rich metadata + versioning)
                var tipoDocumento = DocumentoHelper.GetOrCreateTipoDocumento(db, DocumentoHelper.TipoEntregableInduccion);
                var estadoDocumento = DocumentoHelper.GetOrCreateEstadoDocumento(db, DocumentoHelper.EstadoPendiente);

                var previousDocumento = submision?.DocumentoID != null
                    ? db.Documentos.Find(submision.DocumentoID)
                    : null;

                if (previousDocumento != null)
                {
                    previousDocumento.VersionActual = false;
                }

                var documento = new Documento
                {
                    NombreOriginal = Path.GetFileName(archivo.FileName),
                    ExtensionArchivo = Path.GetExtension(archivo.FileName).ToLowerInvariant(),
                    TipoMIME = System.Web.MimeMapping.GetMimeMapping(archivo.FileName),
                    TamanoArchivoBytes = archivo.ContentLength,
                    RutaAlmacenamiento = rutaRelativa,
                    HashArchivo = DocumentoHelper.ComputeSha256Hash(fullPath),
                    FechaSubida = DateTime.Now,
                    NumeroVersion = (previousDocumento?.NumeroVersion ?? 0) + 1,
                    VersionActual = true,
                    AspiranteID = aspiranteId.Value,
                    TipoDocumentoID = tipoDocumento.TipoDocumentoID,
                    EstadoDocumentoID = estadoDocumento.EstadoDocumentoID,
                    UsuarioID = Session["UsuarioID"] as int?
                };
                db.Documentos.Add(documento);

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
                    submision.ComentarioRevisor = null;
                    submision.UsuarioRevisorID = null;
                    submision.FechaRevision = null;
                }

                submision.Documento = documento;

                db.SaveChanges();

                // La unidad pasa a "Entregado" sola en cuanto el aspirante ya subió
                // archivo para TODOS sus entregables — ya no existe un botón de
                // "Marcar como Entregado" aparte, porque ahora toda unidad requiere
                // al menos un entregable.
                var unidadId = entregable.UnidadID;
                var entregableIdsUnidad = db.Ind_Entregables
                    .Where(e => e.UnidadID == unidadId && e.Activo)
                    .Select(e => e.EntregableID)
                    .ToList();
                var todosEnviados = entregableIdsUnidad.All(eid =>
                    db.Ind_Submisiones.Any(s => s.AspiranteID == aspiranteId && s.EntregableID == eid));

                if (todosEnviados)
                {
                    var progreso = db.Ind_ProgresoAspirante
                        .FirstOrDefault(p => p.AspiranteID == aspiranteId && p.UnidadID == unidadId);
                    if (progreso != null && progreso.Estado == "Asignado")
                    {
                        progreso.Estado = "Entregado";
                        progreso.FechaEnvio = DateTime.Now;
                        db.SaveChanges();
                    }
                }

                TempData["Success"] = $"Archivo para '{entregable.Titulo}' enviado exitosamente.";

                return RedirectToAction("MateriaDetails", new { id = entregable.Ind_Unidad.MateriaID });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al subir el archivo: {ex.Message}";
                return RedirectToAction("MateriaDetails", new { id = entregable.Ind_Unidad.MateriaID });
            }
        }

        // GET: /Aspirante/DownloadSubmission/5
        [RequierePermiso("MiEspacio", Accion.Leer)]
        public ActionResult DownloadSubmission(int id)
        {
            int? aspiranteId = Session["AspiranteID"] as int?;
            if (aspiranteId == null)
            {
                TempData["Error"] = "Sesión inválida.";
                return RedirectToAction("Index");
            }

            var submision = db.Ind_Submisiones
                .Include(s => s.Documento)
                .FirstOrDefault(s => s.SubmisionID == id && s.AspiranteID == aspiranteId);

            if (submision == null)
            {
                TempData["Error"] = "Archivo no encontrado.";
                return RedirectToAction("Index");
            }

            var rutaArchivo = submision.Documento?.RutaAlmacenamiento ?? submision.RutaArchivo;
            var nombreDescarga = submision.Documento?.NombreOriginal ?? Path.GetFileName(rutaArchivo);
            var tipoMime = submision.Documento?.TipoMIME ?? "application/octet-stream";

            var fullPath = Server.MapPath(rutaArchivo);
            if (!System.IO.File.Exists(fullPath))
            {
                TempData["Error"] = "El archivo ya no está disponible en el servidor.";
                return RedirectToAction("Index");
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
