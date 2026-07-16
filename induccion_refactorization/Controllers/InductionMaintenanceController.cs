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
    [RoleAuthorize(1, 3)] // Admin and Coordinador only
    public class InductionMaintenanceController : Controller
    {
        private CaptacionDbContext db = new CaptacionDbContext();

        // GET: /InductionMaintenance/Index
        public ActionResult Index(string search, int? carreraId, int? periodoId, string sortBy, string sortDir, int page = 1, int pageSize = 10)
        {
            var query = db.Ind_Materias
                .Include(m => m.Carreras)
                .Include(m => m.Periodo)
                .Include(m => m.Ind_Unidades)
                .Where(m => m.Activo);

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

            ViewBag.TotalMaterias = db.Ind_Materias.Count(m => m.Activo);
            ViewBag.TotalUnidades = db.Ind_Unidades.Count();
            ViewBag.TotalMateriales = db.Ind_Materiales.Count();
            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Search = search;
            ViewBag.CarreraId = carreraId;
            ViewBag.PeriodoId = periodoId;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDir = sortDir;
            ViewBag.CarrerasFiltro = new SelectList(db.Carreras, "CarreraID", "Nombre", carreraId);
            ViewBag.PeriodosFiltro = new SelectList(db.Periodos, "PeriodoID", "Nombre", periodoId);

            return View(result);
        }

        // GET: /InductionMaintenance/CreateMateria
        public ActionResult CreateMateria()
        {
            ViewBag.CarrerasList = db.Carreras.OrderBy(c => c.Nombre).ToList();
            ViewBag.PeriodoID = new SelectList(db.Periodos.Where(p => p.Activo), "PeriodoID", "Nombre");
            return View();
        }

        // POST: /InductionMaintenance/CreateMateria
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateMateria(Ind_Materia materia, int[] carreraIds, bool todasLasCarreras)
        {
            ModelState.Remove("Carreras");

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
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                }
            }

            ViewBag.CarrerasList = db.Carreras.OrderBy(c => c.Nombre).ToList();
            ViewBag.SelectedCarreraIds = carreraIds ?? new int[0];
            ViewBag.PeriodoID = new SelectList(db.Periodos.Where(p => p.Activo), "PeriodoID", "Nombre", materia.PeriodoID);
            return View(materia);
        }

        // GET: /InductionMaintenance/EditMateria/5
        public ActionResult EditMateria(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var materia = db.Ind_Materias
                .Include(m => m.Carreras)
                .FirstOrDefault(m => m.MateriaID == id);
            if (materia == null)
            {
                return HttpNotFound();
            }

            ViewBag.CarrerasList = db.Carreras.OrderBy(c => c.Nombre).ToList();
            ViewBag.SelectedCarreraIds = materia.Carreras.Select(c => c.CarreraID).ToArray();
            ViewBag.PeriodoID = new SelectList(db.Periodos.Where(p => p.Activo), "PeriodoID", "Nombre", materia.PeriodoID);
            return View(materia);
        }

        // POST: /InductionMaintenance/EditMateria/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditMateria(Ind_Materia materia, int[] carreraIds, bool todasLasCarreras)
        {
            ModelState.Remove("Carreras");

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
                        return HttpNotFound();
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
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
                }
            }

            ViewBag.CarrerasList = db.Carreras.OrderBy(c => c.Nombre).ToList();
            ViewBag.SelectedCarreraIds = carreraIds ?? new int[0];
            ViewBag.PeriodoID = new SelectList(db.Periodos.Where(p => p.Activo), "PeriodoID", "Nombre", materia.PeriodoID);
            return View(materia);
        }

        // GET: /InductionMaintenance/ManageUnidades/5
        public ActionResult ManageUnidades(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var materia = db.Ind_Materias
                .Include(m => m.Ind_Unidades.Select(u => u.Ind_Materiales))
                .Include(m => m.Ind_Unidades.Select(u => u.Ind_Entregables))
                .FirstOrDefault(m => m.MateriaID == id);

            if (materia == null)
            {
                return HttpNotFound();
            }

            ViewBag.MateriaNombre = materia.Nombre;
            return View(materia.Ind_Unidades.OrderBy(u => u.Orden).ToList());
        }

        // GET: /InductionMaintenance/CreateUnidad/5
        public ActionResult CreateUnidad(int? materiaId)
        {
            if (materiaId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var materia = db.Ind_Materias.Find(materiaId);
            if (materia == null)
            {
                return HttpNotFound();
            }

            ViewBag.MateriaNombre = materia.Nombre;
            ViewBag.MateriaID = materiaId;
            ViewBag.NextOrden = db.Ind_Unidades.Where(u => u.MateriaID == materiaId).Count() + 1;

            return View(new Ind_Unidad { MateriaID = materiaId.Value });
        }

        // POST: /InductionMaintenance/CreateUnidad
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUnidad(Ind_Unidad unidad)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Ind_Unidades.Add(unidad);
                    db.SaveChanges();

                    TempData["Success"] = $"Unidad '{unidad.Nombre}' creada exitosamente.";
                    return RedirectToAction("ManageUnidades", new { id = unidad.MateriaID });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                }
            }

            var materia = db.Ind_Materias.Find(unidad.MateriaID);
            ViewBag.MateriaNombre = materia?.Nombre ?? "Desconocida";
            ViewBag.MateriaID = unidad.MateriaID;
            return View(unidad);
        }

        // GET: /InductionMaintenance/CreateMaterial/5
        public ActionResult CreateMaterial(int? unidadId)
        {
            if (unidadId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var unidad = db.Ind_Unidades.Include(u => u.Ind_Materia).FirstOrDefault(u => u.UnidadID == unidadId);
            if (unidad == null)
            {
                return HttpNotFound();
            }

            ViewBag.UnidadNombre = unidad.Nombre;
            ViewBag.MateriaNombre = unidad.Ind_Materia?.Nombre ?? "Desconocida";
            ViewBag.UnidadID = unidadId;
            ViewBag.MateriaID = unidad.MateriaID;

            return View(new Ind_Material { UnidadID = unidadId.Value });
        }

        // POST: /InductionMaintenance/CreateMaterial
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateMaterial(Ind_Material material)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Ind_Materiales.Add(material);
                    db.SaveChanges();

                    TempData["Success"] = $"Material '{material.Nombre}' agregado exitosamente.";
                    
                    // Redirect back to ManageUnidades
                    var unidad = db.Ind_Unidades.Find(material.UnidadID);
                    return RedirectToAction("ManageUnidades", new { id = unidad.MateriaID });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                }
            }

            var unidadData = db.Ind_Unidades.Include(u => u.Ind_Materia).FirstOrDefault(u => u.UnidadID == material.UnidadID);
            ViewBag.UnidadNombre = unidadData?.Nombre ?? "Desconocida";
            ViewBag.MateriaNombre = unidadData?.Ind_Materia?.Nombre ?? "Desconocida";
            ViewBag.UnidadID = material.UnidadID;
            ViewBag.MateriaID = unidadData?.MateriaID;

            return View(material);
        }

        // GET: /InductionMaintenance/CreateEntregable/5
        public ActionResult CreateEntregable(int? unidadId)
        {
            if (unidadId == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var unidad = db.Ind_Unidades.Include(u => u.Ind_Materia).FirstOrDefault(u => u.UnidadID == unidadId);
            if (unidad == null)
            {
                return HttpNotFound();
            }

            ViewBag.UnidadNombre = unidad.Nombre;
            ViewBag.MateriaNombre = unidad.Ind_Materia?.Nombre ?? "Desconocida";
            ViewBag.UnidadID = unidadId;
            ViewBag.MateriaID = unidad.MateriaID;

            return View(new Ind_Entregable { UnidadID = unidadId.Value, PonderacionMax = 100 });
        }

        // POST: /InductionMaintenance/CreateEntregable
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateEntregable(Ind_Entregable entregable)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    entregable.Activo = true;
                    db.Ind_Entregables.Add(entregable);
                    db.SaveChanges();

                    TempData["Success"] = $"Entregable '{entregable.Titulo}' creado exitosamente.";

                    var unidadCreated = db.Ind_Unidades.Find(entregable.UnidadID);
                    return RedirectToAction("ManageUnidades", new { id = unidadCreated.MateriaID });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                }
            }

            var unidadData = db.Ind_Unidades.Include(u => u.Ind_Materia).FirstOrDefault(u => u.UnidadID == entregable.UnidadID);
            ViewBag.UnidadNombre = unidadData?.Nombre ?? "Desconocida";
            ViewBag.MateriaNombre = unidadData?.Ind_Materia?.Nombre ?? "Desconocida";
            ViewBag.UnidadID = entregable.UnidadID;
            ViewBag.MateriaID = unidadData?.MateriaID;

            return View(entregable);
        }

        // POST: /InductionMaintenance/DeleteEntregable/5
        [HttpPost]
        [ValidateAntiForgeryToken]
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

                var unidad = db.Ind_Unidades.Find(entregable.UnidadID);

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
        public ActionResult DeleteMateria(int id)
        {
            try
            {
                var materia = db.Ind_Materias.Find(id);
                if (materia == null)
                {
                    TempData["Error"] = "Materia no encontrada.";
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
        public ActionResult DeleteUnidad(int id)
        {
            try
            {
                var unidad = db.Ind_Unidades.Find(id);
                if (unidad == null)
                {
                    TempData["Error"] = "Unidad no encontrada.";
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

                var unidad = db.Ind_Unidades.Find(material.UnidadID);
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
