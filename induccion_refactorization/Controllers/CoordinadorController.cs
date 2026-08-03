using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using induccion_refactorization.Filters;
using induccion_refactorization.Helpers;
using induccion_refactorization.Models;
using induccion_refactorization.ViewModels;

namespace induccion_refactorization.Controllers
{
    [RoleAuthorize(3, 5)] // Coordinador y Maestro
    public class CoordinadorController : Controller
    {
        private CaptacionDbContext db = new CaptacionDbContext();

        private int CurrentRolID => (int)(Session["RolID"] ?? 0);
        private int CurrentUsuarioID => (int)(Session["UsuarioID"] ?? 0);
        private List<int> CurrentCarreraIds => CarreraScopeHelper.GetUserCarreraIds(db, CurrentUsuarioID);

        // GET: /Coordinador/Index
        public ActionResult Index()
        {
            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Email = Session["Email"];

            var rolId = CurrentRolID;
            var carreraIds = CurrentCarreraIds;
            var materiasScope = CarreraScopeHelper.ScopeMaterias(db.Ind_Materias.Where(m => m.Activo), rolId, carreraIds);
            var materiaIdsScope = materiasScope.Select(m => m.MateriaID);

            var submisiones = db.Ind_Submisiones
                .Where(s => materiaIdsScope.Contains(s.Ind_Entregable.Ind_Unidad.MateriaID))
                .ToList();
            ViewBag.PendientesCount = submisiones.Count(s => s.Estado == "Pendiente");
            ViewBag.RevisadasCount = submisiones.Count(s => s.Estado == "Revisado");
            ViewBag.RechazadasCount = submisiones.Count(s => s.Estado == "Rechazado");
            ViewBag.TotalMaterias = materiasScope.Count();
            ViewBag.ProgresoPendienteCount = db.Ind_ProgresoAspirante
                .Count(p => p.Estado == "Entregado" && materiaIdsScope.Contains(p.Ind_Unidad.MateriaID));

            return View();
        }

        // GET: /Coordinador/RevisarProgreso
        [RequierePermiso("RevisarUnidades", Accion.Leer)]
        public ActionResult RevisarProgreso(string search, int? materiaId, string sortBy, string sortDir, int page = 1, int pageSize = 10)
        {
            var rolId = CurrentRolID;
            var carreraIds = CurrentCarreraIds;

            var query = db.Ind_ProgresoAspirante
                .Include(p => p.Aspirante.Usuario)
                .Include(p => p.Ind_Unidad.Ind_Materia)
                .Where(p => p.Estado == "Entregado");

            if (CarreraScopeHelper.IsScopedRole(rolId))
            {
                query = query.Where(p => p.Ind_Unidad.Ind_Materia.TodasLasCarreras
                    || p.Ind_Unidad.Ind_Materia.Carreras.Any(c => carreraIds.Contains(c.CarreraID)));
            }

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
            ViewBag.MateriasFiltro = new SelectList(
                CarreraScopeHelper.ScopeMaterias(db.Ind_Materias.Where(m => m.Activo), rolId, carreraIds),
                "MateriaID", "Nombre", materiaId);

            return View(result);
        }

        // GET: /Coordinador/CalificarProgreso/5
        [RequierePermiso("RevisarUnidades", Accion.Leer)]
        public ActionResult CalificarProgreso(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var progreso = db.Ind_ProgresoAspirante
                .Include(p => p.Aspirante.Usuario)
                .Include(p => p.Ind_Unidad.Ind_Materia.Carreras)
                .FirstOrDefault(p => p.ProgresoID == id);

            if (progreso == null)
            {
                return HttpNotFound();
            }

            if (!CarreraScopeHelper.MateriaEnScope(progreso.Ind_Unidad.Ind_Materia, CurrentRolID, CurrentCarreraIds))
            {
                TempData["Error"] = "No tienes acceso a este registro.";
                return RedirectToAction("RevisarProgreso");
            }

            return View(progreso);
        }

        // POST: /Coordinador/CalificarProgreso/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("RevisarUnidades", Accion.Editar)]
        public ActionResult CalificarProgreso(int progresoId, decimal calificacion, string comentariosEvaluador)
        {
            try
            {
                var progreso = db.Ind_ProgresoAspirante
                    .Include(p => p.Ind_Unidad.Ind_Materia.Carreras)
                    .FirstOrDefault(p => p.ProgresoID == progresoId);
                if (progreso == null)
                {
                    TempData["Error"] = "Registro de progreso no encontrado.";
                    return RedirectToAction("RevisarProgreso");
                }

                if (!CarreraScopeHelper.MateriaEnScope(progreso.Ind_Unidad.Ind_Materia, CurrentRolID, CurrentCarreraIds))
                {
                    TempData["Error"] = "No tienes acceso a este registro.";
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
        [RequierePermiso("RevisarEntregables", Accion.Leer)]
        public ActionResult RevisarEntregas(string search, int? materiaId, string sortBy, string sortDir, int page = 1, int pageSize = 10)
        {
            var rolId = CurrentRolID;
            var carreraIds = CurrentCarreraIds;

            var query = db.Ind_Submisiones
                .Include(s => s.Aspirante.Usuario)
                .Include(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia)
                .Where(s => s.Estado == "Pendiente");

            if (CarreraScopeHelper.IsScopedRole(rolId))
            {
                query = query.Where(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia.TodasLasCarreras
                    || s.Ind_Entregable.Ind_Unidad.Ind_Materia.Carreras.Any(c => carreraIds.Contains(c.CarreraID)));
            }

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
            ViewBag.MateriasFiltro = new SelectList(
                CarreraScopeHelper.ScopeMaterias(db.Ind_Materias.Where(m => m.Activo), rolId, carreraIds),
                "MateriaID", "Nombre", materiaId);

            return View(result);
        }

        // GET: /Coordinador/Calificar/5
        [RequierePermiso("RevisarEntregables", Accion.Leer)]
        public ActionResult Calificar(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var submision = db.Ind_Submisiones
                .Include(s => s.Aspirante.Usuario)
                .Include(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia.Carreras)
                .FirstOrDefault(s => s.SubmisionID == id);

            if (submision == null)
            {
                return HttpNotFound();
            }

            if (!CarreraScopeHelper.MateriaEnScope(submision.Ind_Entregable.Ind_Unidad.Ind_Materia, CurrentRolID, CurrentCarreraIds))
            {
                TempData["Error"] = "No tienes acceso a esta entrega.";
                return RedirectToAction("RevisarEntregas");
            }

            return View(submision);
        }

        // POST: /Coordinador/Calificar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("RevisarEntregables", Accion.Editar)]
        public ActionResult Calificar(int submisionId, string estado, decimal? calificacion, string comentarioRevisor)
        {
            try
            {
                var submision = db.Ind_Submisiones
                    .Include(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia.Carreras)
                    .FirstOrDefault(s => s.SubmisionID == submisionId);
                if (submision == null)
                {
                    TempData["Error"] = "Entrega no encontrada.";
                    return RedirectToAction("RevisarEntregas");
                }

                if (!CarreraScopeHelper.MateriaEnScope(submision.Ind_Entregable.Ind_Unidad.Ind_Materia, CurrentRolID, CurrentCarreraIds))
                {
                    TempData["Error"] = "No tienes acceso a esta entrega.";
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

                var fechaRevision = DateTime.Now;

                submision.Estado = estado;
                submision.Calificacion = estado == "Revisado" ? calificacion : null;
                submision.ComentarioRevisor = comentarioRevisor;
                submision.UsuarioRevisorID = Session["UsuarioID"] as int?;
                submision.FechaRevision = fechaRevision;

                // El expediente en dbo.Documentos (tabla compartida con el resto del
                // sistema de captación) también debe reflejar la revisión, no solo el
                // registro propio de Ind_Submisiones.
                if (submision.DocumentoID.HasValue)
                {
                    var documento = db.Documentos.Find(submision.DocumentoID.Value);
                    if (documento != null)
                    {
                        documento.FechaRevision = fechaRevision;
                        documento.EstadoDocumentoID = DocumentoHelper
                            .GetOrCreateEstadoDocumento(db, estado == "Revisado" ? DocumentoHelper.EstadoRevisado : DocumentoHelper.EstadoRechazado)
                            .EstadoDocumentoID;
                    }
                }

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
        [RequierePermiso("RevisarEntregables", Accion.Leer)]
        public ActionResult DownloadSubmission(int id)
        {
            var submision = db.Ind_Submisiones
                .Include(s => s.Documento)
                .Include(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia.Carreras)
                .FirstOrDefault(s => s.SubmisionID == id);
            if (submision == null)
            {
                TempData["Error"] = "Archivo no encontrado.";
                return RedirectToAction("RevisarEntregas");
            }

            if (!CarreraScopeHelper.MateriaEnScope(submision.Ind_Entregable.Ind_Unidad.Ind_Materia, CurrentRolID, CurrentCarreraIds))
            {
                TempData["Error"] = "No tienes acceso a este archivo.";
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

        // GET: /Coordinador/MisAspirantes
        [RequierePermiso("MisAspirantes", Accion.Leer)]
        public ActionResult MisAspirantes(string search, string sortBy, string sortDir, int page = 1, int pageSize = 10)
        {
            var rolId = CurrentRolID;
            var carreraIds = CurrentCarreraIds;

            var aspirantesQuery = db.Aspirantes
                .Include(a => a.Usuario)
                .Where(a => a.Usuario.Carreras.Any(c => carreraIds.Contains(c.CarreraID)));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                aspirantesQuery = aspirantesQuery.Where(a =>
                    a.Usuario.Nombre.ToLower().Contains(term) ||
                    a.Usuario.ApellidoPaterno.ToLower().Contains(term) ||
                    (a.Matricula != null && a.Matricula.ToLower().Contains(term)) ||
                    (a.Folio != null && a.Folio.ToLower().Contains(term)));
            }

            var aspirantesList = aspirantesQuery.OrderBy(a => a.Usuario.Nombre).ToList();
            var aspiranteIds = aspirantesList.Select(a => a.AspiranteID).ToList();

            var progresosPorAspirante = db.Ind_ProgresoAspirante
                .Where(p => aspiranteIds.Contains(p.AspiranteID))
                .ToList()
                .GroupBy(p => p.AspiranteID)
                .ToDictionary(g => g.Key, g => g.ToList());

            var resumenes = aspirantesList.Select(a =>
            {
                var progresos = progresosPorAspirante.TryGetValue(a.AspiranteID, out var list) ? list : new List<Ind_ProgresoAspirante>();
                var calificados = progresos.Where(p => p.Calificacion.HasValue).ToList();
                return new AspiranteResumenViewModel
                {
                    Aspirante = a,
                    TotalUnidadesAsignadas = progresos.Count,
                    UnidadesCompletadas = progresos.Count(p => p.Estado == "Calificado"),
                    PromedioCalificacion = calificados.Any() ? calificados.Average(p => p.Calificacion.Value) : (decimal?)null
                };
            }).ToList();

            bool descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            switch (sortBy)
            {
                case "Progreso":
                    resumenes = (descending ? resumenes.OrderByDescending(r => r.PorcentajeProgreso) : resumenes.OrderBy(r => r.PorcentajeProgreso)).ToList();
                    break;
                case "Promedio":
                    resumenes = (descending ? resumenes.OrderByDescending(r => r.PromedioCalificacion) : resumenes.OrderBy(r => r.PromedioCalificacion)).ToList();
                    break;
                case "Aspirante":
                    resumenes = (descending ? resumenes.OrderByDescending(r => r.Aspirante.Usuario.NombreCompleto) : resumenes.OrderBy(r => r.Aspirante.Usuario.NombreCompleto)).ToList();
                    break;
            }

            var result = PagedResult<AspiranteResumenViewModel>.Create(resumenes.AsQueryable(), page, pageSize);

            // Estadísticas agregadas sobre todo el alcance (no solo la página actual)
            var todosProgresos = progresosPorAspirante.Values.SelectMany(p => p).ToList();
            ViewBag.TotalAspirantes = aspirantesList.Count;
            ViewBag.PromedioAvance = todosProgresos.Any()
                ? (int)((todosProgresos.Count(p => p.Estado == "Calificado") * 100.0) / todosProgresos.Count)
                : 0;
            ViewBag.UnidadesPendientesCalificar = todosProgresos.Count(p => p.Estado == "Entregado");

            var materiaIdsScope = CarreraScopeHelper.ScopeMaterias(db.Ind_Materias.Where(m => m.Activo), rolId, carreraIds).Select(m => m.MateriaID);
            ViewBag.EntregasPendientesCalificar = db.Ind_Submisiones
                .Count(s => s.Estado == "Pendiente" && aspiranteIds.Contains(s.AspiranteID)
                    && materiaIdsScope.Contains(s.Ind_Entregable.Ind_Unidad.MateriaID));

            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDir = sortDir;

            return View(result);
        }

        // GET: /Coordinador/AspiranteDetalle/5
        [RequierePermiso("MisAspirantes", Accion.Leer)]
        public ActionResult AspiranteDetalle(int id, int progresoPage = 1, int progresoPageSize = 10, int submisionPage = 1, int submisionPageSize = 10)
        {
            var carreraIds = CurrentCarreraIds;

            var aspirante = db.Aspirantes
                .Include(a => a.Usuario.Carreras)
                .FirstOrDefault(a => a.AspiranteID == id);

            if (aspirante == null || !aspirante.Usuario.Carreras.Any(c => carreraIds.Contains(c.CarreraID)))
            {
                TempData["Error"] = "No tienes acceso a este aspirante.";
                return RedirectToAction("MisAspirantes");
            }

            var progresosQuery = db.Ind_ProgresoAspirante
                .Include(p => p.Ind_Unidad.Ind_Materia)
                .Where(p => p.AspiranteID == id)
                .OrderBy(p => p.Ind_Unidad.Ind_Materia.Nombre).ThenBy(p => p.Ind_Unidad.Orden);

            var submisionesQuery = db.Ind_Submisiones
                .Include(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia)
                .Where(s => s.AspiranteID == id)
                .OrderByDescending(s => s.FechaEnvio);

            ViewBag.ProgresosResult = PagedResult<Ind_ProgresoAspirante>.Create(progresosQuery, progresoPage, progresoPageSize);
            ViewBag.SubmisionesResult = PagedResult<Ind_Submision>.Create(submisionesQuery, submisionPage, submisionPageSize);
            ViewBag.NombreCompleto = Session["NombreCompleto"];
            return View(aspirante);
        }

        // GET: /Coordinador/AsignarUnidadAspirantes?unidadId=5
        // Alimenta la lista de aspirantes (con su estado "ya asignado") del modal
        // "Asignar" en ManageUnidades vía AJAX, ya que ese modal es genérico para
        // cualquier unidad de la materia en lugar de una página por unidad.
        // Admin también puede asignar (además de Coordinador/Maestro): por eso este
        // método sí incluye el rol 1, aunque el resto del controlador no.
        [HttpGet]
        [RoleAuthorize(CarreraScopeHelper.RolAdmin, CarreraScopeHelper.RolCoordinador, CarreraScopeHelper.RolMaestro)]
        [RequierePermiso("MisAspirantes", Accion.Leer)]
        public JsonResult AsignarUnidadAspirantes(int unidadId)
        {
            var rolId = CurrentRolID;
            var carreraIds = CurrentCarreraIds;

            var unidad = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras).FirstOrDefault(u => u.UnidadID == unidadId);
            if (unidad == null || !CarreraScopeHelper.MateriaEnScope(unidad.Ind_Materia, rolId, carreraIds))
            {
                return Json(new { success = false, message = "No tienes acceso a esta unidad." }, JsonRequestBehavior.AllowGet);
            }

            // IsScopedRole se evalúa aquí (no dentro del Where) porque EF6/LINQ to
            // Entities no puede traducir una llamada a un método C# arbitrario a SQL.
            bool scoped = CarreraScopeHelper.IsScopedRole(rolId);
            var aspirantesScope = db.Aspirantes
                .Include(a => a.Usuario)
                .Where(a => !scoped || a.Usuario.Carreras.Any(c => carreraIds.Contains(c.CarreraID)))
                .OrderBy(a => a.Usuario.Nombre)
                .ToList();

            var yaAsignados = new HashSet<int>(db.Ind_ProgresoAspirante
                .Where(p => p.UnidadID == unidadId)
                .Select(p => p.AspiranteID));

            var aspirantes = aspirantesScope.Select(a => new
            {
                id = a.AspiranteID,
                nombre = a.Usuario?.NombreCompleto,
                matricula = a.Matricula,
                yaAsignado = yaAsignados.Contains(a.AspiranteID)
            });

            return Json(new { success = true, aspirantes }, JsonRequestBehavior.AllowGet);
        }

        // POST: /Coordinador/AsignarUnidad
        // Admin también puede asignar (además de Coordinador/Maestro): por eso este
        // método sí incluye el rol 1, aunque el resto del controlador no.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(CarreraScopeHelper.RolAdmin, CarreraScopeHelper.RolCoordinador, CarreraScopeHelper.RolMaestro)]
        [RequierePermiso("MisAspirantes", Accion.Crear)]
        public ActionResult AsignarUnidad(int unidadId, string modo, int[] aspiranteIds, bool reasignar)
        {
            var rolId = CurrentRolID;
            var carreraIds = CurrentCarreraIds;

            var unidad = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras).FirstOrDefault(u => u.UnidadID == unidadId);
            if (!CarreraScopeHelper.MateriaEnScope(unidad?.Ind_Materia, rolId, carreraIds))
            {
                TempData["Error"] = "No tienes acceso a esta unidad.";
                return RedirectToAction("Index", "InductionMaintenance");
            }

            // IsScopedRole se evalúa aquí (no dentro del Where) porque EF6/LINQ to
            // Entities no puede traducir una llamada a un método C# arbitrario a SQL.
            bool scoped = CarreraScopeHelper.IsScopedRole(rolId);
            var aspirantesScopeIds = db.Aspirantes
                .Where(a => !scoped || a.Usuario.Carreras.Any(c => carreraIds.Contains(c.CarreraID)))
                .Select(a => a.AspiranteID)
                .ToList();

            // Sin importar lo que se haya mandado en el POST, el destino siempre se
            // restringe a aspirantes dentro del propio alcance de carrera.
            List<int> targetIds = string.Equals(modo, "todos", StringComparison.OrdinalIgnoreCase)
                ? aspirantesScopeIds
                : (aspiranteIds ?? new int[0]).Where(aid => aspirantesScopeIds.Contains(aid)).ToList();

            if (targetIds.Count == 0)
            {
                TempData["Error"] = "No se seleccionó ningún aspirante.";
                return RedirectToAction("ManageUnidades", "InductionMaintenance", new { id = unidad.MateriaID });
            }

            try
            {
                var existentes = db.Ind_ProgresoAspirante
                    .Where(p => p.UnidadID == unidadId && targetIds.Contains(p.AspiranteID))
                    .ToList();
                var existentesIds = new HashSet<int>(existentes.Select(p => p.AspiranteID));

                int nuevos = 0, reasignados = 0;

                foreach (var aspiranteId in targetIds)
                {
                    if (existentesIds.Contains(aspiranteId))
                    {
                        if (reasignar)
                        {
                            var progreso = existentes.First(p => p.AspiranteID == aspiranteId);
                            progreso.Estado = "Asignado";
                            progreso.Calificacion = null;
                            progreso.ComentariosEvaluador = null;
                            progreso.FechaEnvio = null;
                            progreso.FechaAsignacion = DateTime.Now;
                            progreso.UsuarioCalificadorID = null;
                            reasignados++;
                        }
                    }
                    else
                    {
                        db.Ind_ProgresoAspirante.Add(new Ind_ProgresoAspirante
                        {
                            AspiranteID = aspiranteId,
                            UnidadID = unidadId,
                            Estado = "Asignado",
                            FechaAsignacion = DateTime.Now
                        });
                        nuevos++;
                    }
                }

                db.SaveChanges();

                TempData["Success"] = $"Unidad asignada: {nuevos} nuevo(s), {reasignados} reasignado(s).";
                return RedirectToAction("ManageUnidades", "InductionMaintenance", new { id = unidad.MateriaID });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al asignar: {ex.Message}";
                return RedirectToAction("ManageUnidades", "InductionMaintenance", new { id = unidad.MateriaID });
            }
        }

        // GET: /Coordinador/MisMaestros
        // Solo Coordinador (no Maestro) puede ver esto, aunque el resto del controlador se comparte.
        [RoleAuthorize(CarreraScopeHelper.RolCoordinador)]
        [RequierePermiso("MisMaestros", Accion.Leer)]
        public ActionResult MisMaestros(string search, string sortBy, string sortDir, int page = 1, int pageSize = 10)
        {
            var carreraIds = CurrentCarreraIds;

            var maestrosQuery = db.Usuarios
                .Include(u => u.Carreras)
                .Where(u => u.RolID == CarreraScopeHelper.RolMaestro && u.Activo && u.Carreras.Any(c => carreraIds.Contains(c.CarreraID)));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                maestrosQuery = maestrosQuery.Where(u =>
                    u.Nombre.ToLower().Contains(term) ||
                    u.ApellidoPaterno.ToLower().Contains(term) ||
                    u.CorreoElectronico.ToLower().Contains(term));
            }

            var maestros = maestrosQuery.OrderBy(u => u.Nombre).ToList();

            var resumenes = maestros.Select(m =>
            {
                var carrerasCompartidas = m.Carreras.Select(c => c.CarreraID).Intersect(carreraIds).ToList();
                var materiaIdsCompartidas = CarreraScopeHelper
                    .ScopeMaterias(db.Ind_Materias.Where(x => x.Activo), CarreraScopeHelper.RolMaestro, carrerasCompartidas)
                    .Select(x => x.MateriaID)
                    .ToList();

                return new MaestroResumenViewModel
                {
                    Usuario = m,
                    TotalMateriasCompartidas = materiaIdsCompartidas.Count,
                    UnidadesCalificadasPorEl = db.Ind_ProgresoAspirante.Count(p => p.UsuarioCalificadorID == m.UsuarioID),
                    EntregasCalificadasPorEl = db.Ind_Submisiones.Count(s => s.UsuarioRevisorID == m.UsuarioID)
                };
            }).ToList();

            bool descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            switch (sortBy)
            {
                case "Materias":
                    resumenes = (descending ? resumenes.OrderByDescending(r => r.TotalMateriasCompartidas) : resumenes.OrderBy(r => r.TotalMateriasCompartidas)).ToList();
                    break;
                case "Unidades":
                    resumenes = (descending ? resumenes.OrderByDescending(r => r.UnidadesCalificadasPorEl) : resumenes.OrderBy(r => r.UnidadesCalificadasPorEl)).ToList();
                    break;
                case "Entregas":
                    resumenes = (descending ? resumenes.OrderByDescending(r => r.EntregasCalificadasPorEl) : resumenes.OrderBy(r => r.EntregasCalificadasPorEl)).ToList();
                    break;
                case "Maestro":
                    resumenes = (descending ? resumenes.OrderByDescending(r => r.Usuario.NombreCompleto) : resumenes.OrderBy(r => r.Usuario.NombreCompleto)).ToList();
                    break;
            }

            var result = PagedResult<MaestroResumenViewModel>.Create(resumenes.AsQueryable(), page, pageSize);

            // Estadísticas agregadas sobre todo el alcance (no solo la página actual)
            ViewBag.TotalMaestros = maestros.Count;
            ViewBag.TotalMateriasCompartidas = resumenes.Sum(r => r.TotalMateriasCompartidas);
            ViewBag.TotalUnidadesCalificadas = resumenes.Sum(r => r.UnidadesCalificadasPorEl);
            ViewBag.TotalEntregasCalificadas = resumenes.Sum(r => r.EntregasCalificadasPorEl);

            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDir = sortDir;

            return View(result);
        }

        // GET: /Coordinador/MaestroDetalle/5
        [RoleAuthorize(CarreraScopeHelper.RolCoordinador)]
        [RequierePermiso("MisMaestros", Accion.Leer)]
        public ActionResult MaestroDetalle(int id, int materiaPage = 1, int materiaPageSize = 10, int progresoPage = 1, int progresoPageSize = 10, int entregaPage = 1, int entregaPageSize = 10)
        {
            var carreraIds = CurrentCarreraIds;

            var maestro = db.Usuarios
                .Include(u => u.Carreras)
                .FirstOrDefault(u => u.UsuarioID == id && u.RolID == CarreraScopeHelper.RolMaestro);

            if (maestro == null || !maestro.Carreras.Any(c => carreraIds.Contains(c.CarreraID)))
            {
                TempData["Error"] = "No tienes acceso a este maestro.";
                return RedirectToAction("MisMaestros");
            }

            var carrerasCompartidas = maestro.Carreras.Select(c => c.CarreraID).Intersect(carreraIds).ToList();

            var materiasQuery = CarreraScopeHelper.ScopeMaterias(
                    db.Ind_Materias.Include(m => m.Ind_Unidades.Select(u => u.Ind_Entregables)).Where(m => m.Activo),
                    CarreraScopeHelper.RolMaestro, carrerasCompartidas)
                .OrderBy(m => m.Nombre);

            var progresosQuery = db.Ind_ProgresoAspirante
                .Include(p => p.Aspirante.Usuario)
                .Include(p => p.Ind_Unidad.Ind_Materia)
                .Where(p => p.UsuarioCalificadorID == id)
                .OrderByDescending(p => p.ProgresoID);

            var entregasQuery = db.Ind_Submisiones
                .Include(s => s.Aspirante.Usuario)
                .Include(s => s.Ind_Entregable)
                .Where(s => s.UsuarioRevisorID == id)
                .OrderByDescending(s => s.SubmisionID);

            ViewBag.MateriasResult = PagedResult<Ind_Materia>.Create(materiasQuery, materiaPage, materiaPageSize);
            ViewBag.ProgresosResult = PagedResult<Ind_ProgresoAspirante>.Create(progresosQuery, progresoPage, progresoPageSize);
            ViewBag.EntregasResult = PagedResult<Ind_Submision>.Create(entregasQuery, entregaPage, entregaPageSize);
            ViewBag.NombreCompleto = Session["NombreCompleto"];
            return View(maestro);
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
