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
            // esta misma vista en vez de páginas aparte).
            ViewBag.CarrerasList = CarreraScopeHelper.IsScopedRole(rolId)
                ? db.Carreras.Where(c => carreraIds.Contains(c.CarreraID)).OrderBy(c => c.Nombre).ToList()
                : db.Carreras.OrderBy(c => c.Nombre).ToList();
            ViewBag.PuedeElegirTodasLasCarreras = rolId == CarreraScopeHelper.RolAdmin;
            ViewBag.PeriodosList = new SelectList(db.Periodos.Where(p => p.Activo), "PeriodoID", "Nombre");

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
            return View(materia.Ind_Unidades.OrderBy(u => u.Orden).ToList());
        }

        // POST: /InductionMaintenance/CreateUnidad
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Crear)]
        public ActionResult CreateUnidad(Ind_Unidad unidad)
        {
            ModelState.Remove("Orden");

            var materiaPadre = db.Ind_Materias.Include(m => m.Carreras).FirstOrDefault(m => m.MateriaID == unidad.MateriaID);
            if (!CarreraScopeHelper.MateriaEnScope(materiaPadre, CurrentRolID, CurrentCarreraIds))
            {
                TempData["Error"] = "No tienes acceso a esta materia.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    unidad.Orden = db.Ind_Unidades.Count(u => u.MateriaID == unidad.MateriaID) + 1;
                    db.Ind_Unidades.Add(unidad);
                    db.SaveChanges();

                    TempData["Success"] = $"Unidad '{unidad.Nombre}' creada exitosamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al guardar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo crear la unidad. Verifica los datos.";
            }

            return RedirectToAction("ManageUnidades", new { id = unidad.MateriaID });
        }

        // POST: /InductionMaintenance/EditUnidad
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Editar)]
        public ActionResult EditUnidad(int unidadId, string nombre)
        {
            var unidad = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras).FirstOrDefault(u => u.UnidadID == unidadId);
            if (unidad == null)
            {
                TempData["Error"] = "Unidad no encontrada.";
                return RedirectToAction("Index");
            }

            if (!CarreraScopeHelper.MateriaEnScope(unidad.Ind_Materia, CurrentRolID, CurrentCarreraIds))
            {
                TempData["Error"] = "No tienes acceso a esta materia.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["Error"] = "El nombre de la unidad es obligatorio.";
                return RedirectToAction("ManageUnidades", new { id = unidad.MateriaID });
            }

            try
            {
                unidad.Nombre = nombre.Trim();
                db.SaveChanges();
                TempData["Success"] = "Unidad actualizada exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
            }

            return RedirectToAction("ManageUnidades", new { id = unidad.MateriaID });
        }

        // POST: /InductionMaintenance/ReordenarUnidades
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Editar)]
        public JsonResult ReordenarUnidades(int materiaId, int[] orderedIds)
        {
            var materia = db.Ind_Materias.Include(m => m.Carreras).FirstOrDefault(m => m.MateriaID == materiaId);
            if (!CarreraScopeHelper.MateriaEnScope(materia, CurrentRolID, CurrentCarreraIds))
            {
                return Json(new { success = false, message = "No tienes acceso a esta materia." });
            }

            try
            {
                var unidades = db.Ind_Unidades.Where(u => u.MateriaID == materiaId && orderedIds.Contains(u.UnidadID)).ToList();
                for (int i = 0; i < orderedIds.Length; i++)
                {
                    var unidad = unidades.FirstOrDefault(u => u.UnidadID == orderedIds[i]);
                    if (unidad != null)
                    {
                        unidad.Orden = i + 1;
                    }
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /InductionMaintenance/CreateMaterial
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Crear)]
        public ActionResult CreateMaterial(Ind_Material material)
        {
            ModelState.Remove("Orden");

            var unidadData = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras).FirstOrDefault(u => u.UnidadID == material.UnidadID);
            if (!CarreraScopeHelper.MateriaEnScope(unidadData?.Ind_Materia, CurrentRolID, CurrentCarreraIds))
            {
                TempData["Error"] = "No tienes acceso a esta materia.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    material.Orden = db.Ind_Materiales.Count(m => m.UnidadID == material.UnidadID) + 1;
                    db.Ind_Materiales.Add(material);
                    db.SaveChanges();

                    TempData["Success"] = $"Material '{material.Nombre}' agregado exitosamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al guardar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo agregar el material. Verifica los datos.";
            }

            return RedirectToAction("ManageUnidades", new { id = unidadData?.MateriaID });
        }

        // POST: /InductionMaintenance/EditMaterial
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Editar)]
        public ActionResult EditMaterial(int materialId, string nombre, string tipoRecurso, string rutaURL)
        {
            var material = db.Ind_Materiales.Find(materialId);
            if (material == null)
            {
                TempData["Error"] = "Material no encontrado.";
                return RedirectToAction("Index");
            }

            var unidad = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras).FirstOrDefault(u => u.UnidadID == material.UnidadID);
            if (!CarreraScopeHelper.MateriaEnScope(unidad?.Ind_Materia, CurrentRolID, CurrentCarreraIds))
            {
                TempData["Error"] = "No tienes acceso a esta materia.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(tipoRecurso) || string.IsNullOrWhiteSpace(rutaURL))
            {
                TempData["Error"] = "Todos los campos son obligatorios.";
                return RedirectToAction("ManageUnidades", new { id = unidad?.MateriaID });
            }

            try
            {
                material.Nombre = nombre.Trim();
                material.TipoRecurso = tipoRecurso.Trim();
                material.RutaURL = rutaURL.Trim();
                db.SaveChanges();
                TempData["Success"] = "Material actualizado exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
            }

            return RedirectToAction("ManageUnidades", new { id = unidad?.MateriaID });
        }

        // POST: /InductionMaintenance/ReordenarMateriales
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Editar)]
        public JsonResult ReordenarMateriales(int unidadId, int[] orderedIds)
        {
            var unidad = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras).FirstOrDefault(u => u.UnidadID == unidadId);
            if (!CarreraScopeHelper.MateriaEnScope(unidad?.Ind_Materia, CurrentRolID, CurrentCarreraIds))
            {
                return Json(new { success = false, message = "No tienes acceso a esta materia." });
            }

            try
            {
                var materiales = db.Ind_Materiales.Where(m => m.UnidadID == unidadId && orderedIds.Contains(m.MaterialID)).ToList();
                for (int i = 0; i < orderedIds.Length; i++)
                {
                    var material = materiales.FirstOrDefault(m => m.MaterialID == orderedIds[i]);
                    if (material != null)
                    {
                        material.Orden = i + 1;
                    }
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /InductionMaintenance/CreateEntregable
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Crear)]
        public ActionResult CreateEntregable(Ind_Entregable entregable)
        {
            ModelState.Remove("Orden");

            var unidadData = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras).FirstOrDefault(u => u.UnidadID == entregable.UnidadID);
            if (!CarreraScopeHelper.MateriaEnScope(unidadData?.Ind_Materia, CurrentRolID, CurrentCarreraIds))
            {
                TempData["Error"] = "No tienes acceso a esta materia.";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    entregable.Activo = true;
                    entregable.Orden = db.Ind_Entregables.Count(e => e.UnidadID == entregable.UnidadID) + 1;
                    db.Ind_Entregables.Add(entregable);
                    db.SaveChanges();

                    TempData["Success"] = $"Entregable '{entregable.Titulo}' creado exitosamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al guardar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo crear el entregable. Verifica los datos.";
            }

            return RedirectToAction("ManageUnidades", new { id = unidadData?.MateriaID });
        }

        // POST: /InductionMaintenance/EditEntregable
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Editar)]
        public ActionResult EditEntregable(int entregableId, string titulo, string instrucciones, DateTime? fechaLimite, decimal ponderacionMax)
        {
            var entregable = db.Ind_Entregables.Find(entregableId);
            if (entregable == null)
            {
                TempData["Error"] = "Entregable no encontrado.";
                return RedirectToAction("Index");
            }

            var unidad = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras).FirstOrDefault(u => u.UnidadID == entregable.UnidadID);
            if (!CarreraScopeHelper.MateriaEnScope(unidad?.Ind_Materia, CurrentRolID, CurrentCarreraIds))
            {
                TempData["Error"] = "No tienes acceso a esta materia.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(titulo))
            {
                TempData["Error"] = "El título es obligatorio.";
                return RedirectToAction("ManageUnidades", new { id = unidad?.MateriaID });
            }

            try
            {
                entregable.Titulo = titulo.Trim();
                entregable.Instrucciones = instrucciones;
                entregable.FechaLimite = fechaLimite;
                entregable.PonderacionMax = ponderacionMax;
                db.SaveChanges();
                TempData["Success"] = "Entregable actualizado exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
            }

            return RedirectToAction("ManageUnidades", new { id = unidad?.MateriaID });
        }

        // POST: /InductionMaintenance/ReordenarEntregables
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Editar)]
        public JsonResult ReordenarEntregables(int unidadId, int[] orderedIds)
        {
            var unidad = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras).FirstOrDefault(u => u.UnidadID == unidadId);
            if (!CarreraScopeHelper.MateriaEnScope(unidad?.Ind_Materia, CurrentRolID, CurrentCarreraIds))
            {
                return Json(new { success = false, message = "No tienes acceso a esta materia." });
            }

            try
            {
                var entregables = db.Ind_Entregables.Where(e => e.UnidadID == unidadId && orderedIds.Contains(e.EntregableID)).ToList();
                for (int i = 0; i < orderedIds.Length; i++)
                {
                    var entregable = entregables.FirstOrDefault(e => e.EntregableID == orderedIds[i]);
                    if (entregable != null)
                    {
                        entregable.Orden = i + 1;
                    }
                }
                db.SaveChanges();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /InductionMaintenance/DeleteEntregable/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Eliminar)]
        public ActionResult DeleteEntregable(int id)
        {
            try
            {
                var entregable = db.Ind_Entregables.Find(id);
                if (entregable == null)
                {
                    TempData["Error"] = "Entregable no encontrado.";
                    return RedirectToAction("Index");
                }

                var unidad = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras).FirstOrDefault(u => u.UnidadID == entregable.UnidadID);
                if (!CarreraScopeHelper.MateriaEnScope(unidad?.Ind_Materia, CurrentRolID, CurrentCarreraIds))
                {
                    TempData["Error"] = "No tienes acceso a esta materia.";
                    return RedirectToAction("Index");
                }

                // Soft delete
                entregable.Activo = false;
                db.SaveChanges();

                TempData["Success"] = $"Entregable '{entregable.Titulo}' desactivado exitosamente.";
                return RedirectToAction("ManageUnidades", new { id = unidad?.MateriaID });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
                return RedirectToAction("Index");
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

        // POST: /InductionMaintenance/DeleteUnidad/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Eliminar)]
        public ActionResult DeleteUnidad(int id)
        {
            try
            {
                var unidad = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras).FirstOrDefault(u => u.UnidadID == id);
                if (unidad == null)
                {
                    TempData["Error"] = "Unidad no encontrada.";
                    return RedirectToAction("Index");
                }

                if (!CarreraScopeHelper.MateriaEnScope(unidad.Ind_Materia, CurrentRolID, CurrentCarreraIds))
                {
                    TempData["Error"] = "No tienes acceso a esta materia.";
                    return RedirectToAction("Index");
                }

                var materiaId = unidad.MateriaID;

                // Hard delete (or implement soft delete if needed)
                db.Ind_Unidades.Remove(unidad);
                db.SaveChanges();

                TempData["Success"] = $"Unidad '{unidad.Nombre}' eliminada exitosamente.";
                return RedirectToAction("ManageUnidades", new { id = materiaId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // POST: /InductionMaintenance/DeleteMaterial/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionContenido", Accion.Eliminar)]
        public ActionResult DeleteMaterial(int id)
        {
            try
            {
                var material = db.Ind_Materiales.Find(id);
                if (material == null)
                {
                    TempData["Error"] = "Material no encontrado.";
                    return RedirectToAction("Index");
                }

                var unidad = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras).FirstOrDefault(u => u.UnidadID == material.UnidadID);
                if (!CarreraScopeHelper.MateriaEnScope(unidad?.Ind_Materia, CurrentRolID, CurrentCarreraIds))
                {
                    TempData["Error"] = "No tienes acceso a esta materia.";
                    return RedirectToAction("Index");
                }

                var materiaId = unidad?.MateriaID;

                db.Ind_Materiales.Remove(material);
                db.SaveChanges();

                TempData["Success"] = $"Material '{material.Nombre}' eliminado exitosamente.";
                return RedirectToAction("ManageUnidades", new { id = materiaId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
                return RedirectToAction("Index");
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
