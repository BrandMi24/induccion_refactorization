using System;
using System.Collections.Generic;
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
    [RoleAuthorize(1, 3, 5)] // Admin, Coordinador y Maestro
    public class InductionMaintenanceController : Controller
    {
        private CaptacionDbContext db = new CaptacionDbContext();

        private int CurrentRolID => (int)(Session["RolID"] ?? 0);
        private int CurrentUsuarioID => (int)(Session["UsuarioID"] ?? 0);
        private List<int> CurrentCarreraIds => CarreraScopeHelper.GetUserCarreraIds(db, CurrentUsuarioID);

        // GET: /InductionMaintenance/Index
        [RequierePermiso("GestionContenido", Accion.Leer)]
        public ActionResult Index(string search, int? carreraId, int? periodoId, string sortBy, string sortDir, int page = 1, int pageSize = 10)
        {
            var rolId = CurrentRolID;
            var carreraIds = CurrentCarreraIds;

            var query = db.Ind_Materias
                .Include(m => m.Carreras)
                .Include(m => m.Periodo)
                .Include(m => m.Ind_Unidades)
                .Where(m => m.Activo);

            query = CarreraScopeHelper.ScopeMaterias(query, rolId, carreraIds);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(m => m.Nombre.ToLower().Contains(term));
            }

            if (carreraId.HasValue)
            {
                query = query.Where(m => m.TodasLasCarreras || m.Carreras.Any(c => c.CarreraID == carreraId.Value));
            }

            if (periodoId.HasValue)
            {
                query = query.Where(m => m.PeriodoID == periodoId.Value);
            }

            bool descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            switch (sortBy)
            {
                case "Carrera":
                    query = descending
                        ? query.OrderByDescending(m => m.TodasLasCarreras).ThenByDescending(m => m.MateriaID)
                        : query.OrderBy(m => m.TodasLasCarreras).ThenBy(m => m.MateriaID);
                    break;
                case "Periodo":
                    query = descending ? query.OrderByDescending(m => m.Periodo.FechaInicio) : query.OrderBy(m => m.Periodo.FechaInicio);
                    break;
                case "Unidades":
                    query = descending ? query.OrderByDescending(m => m.Ind_Unidades.Count) : query.OrderBy(m => m.Ind_Unidades.Count);
                    break;
                case "Nombre":
                    query = descending ? query.OrderByDescending(m => m.Nombre) : query.OrderBy(m => m.Nombre);
                    break;
                default:
                    query = query.OrderBy(m => m.MateriaID);
                    break;
            }

            var result = PagedResult<Ind_Materia>.Create(query, page, pageSize);

            var materiasScope = CarreraScopeHelper.ScopeMaterias(db.Ind_Materias.Where(m => m.Activo), rolId, carreraIds);
            ViewBag.TotalMaterias = materiasScope.Count();
            ViewBag.TotalUnidades = materiasScope.SelectMany(m => m.Ind_Unidades).Count();
            ViewBag.TotalMateriales = materiasScope.SelectMany(m => m.Ind_Unidades.SelectMany(u => u.Ind_Materiales)).Count();
            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Search = search;
            ViewBag.CarreraId = carreraId;
            ViewBag.PeriodoId = periodoId;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDir = sortDir;

            var carrerasFiltroQuery = CarreraScopeHelper.IsScopedRole(rolId)
                ? db.Carreras.Where(c => carreraIds.Contains(c.CarreraID))
                : db.Carreras;
            ViewBag.CarrerasFiltro = new SelectList(carrerasFiltroQuery, "CarreraID", "Nombre", carreraId);
            ViewBag.PeriodosFiltro = new SelectList(db.Periodos, "PeriodoID", "Nombre", periodoId);

            // Datos para los modales de Nueva Materia / Editar Materia (ambos viven en
            // esta misma vista en vez de páginas aparte). Solo se ofrecen carreras
            // activas para asignar (igual que Periodos), aunque una materia ya
            // asignada a una carrera desactivada conserva esa asignación.
            ViewBag.CarrerasList = CarreraScopeHelper.IsScopedRole(rolId)
                ? db.Carreras.Where(c => c.Activo && carreraIds.Contains(c.CarreraID)).OrderBy(c => c.Nombre).ToList()
                : db.Carreras.Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
            ViewBag.PuedeElegirTodasLasCarreras = rolId == CarreraScopeHelper.RolAdmin;
            ViewBag.PeriodosList = new SelectList(db.Periodos.Where(p => p.Activo), "PeriodoID", "Nombre");
            PermissionHelper.AsignarFlagsVista(ViewBag, db, CurrentUsuarioID, rolId, "GestionContenido");

            return View(result);
        }

        // POST: /InductionMaintenance/CreateMateria
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Crear)]
        public ActionResult CreateMateria(Ind_Materia materia, int[] carreraIds, bool todasLasCarreras)
        {
            ModelState.Remove("Carreras");

            var rolId = CurrentRolID;
            var userCarreraIds = CurrentCarreraIds;

            // Un Coordinador/Maestro nunca puede marcar "todas las carreras" ni asignar
            // una materia a una carrera fuera de su propio alcance, sin importar lo que
            // haya llegado en el POST.
            if (CarreraScopeHelper.IsScopedRole(rolId))
            {
                todasLasCarreras = false;
                carreraIds = carreraIds?.Where(id => userCarreraIds.Contains(id)).ToArray();
            }

            if (!todasLasCarreras && (carreraIds == null || carreraIds.Length == 0))
            {
                ModelState.AddModelError("", "Selecciona al menos una carrera, o marca \"Visible para todas las carreras\".");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    materia.Activo = true;
                    materia.TodasLasCarreras = todasLasCarreras;

                    if (!todasLasCarreras)
                    {
                        var carreras = db.Carreras.Where(c => carreraIds.Contains(c.CarreraID)).ToList();
                        foreach (var carrera in carreras)
                        {
                            materia.Carreras.Add(carrera);
                        }
                    }

                    db.Ind_Materias.Add(materia);
                    db.SaveChanges();

                    TempData["Success"] = $"Materia '{materia.Nombre}' creada exitosamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al guardar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo crear la materia. Verifica los datos.";
            }

            return RedirectToAction("Index");
        }

        // POST: /InductionMaintenance/EditMateria
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Editar)]
        public ActionResult EditMateria(Ind_Materia materia, int[] carreraIds, bool todasLasCarreras)
        {
            ModelState.Remove("Carreras");

            var rolId = CurrentRolID;
            var userCarreraIds = CurrentCarreraIds;

            if (CarreraScopeHelper.IsScopedRole(rolId))
            {
                todasLasCarreras = false;
                carreraIds = carreraIds?.Where(cid => userCarreraIds.Contains(cid)).ToArray();
            }

            if (!todasLasCarreras && (carreraIds == null || carreraIds.Length == 0))
            {
                ModelState.AddModelError("", "Selecciona al menos una carrera, o marca \"Visible para todas las carreras\".");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = db.Ind_Materias
                        .Include(m => m.Carreras)
                        .FirstOrDefault(m => m.MateriaID == materia.MateriaID);
                    if (existing == null)
                    {
                        TempData["Error"] = "Materia no encontrada.";
                        return RedirectToAction("Index");
                    }

                    if (!CarreraScopeHelper.MateriaEnScope(existing, rolId, userCarreraIds))
                    {
                        TempData["Error"] = "No tienes acceso a esta materia.";
                        return RedirectToAction("Index");
                    }

                    existing.Nombre = materia.Nombre;
                    existing.Descripcion = materia.Descripcion;
                    existing.PeriodoID = materia.PeriodoID;
                    existing.TodasLasCarreras = todasLasCarreras;

                    existing.Carreras.Clear();
                    if (!todasLasCarreras)
                    {
                        var carreras = db.Carreras.Where(c => carreraIds.Contains(c.CarreraID)).ToList();
                        foreach (var carrera in carreras)
                        {
                            existing.Carreras.Add(carrera);
                        }
                    }

                    db.SaveChanges();

                    TempData["Success"] = $"Materia '{existing.Nombre}' actualizada exitosamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al actualizar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar la materia. Verifica los datos.";
            }

            return RedirectToAction("Index");
        }

        // GET: /InductionMaintenance/ManageUnidades/5
        [RequierePermiso("GestionContenido", Accion.Leer)]
        public ActionResult ManageUnidades(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var materia = db.Ind_Materias
                .Include(m => m.Carreras)
                .Include(m => m.Ind_Unidades.Select(u => u.Ind_Materiales))
                .Include(m => m.Ind_Unidades.Select(u => u.Ind_Entregables))
                .FirstOrDefault(m => m.MateriaID == id);

            if (materia == null)
            {
                return HttpNotFound();
            }

            if (!CarreraScopeHelper.MateriaEnScope(materia, CurrentRolID, CurrentCarreraIds))
            {
                TempData["Error"] = "No tienes acceso a esta materia.";
                return RedirectToAction("Index");
            }

            ViewBag.MateriaNombre = materia.Nombre;
            ViewBag.MateriaID = materia.MateriaID;
            PermissionHelper.AsignarFlagsVista(ViewBag, db, CurrentUsuarioID, CurrentRolID, "GestionContenido");
            ViewBag.PuedeAsignar = PermissionHelper.TieneAcceso(db, CurrentUsuarioID, CurrentRolID, "MisAspirantes", Accion.Crear);
            return View(materia.Ind_Unidades.OrderBy(u => u.Orden).ToList());
        }

        // POST: /InductionMaintenance/GuardarCambiosUnidades
        // Reemplaza a las 12 acciones individuales de Crear/Editar/Eliminar/Reordenar
        // unidad/material/entregable: la vista Gestionar Unidades ahora arma todos los
        // cambios en un árbol de estado en el navegador y los manda de un solo golpe
        // aquí cuando se aprieta "Guardar Cambios". Todo o nada, dentro de una transacción.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Editar)]
        public JsonResult GuardarCambiosUnidades(int materiaId, string cambiosJson)
        {
            var materia = db.Ind_Materias
                .Include(m => m.Carreras)
                .Include(m => m.Ind_Unidades.Select(u => u.Ind_Materiales))
                .Include(m => m.Ind_Unidades.Select(u => u.Ind_Entregables))
                .FirstOrDefault(m => m.MateriaID == materiaId);

            if (!CarreraScopeHelper.MateriaEnScope(materia, CurrentRolID, CurrentCarreraIds))
            {
                return Json(new { success = false, message = "No tienes acceso a esta materia." });
            }

            List<UnidadCambioDto> unidadesCambio;
            try
            {
                unidadesCambio = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UnidadCambioDto>>(cambiosJson ?? "[]") ?? new List<UnidadCambioDto>();
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Los datos enviados no son válidos." });
            }

            // Los IDs que llegan deben pertenecer a ESTA materia (evita que alguien
            // edite/elimine unidades, materiales o entregables de otra materia).
            var unidadIdsValidos = materia.Ind_Unidades.Select(u => u.UnidadID).ToList();
            var materialIdsValidos = materia.Ind_Unidades.SelectMany(u => u.Ind_Materiales).Select(m => m.MaterialID).ToList();
            var entregableIdsValidos = materia.Ind_Unidades.SelectMany(u => u.Ind_Entregables).Select(e => e.EntregableID).ToList();

            foreach (var u in unidadesCambio)
            {
                if (u.UnidadID.HasValue && !unidadIdsValidos.Contains(u.UnidadID.Value))
                {
                    return Json(new { success = false, message = "Solicitud inválida." });
                }
                if (u.Materiales.Any(m => m.MaterialID.HasValue && !materialIdsValidos.Contains(m.MaterialID.Value)))
                {
                    return Json(new { success = false, message = "Solicitud inválida." });
                }
                if (u.Entregables.Any(e => e.EntregableID.HasValue && !entregableIdsValidos.Contains(e.EntregableID.Value)))
                {
                    return Json(new { success = false, message = "Solicitud inválida." });
                }
            }

            foreach (var u in unidadesCambio.Where(u => !u.Eliminado))
            {
                if (string.IsNullOrWhiteSpace(u.Nombre))
                {
                    return Json(new { success = false, message = "Todas las unidades deben tener un nombre." });
                }
                if (u.Materiales.Where(m => !m.Eliminado).Any(m => string.IsNullOrWhiteSpace(m.Nombre) || string.IsNullOrWhiteSpace(m.TipoRecurso) || string.IsNullOrWhiteSpace(m.RutaURL)))
                {
                    return Json(new { success = false, message = "Todos los materiales deben tener nombre, tipo de recurso y URL." });
                }
                if (u.Entregables.Where(e => !e.Eliminado).Any(e => string.IsNullOrWhiteSpace(e.Titulo)))
                {
                    return Json(new { success = false, message = "Todos los entregables deben tener un título." });
                }
            }

            // Cada unidad necesita al menos un entregable activo — ya no basta con
            // que la materia tenga uno en total, porque una unidad sin entregable
            // nunca podría marcarse como revisada (eso ahora depende por completo
            // de que se revisen sus entregables).
            var unidadSinEntregable = unidadesCambio
                .Where(u => !u.Eliminado)
                .FirstOrDefault(u => !u.Entregables.Any(e => !e.Eliminado));
            if (unidadSinEntregable != null)
            {
                var nombreUnidad = string.IsNullOrWhiteSpace(unidadSinEntregable.Nombre) ? "(sin nombre)" : unidadSinEntregable.Nombre;
                return Json(new { success = false, message = $"La unidad '{nombreUnidad}' necesita al menos un entregable activo. Agrega uno antes de guardar." });
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var ordenUnidad = 0;
                    foreach (var uDto in unidadesCambio)
                    {
                        Ind_Unidad unidad;
                        if (uDto.UnidadID.HasValue)
                        {
                            unidad = materia.Ind_Unidades.First(u => u.UnidadID == uDto.UnidadID.Value);
                            if (uDto.Eliminado)
                            {
                                // Los materiales y entregables ya están cargados (Include) y EF los
                                // sigue rastreando: hay que quitarlos explícitamente antes de borrar
                                // la unidad, porque su FK a UnidadID no es nullable (sin ON DELETE CASCADE).
                                db.Ind_Materiales.RemoveRange(unidad.Ind_Materiales.ToList());
                                db.Ind_Entregables.RemoveRange(unidad.Ind_Entregables.ToList());
                                db.Ind_Unidades.Remove(unidad);
                                continue;
                            }
                        }
                        else
                        {
                            if (uDto.Eliminado) continue;
                            unidad = new Ind_Unidad { MateriaID = materiaId };
                            db.Ind_Unidades.Add(unidad);
                        }

                        unidad.Nombre = uDto.Nombre.Trim();
                        ordenUnidad++;
                        unidad.Orden = ordenUnidad;

                        var ordenMaterial = 0;
                        foreach (var mDto in uDto.Materiales)
                        {
                            Ind_Material material;
                            if (mDto.MaterialID.HasValue)
                            {
                                material = unidad.Ind_Materiales.First(m => m.MaterialID == mDto.MaterialID.Value);
                                if (mDto.Eliminado)
                                {
                                    db.Ind_Materiales.Remove(material);
                                    continue;
                                }
                            }
                            else
                            {
                                if (mDto.Eliminado) continue;
                                material = new Ind_Material();
                                unidad.Ind_Materiales.Add(material);
                            }

                            material.Nombre = mDto.Nombre.Trim();
                            material.TipoRecurso = mDto.TipoRecurso.Trim();
                            material.RutaURL = mDto.RutaURL.Trim();
                            ordenMaterial++;
                            material.Orden = ordenMaterial;
                        }

                        var ordenEntregable = 0;
                        foreach (var eDto in uDto.Entregables)
                        {
                            Ind_Entregable entregable;
                            if (eDto.EntregableID.HasValue)
                            {
                                entregable = unidad.Ind_Entregables.First(e => e.EntregableID == eDto.EntregableID.Value);
                            }
                            else
                            {
                                if (eDto.Eliminado) continue;
                                entregable = new Ind_Entregable();
                                unidad.Ind_Entregables.Add(entregable);
                            }

                            if (eDto.Eliminado)
                            {
                                // Baja lógica, igual que la vieja DeleteEntregable (puede tener submisiones asociadas).
                                entregable.Activo = false;
                                continue;
                            }

                            entregable.Titulo = eDto.Titulo.Trim();
                            entregable.Instrucciones = eDto.Instrucciones;
                            entregable.FechaLimite = eDto.FechaLimite;
                            entregable.Activo = true;
                            ordenEntregable++;
                            entregable.Orden = ordenEntregable;
                        }
                    }

                    db.SaveChanges();
                    transaction.Commit();
                    return Json(new { success = true, message = "Cambios guardados exitosamente." });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = $"Error al guardar los cambios: {ex.Message}" });
                }
            }
        }

        // POST: /InductionMaintenance/DeleteMateria/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Eliminar)]
        public ActionResult DeleteMateria(int id)
        {
            try
            {
                var materia = db.Ind_Materias.Include(m => m.Carreras).FirstOrDefault(m => m.MateriaID == id);
                if (materia == null)
                {
                    TempData["Error"] = "Materia no encontrada.";
                    return RedirectToAction("Index");
                }

                if (!CarreraScopeHelper.MateriaEnScope(materia, CurrentRolID, CurrentCarreraIds))
                {
                    TempData["Error"] = "No tienes acceso a esta materia.";
                    return RedirectToAction("Index");
                }

                // Soft delete
                materia.Activo = false;
                db.SaveChanges();

                TempData["Success"] = $"Materia '{materia.Nombre}' desactivada exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
            }

            return RedirectToAction("Index");
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
