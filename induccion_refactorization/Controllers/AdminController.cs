using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
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
        [RequierePermiso("GestionUsuarios", Accion.Leer)]
        public ActionResult GestionUsuarios(string search, int? rolId, bool? activo, string sortBy, string sortDir, int page = 1, int pageSize = 10)
        {
            var query = db.Usuarios.Include(u => u.Role).Include(u => u.Carreras).AsQueryable();

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

            // Datos para los modales de Nuevo Usuario / Editar Usuario.
            ViewBag.RolesList = new SelectList(db.Roles, "RolID", "Nombre");
            ViewBag.CarrerasList = db.Carreras.OrderBy(c => c.Nombre).ToList();
            ViewBag.RolesConCarrera = RolesConCarrera;

            // Datos para el modal de Permisos Individuales (excepciones por usuario
            // sobre lo que su rol permite por defecto). Se proyectan a objetos
            // simples (no las entidades de EF) para serializarlos a JSON sin
            // arrastrar propiedades de navegación/proxy.
            var idsPagina = result.Items.Select(u => u.UsuarioID).ToList();
            ViewBag.PermisosCatalogo = db.Ind_Permisos.OrderBy(p => p.PermisoID)
                .Select(p => new { p.PermisoID, p.Clave, p.Nombre })
                .ToList();
            ViewBag.RolPermisosTodos = db.Ind_RolPermisos
                .Select(rp => new { rp.RolID, rp.PermisoID, rp.PuedeLeer, rp.PuedeCrear, rp.PuedeEditar, rp.PuedeEliminar })
                .ToList();
            ViewBag.UsuarioPermisosOverrides = db.Ind_UsuarioPermisos
                .Where(up => idsPagina.Contains(up.UsuarioID))
                .Select(up => new { up.UsuarioID, up.PermisoID, up.PuedeLeer, up.PuedeCrear, up.PuedeEditar, up.PuedeEliminar })
                .ToList();
            ViewBag.UsuarioIDActual = Session["UsuarioID"];

            return View(result);
        }

        // GET: /Admin/Reportes
        [RequierePermiso("Reportes", Accion.Leer)]
        public ActionResult Reportes(int[] carreraId, int? periodoId, int[] calificadorId, bool? activo)
        {
            ViewBag.NombreCompleto = Session["NombreCompleto"];

            // Carrera y Coordinador/Maestro admiten selección múltiple (o ninguna
            // selección = "todas"/"todos"); Periodo y Estado se quedan de selección
            // única porque no tiene sentido combinar varios ahí.
            var carreraIds = carreraId ?? new int[0];
            var calificadorIds = calificadorId ?? new int[0];
            // Se evalúa aquí (no dentro de los .Where/.Count de EF) porque LINQ to
            // Entities no puede traducir "arreglo.Length" a SQL.
            bool sinFiltroCarrera = carreraIds.Length == 0;

            ViewBag.CarrerasFiltro = db.Carreras.OrderBy(c => c.Nombre).ToList();
            ViewBag.PeriodosFiltro = new SelectList(db.Periodos.OrderByDescending(p => p.FechaInicio), "PeriodoID", "Nombre", periodoId);
            ViewBag.CoordinadoresFiltro = db.Usuarios.Where(u => u.RolID == 3).OrderBy(u => u.Nombre).ToList();
            ViewBag.MaestrosFiltro = db.Usuarios.Where(u => u.RolID == 5).OrderBy(u => u.Nombre).ToList();
            ViewBag.CarreraIds = carreraIds;
            ViewBag.PeriodoId = periodoId;
            ViewBag.CalificadorIds = calificadorIds;
            ViewBag.Activo = activo;

            var model = new ReportesViewModel
            {
                TotalUsuarios = db.Usuarios.Count(),
                TotalAdministradores = db.Usuarios.Count(u => u.RolID == 1 && (!activo.HasValue || u.Activo == activo.Value)),
                TotalCoordinadores = db.Usuarios.Count(u => u.RolID == 3
                    && (!activo.HasValue || u.Activo == activo.Value)
                    && (sinFiltroCarrera || u.Carreras.Any(c => carreraIds.Contains(c.CarreraID)))),
                TotalMaestros = db.Usuarios.Count(u => u.RolID == 5
                    && (!activo.HasValue || u.Activo == activo.Value)
                    && (sinFiltroCarrera || u.Carreras.Any(c => carreraIds.Contains(c.CarreraID)))),
                TotalAspirantes = db.Usuarios.Count(u => u.RolID == 4
                    && (!activo.HasValue || u.Activo == activo.Value)
                    && (sinFiltroCarrera || u.Carreras.Any(c => carreraIds.Contains(c.CarreraID))))
            };

            var materiasQuery = db.Ind_Materias.Where(m => m.Activo);
            if (periodoId.HasValue)
            {
                materiasQuery = materiasQuery.Where(m => m.PeriodoID == periodoId.Value);
            }
            if (carreraIds.Length > 0)
            {
                materiasQuery = materiasQuery.Where(m => m.TodasLasCarreras || m.Carreras.Any(c => carreraIds.Contains(c.CarreraID)));
            }

            var materiasList = materiasQuery
                .Include(m => m.Ind_Unidades.Select(u => u.Ind_Materiales))
                .Include(m => m.Ind_Unidades.Select(u => u.Ind_Entregables))
                .Include(m => m.Ind_Unidades.Select(u => u.Ind_ProgresoAspirantes))
                .ToList();

            model.TotalMaterias = materiasList.Count;
            model.TotalUnidades = materiasList.Sum(m => m.Ind_Unidades.Count);
            model.TotalMateriales = materiasList.Sum(m => m.Ind_Unidades.Sum(u => u.Ind_Materiales.Count));
            model.TotalEntregables = materiasList.Sum(m => m.Ind_Unidades.Sum(u => u.Ind_Entregables.Count(e => e.Activo)));

            var materiaIdsScope = materiasList.Select(m => m.MateriaID).ToList();

            var submisionesQuery = db.Ind_Submisiones.Where(s => materiaIdsScope.Contains(s.Ind_Entregable.Ind_Unidad.MateriaID));
            if (calificadorIds.Length > 0)
            {
                submisionesQuery = submisionesQuery.Where(s => s.UsuarioRevisorID.HasValue && calificadorIds.Contains(s.UsuarioRevisorID.Value));
            }
            var submisiones = submisionesQuery.ToList();
            model.EntregasPendientes = submisiones.Count(s => s.Estado == "Pendiente");
            model.EntregasRevisadas = submisiones.Count(s => s.Estado == "Revisado");
            model.EntregasRechazadas = submisiones.Count(s => s.Estado == "Rechazado");
            var calificadasEntregas = submisiones.Where(s => s.Calificacion.HasValue).ToList();
            model.PromedioEntregables = calificadasEntregas.Any() ? calificadasEntregas.Average(s => s.Calificacion.Value) : (decimal?)null;

            var progresosQuery = db.Ind_ProgresoAspirante.Where(p => materiaIdsScope.Contains(p.Ind_Unidad.MateriaID));
            if (calificadorIds.Length > 0)
            {
                progresosQuery = progresosQuery.Where(p => p.UsuarioCalificadorID.HasValue && calificadorIds.Contains(p.UsuarioCalificadorID.Value));
            }
            var progresos = progresosQuery.ToList();
            model.UnidadesAsignadas = progresos.Count(p => p.Estado == "Asignado");
            model.UnidadesEntregadas = progresos.Count(p => p.Estado == "Entregado");
            model.UnidadesCalificadas = progresos.Count(p => p.Estado == "Calificado");
            var calificadasProgreso = progresos.Where(p => p.Calificacion.HasValue).ToList();
            model.PromedioUnidades = calificadasProgreso.Any() ? calificadasProgreso.Average(p => p.Calificacion.Value) : (decimal?)null;

            model.MateriasConProgreso = materiasList
                .Select(m =>
                {
                    var prog = m.Ind_Unidades.SelectMany(u => u.Ind_ProgresoAspirantes)
                        .Where(p => calificadorIds.Length == 0 || (p.UsuarioCalificadorID.HasValue && calificadorIds.Contains(p.UsuarioCalificadorID.Value)))
                        .ToList();
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
        [RequierePermiso("GestionRoles", Accion.Leer)]
        public ActionResult Permisos(int? rolId)
        {
            ViewBag.NombreCompleto = Session["NombreCompleto"];

            var roles = db.Roles.OrderBy(r => r.RolID).ToList();
            var rolSeleccionado = rolId.HasValue
                ? roles.FirstOrDefault(r => r.RolID == rolId.Value)
                : roles.FirstOrDefault();

            var permisosRol = rolSeleccionado != null
                ? db.Ind_RolPermisos.Where(rp => rp.RolID == rolSeleccionado.RolID).ToList()
                : new List<Ind_RolPermiso>();

            ViewBag.Roles = roles;
            ViewBag.RolSeleccionado = rolSeleccionado;
            ViewBag.Permisos = db.Ind_Permisos.OrderBy(p => p.PermisoID).ToList();
            ViewBag.PermisosRol = permisosRol;
            // Los 4 roles originales del sistema no se pueden desactivar (los usa
            // el código en varios lugares con su RolID hardcodeado).
            ViewBag.RolesOriginales = RolesOriginales;

            return View();
        }

        // POST: /Admin/CreateRol
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionRoles", Accion.Crear)]
        public ActionResult CreateRol(string nombre)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["Error"] = "El nombre del rol es obligatorio.";
                return RedirectToAction("Permisos");
            }

            try
            {
                var nombreLimpio = nombre.Trim();
                if (db.Roles.Any(r => r.Nombre == nombreLimpio))
                {
                    TempData["Error"] = "Ya existe un rol con este nombre.";
                    return RedirectToAction("Permisos");
                }

                var rol = new Role { Nombre = nombreLimpio, Activo = true };
                db.Roles.Add(rol);
                db.SaveChanges();

                TempData["Success"] = $"Rol '{rol.Nombre}' creado exitosamente. Ahora puedes asignarle permisos.";
                return RedirectToAction("Permisos", new { rolId = rol.RolID });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al guardar: {ex.Message}";
                return RedirectToAction("Permisos");
            }
        }

        // POST: /Admin/ToggleRol/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionRoles", Accion.Eliminar)]
        public ActionResult ToggleRol(int id)
        {
            try
            {
                var rol = db.Roles.Find(id);
                if (rol == null)
                {
                    TempData["Error"] = "Rol no encontrado.";
                    return RedirectToAction("Permisos");
                }

                if (RolesOriginales.Contains(rol.RolID))
                {
                    TempData["Error"] = "No se puede desactivar un rol original del sistema.";
                    return RedirectToAction("Permisos", new { rolId = id });
                }

                rol.Activo = !rol.Activo;
                db.SaveChanges();

                TempData["Success"] = rol.Activo
                    ? $"Rol '{rol.Nombre}' reactivado exitosamente."
                    : $"Rol '{rol.Nombre}' desactivado exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
            }

            return RedirectToAction("Permisos", new { rolId = id });
        }

        // POST: /Admin/GuardarPermisosRol
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionRoles", Accion.Editar)]
        public ActionResult GuardarPermisosRol(int rolId, List<PermisoRolInputModel> permisos)
        {
            try
            {
                var rol = db.Roles.Find(rolId);
                if (rol == null)
                {
                    TempData["Error"] = "Rol no encontrado.";
                    return RedirectToAction("Permisos");
                }

                var existentes = db.Ind_RolPermisos.Where(rp => rp.RolID == rolId).ToList();

                foreach (var item in permisos ?? new List<PermisoRolInputModel>())
                {
                    var existente = existentes.FirstOrDefault(rp => rp.PermisoID == item.PermisoID);
                    if (existente == null)
                    {
                        db.Ind_RolPermisos.Add(new Ind_RolPermiso
                        {
                            RolID = rolId,
                            PermisoID = item.PermisoID,
                            PuedeLeer = item.Leer,
                            PuedeCrear = item.Crear,
                            PuedeEditar = item.Editar,
                            PuedeEliminar = item.Eliminar
                        });
                    }
                    else
                    {
                        existente.PuedeLeer = item.Leer;
                        existente.PuedeCrear = item.Crear;
                        existente.PuedeEditar = item.Editar;
                        existente.PuedeEliminar = item.Eliminar;
                    }
                }

                db.SaveChanges();
                TempData["Success"] = $"Permisos de '{rol.Nombre}' actualizados exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al guardar: {ex.Message}";
            }

            return RedirectToAction("Permisos", new { rolId });
        }

        // POST: /Admin/GuardarPermisosUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionUsuarios", Accion.Editar)]
        public ActionResult GuardarPermisosUsuario(int usuarioId, List<PermisoUsuarioInputModel> permisos)
        {
            try
            {
                var usuario = db.Usuarios.Find(usuarioId);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("GestionUsuarios");
                }

                // Segunda barrera del lado del servidor: la UI ya oculta esta opción
                // para la cuenta del propio Admin que tiene la sesión abierta, para
                // evitar que se bloquee a sí mismo por accidente.
                if (usuarioId == (int)(Session["UsuarioID"] ?? 0))
                {
                    TempData["Error"] = "No puedes modificar tus propios permisos individuales.";
                    return RedirectToAction("GestionUsuarios");
                }

                var existentes = db.Ind_UsuarioPermisos.Where(up => up.UsuarioID == usuarioId).ToList();

                foreach (var item in permisos ?? new List<PermisoUsuarioInputModel>())
                {
                    var existente = existentes.FirstOrDefault(up => up.PermisoID == item.PermisoID);
                    bool heredaTodo = !item.Leer.HasValue && !item.Crear.HasValue && !item.Editar.HasValue && !item.Eliminar.HasValue;

                    if (existente == null)
                    {
                        if (!heredaTodo)
                        {
                            db.Ind_UsuarioPermisos.Add(new Ind_UsuarioPermiso
                            {
                                UsuarioID = usuarioId,
                                PermisoID = item.PermisoID,
                                PuedeLeer = item.Leer,
                                PuedeCrear = item.Crear,
                                PuedeEditar = item.Editar,
                                PuedeEliminar = item.Eliminar
                            });
                        }
                    }
                    else if (heredaTodo)
                    {
                        db.Ind_UsuarioPermisos.Remove(existente);
                    }
                    else
                    {
                        existente.PuedeLeer = item.Leer;
                        existente.PuedeCrear = item.Crear;
                        existente.PuedeEditar = item.Editar;
                        existente.PuedeEliminar = item.Eliminar;
                    }
                }

                db.SaveChanges();
                TempData["Success"] = $"Permisos individuales de '{usuario.NombreCompleto}' actualizados exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al guardar: {ex.Message}";
            }

            return RedirectToAction("GestionUsuarios");
        }

        // Roles a los que aplica una asignación de carrera(s): Coordinador, Aspirante, Maestro
        private static readonly int[] RolesConCarrera = { 3, 4, 5 };

        // Roles originales del sistema (su RolID está hardcodeado en varios lugares
        // del código, ej. CarreraScopeHelper): Administrador, Coordinador, Aspirante, Maestro.
        private static readonly int[] RolesOriginales = { 1, 3, 4, 5 };

        // POST: /Admin/CreateUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionUsuarios", Accion.Crear)]
        public ActionResult CreateUsuario(Usuario usuario, int[] carreraIds)
        {
            ModelState.Remove("Carreras");

            if (ModelState.IsValid)
            {
                try
                {
                    if (db.Usuarios.Any(u => u.CorreoElectronico == usuario.CorreoElectronico))
                    {
                        TempData["Error"] = "Ya existe un usuario con este correo electrónico.";
                    }
                    else
                    {
                        usuario.Activo = true;
                        usuario.FechaRegistro = DateTime.Now;
                        usuario.Contrasena = PasswordHasher.Hash(usuario.Contrasena);

                        if (RolesConCarrera.Contains(usuario.RolID) && carreraIds != null)
                        {
                            var carreras = db.Carreras.Where(c => carreraIds.Contains(c.CarreraID)).ToList();
                            foreach (var carrera in carreras)
                            {
                                usuario.Carreras.Add(carrera);
                            }
                        }

                        db.Usuarios.Add(usuario);
                        db.SaveChanges();

                        TempData["Success"] = $"Usuario '{usuario.NombreCompleto}' creado exitosamente.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al guardar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo crear el usuario. Verifica los datos.";
            }

            return RedirectToAction("GestionUsuarios");
        }

        // POST: /Admin/EditUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionUsuarios", Accion.Editar)]
        public ActionResult EditUsuario(Usuario usuario, int[] carreraIds)
        {
            // Contrasena is intentionally left blank by the form unless the admin wants to change it;
            // the [Required] attribute would otherwise reject the empty posted value.
            ModelState.Remove("Contrasena");
            ModelState.Remove("Carreras");

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = db.Usuarios.Include(u => u.Carreras).FirstOrDefault(u => u.UsuarioID == usuario.UsuarioID);
                    if (existing == null)
                    {
                        TempData["Error"] = "Usuario no encontrado.";
                        return RedirectToAction("GestionUsuarios");
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

                    existing.Carreras.Clear();
                    if (RolesConCarrera.Contains(existing.RolID) && carreraIds != null)
                    {
                        var carreras = db.Carreras.Where(c => carreraIds.Contains(c.CarreraID)).ToList();
                        foreach (var carrera in carreras)
                        {
                            existing.Carreras.Add(carrera);
                        }
                    }

                    db.SaveChanges();

                    TempData["Success"] = $"Usuario '{existing.NombreCompleto}' actualizado exitosamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al actualizar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el usuario. Verifica los datos.";
            }

            return RedirectToAction("GestionUsuarios");
        }

        // POST: /Admin/ToggleActivoUsuario/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionUsuarios", Accion.Eliminar)]
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
        [RequierePermiso("GestionPeriodos", Accion.Leer)]
        public ActionResult GestionPeriodos()
        {
            ViewBag.NombreCompleto = Session["NombreCompleto"];

            var periodos = db.Periodos
                .OrderByDescending(p => p.FechaInicio)
                .ToList();

            return View(periodos);
        }

        // POST: /Admin/CreatePeriodo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionPeriodos", Accion.Crear)]
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
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al guardar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo crear el periodo. Verifica los datos.";
            }

            return RedirectToAction("GestionPeriodos");
        }

        // POST: /Admin/EditPeriodo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionPeriodos", Accion.Editar)]
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
                        TempData["Error"] = "Periodo no encontrado.";
                        return RedirectToAction("GestionPeriodos");
                    }

                    existing.FechaInicio = periodo.FechaInicio;
                    existing.FechaFin = periodo.FechaFin;
                    existing.Activo = periodo.Activo;

                    db.SaveChanges();

                    TempData["Success"] = "Periodo actualizado exitosamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al actualizar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el periodo. Verifica los datos.";
            }

            return RedirectToAction("GestionPeriodos");
        }

        // POST: /Admin/TogglePeriodo/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionPeriodos", Accion.Eliminar)]
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
