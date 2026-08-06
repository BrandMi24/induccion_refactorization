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

            // Datos para los modales de Nuevo Usuario / Editar Usuario. Solo se
            // ofrecen carreras activas para asignar (igual que Periodos), aunque un
            // usuario ya asignado a una carrera desactivada conserva esa asignación.
            ViewBag.RolesList = new SelectList(db.Roles, "RolID", "Nombre");
            ViewBag.CarrerasList = db.Carreras.Where(c => c.Activo).OrderBy(c => c.Nombre).ToList();
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
            PermissionHelper.AsignarFlagsVista(ViewBag, db, (int)Session["UsuarioID"], 1, "GestionUsuarios");
            ViewBag.PuedeVerRoles = PermissionHelper.TieneAcceso(db, (int)Session["UsuarioID"], 1, "GestionRoles", Accion.Leer);

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

            var progresosQuery = db.Ind_ProgresoAspirante.Where(p => materiaIdsScope.Contains(p.Ind_Unidad.MateriaID));
            if (calificadorIds.Length > 0)
            {
                progresosQuery = progresosQuery.Where(p => p.UsuarioCalificadorID.HasValue && calificadorIds.Contains(p.UsuarioCalificadorID.Value));
            }
            var progresos = progresosQuery.ToList();
            model.UnidadesAsignadas = progresos.Count(p => p.Estado == "Asignado");
            model.UnidadesEntregadas = progresos.Count(p => p.Estado == "Entregado");
            model.UnidadesCalificadas = progresos.Count(p => p.Estado == "Revisado");

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
                        Completados = prog.Count(p => p.Estado == "Revisado")
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
            PermissionHelper.AsignarFlagsVista(ViewBag, db, (int)Session["UsuarioID"], 1, "GestionRoles");

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

        // POST: /Admin/DeleteRol/5
        // Borrado permanente del rol (a diferencia de ToggleRol, que solo lo
        // desactiva). Solo se permite si ya no tiene usuarios asignados — si los
        // tiene, hay que reasignarlos primero o usar "Desactivar Rol" en su lugar.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionRoles", Accion.Eliminar)]
        public ActionResult DeleteRol(int id)
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
                    TempData["Error"] = "No se puede eliminar un rol original del sistema.";
                    return RedirectToAction("Permisos", new { rolId = id });
                }

                if (db.Usuarios.Any(u => u.RolID == id))
                {
                    TempData["Error"] = $"No se puede eliminar el rol '{rol.Nombre}': todavía tiene usuarios asignados. Reasígnalos a otro rol primero, o usa \"Desactivar Rol\" para dejar de usarlo sin eliminarlo.";
                    return RedirectToAction("Permisos", new { rolId = id });
                }

                db.Ind_RolPermisos.RemoveRange(db.Ind_RolPermisos.Where(rp => rp.RolID == id));
                db.Roles.Remove(rol);
                db.SaveChanges();

                TempData["Success"] = $"Rol '{rol.Nombre}' eliminado exitosamente.";
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar: este rol tiene información relacionada en el sistema.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
            }

            return RedirectToAction("Permisos");
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
        // Incluye, en el mismo formulario/POST, los permisos individuales del
        // usuario (excepciones sobre lo que su rol permite por defecto) — antes
        // vivían en una acción/modal aparte (GuardarPermisosUsuario), pero se
        // fusionaron aquí para que todo lo de "gestionar a esta persona" (datos,
        // rol, carrera(s) a cargo y permisos) se edite y se guarde desde un
        // mismo lugar.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionUsuarios", Accion.Editar)]
        public ActionResult EditUsuario(Usuario usuario, int[] carreraIds, List<PermisoUsuarioInputModel> permisos)
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

                    // Permisos individuales: la UI ya oculta esta pestaña para la
                    // propia cuenta del Admin en sesión (para que no se bloquee a
                    // sí mismo por accidente); esto es la segunda barrera del lado
                    // del servidor.
                    bool esPropiaCuenta = usuario.UsuarioID == (int)(Session["UsuarioID"] ?? 0);
                    if (!esPropiaCuenta && permisos != null)
                    {
                        var existentesPermisos = db.Ind_UsuarioPermisos.Where(up => up.UsuarioID == usuario.UsuarioID).ToList();

                        foreach (var item in permisos)
                        {
                            var existentePermiso = existentesPermisos.FirstOrDefault(up => up.PermisoID == item.PermisoID);
                            bool heredaTodo = !item.Leer.HasValue && !item.Crear.HasValue && !item.Editar.HasValue && !item.Eliminar.HasValue;

                            if (existentePermiso == null)
                            {
                                if (!heredaTodo)
                                {
                                    db.Ind_UsuarioPermisos.Add(new Ind_UsuarioPermiso
                                    {
                                        UsuarioID = usuario.UsuarioID,
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
                                db.Ind_UsuarioPermisos.Remove(existentePermiso);
                            }
                            else
                            {
                                existentePermiso.PuedeLeer = item.Leer;
                                existentePermiso.PuedeCrear = item.Crear;
                                existentePermiso.PuedeEditar = item.Editar;
                                existentePermiso.PuedeEliminar = item.Eliminar;
                            }
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

        // POST: /Admin/DeleteUsuario/5
        // Borrado permanente (a diferencia de ToggleActivoUsuario, que solo
        // desactiva). Solo se permite cuando el usuario no tiene historial
        // asociado (documentos, calificaciones que puso, o progreso propio como
        // aspirante) — si lo tiene, se le pide usar "Desactivar" para
        // no perder ese historial. El try/catch de DbUpdateException es una red
        // de seguridad extra por si hay alguna otra relación no contemplada aquí.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionUsuarios", Accion.Eliminar)]
        public ActionResult DeleteUsuario(int id)
        {
            try
            {
                var usuario = db.Usuarios.Find(id);
                if (usuario == null)
                {
                    TempData["Error"] = "Usuario no encontrado.";
                    return RedirectToAction("GestionUsuarios");
                }

                if (id == (int)(Session["UsuarioID"] ?? 0))
                {
                    TempData["Error"] = "No puedes eliminar tu propia cuenta.";
                    return RedirectToAction("GestionUsuarios");
                }

                bool tieneHistorial =
                    db.Documentos.Any(d => d.UsuarioID == id) ||
                    db.Ind_ProgresoAspirante.Any(p => p.UsuarioCalificadorID == id || p.AspiranteID == id) ||
                    db.Ind_Submisiones.Any(s => s.UsuarioRevisorID == id || s.AspiranteID == id);

                if (tieneHistorial)
                {
                    TempData["Error"] = $"No se puede eliminar a '{usuario.NombreCompleto}': tiene historial asociado en el sistema (documentos, calificaciones puestas o progreso como aspirante). Usa \"Desactivar\" en su lugar para conservar ese historial.";
                    return RedirectToAction("GestionUsuarios");
                }

                db.Ind_UsuarioPermisos.RemoveRange(db.Ind_UsuarioPermisos.Where(up => up.UsuarioID == id));
                db.Usuarios.Remove(usuario);
                db.SaveChanges();

                TempData["Success"] = $"Usuario '{usuario.NombreCompleto}' eliminado permanentemente.";
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar: este usuario tiene información relacionada en el sistema. Usa \"Desactivar\" en su lugar.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
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

            PermissionHelper.AsignarFlagsVista(ViewBag, db, (int)Session["UsuarioID"], 1, "GestionPeriodos");

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

        // GET: /Admin/GestionCarreras
        // El Área es ahora el catálogo "padre": una carrera pertenece a un Área
        // (no al revés), y no se puede crear una carrera si todavía no existe
        // ningún Área en el sistema.
        [RequierePermiso("GestionCarreras", Accion.Leer)]
        public ActionResult GestionCarreras(string search, int? tipoCarreraId, int? filtroAreaId, bool? activo, string sortBy, string sortDir, int page = 1, int pageSize = 10,
            string areaSearch = null, int areaPage = 1, int areaPageSize = 10)
        {
            var query = db.Carreras.Include(c => c.TipoCarrera).Include(c => c.Area).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(c =>
                    c.Nombre.ToLower().Contains(term) ||
                    (c.Nomenclatura != null && c.Nomenclatura.ToLower().Contains(term)));
            }

            if (tipoCarreraId.HasValue)
            {
                query = query.Where(c => c.TipoCarreraID == tipoCarreraId.Value);
            }

            if (filtroAreaId.HasValue)
            {
                query = query.Where(c => c.AreaID == filtroAreaId.Value);
            }

            if (activo.HasValue)
            {
                query = query.Where(c => c.Activo == activo.Value);
            }

            bool descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            switch (sortBy)
            {
                case "Nombre":
                    query = descending ? query.OrderByDescending(c => c.Nombre) : query.OrderBy(c => c.Nombre);
                    break;
                case "Tipo":
                    query = descending ? query.OrderByDescending(c => c.TipoCarrera.Nombre) : query.OrderBy(c => c.TipoCarrera.Nombre);
                    break;
                case "Area":
                    query = descending ? query.OrderByDescending(c => c.Area.Nombre) : query.OrderBy(c => c.Area.Nombre);
                    break;
                case "Estado":
                    query = descending ? query.OrderByDescending(c => c.Activo) : query.OrderBy(c => c.Activo);
                    break;
                default:
                    query = query.OrderBy(c => c.Nombre);
                    break;
            }

            var result = PagedResult<Carrera>.Create(query, page, pageSize);

            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Search = search;
            ViewBag.TipoCarreraId = tipoCarreraId;
            ViewBag.FiltroAreaId = filtroAreaId;
            ViewBag.Activo = activo;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDir = sortDir;
            ViewBag.TiposCarreraFiltro = new SelectList(db.TiposCarreras.OrderBy(t => t.Nombre), "TipoCarreraID", "Nombre", tipoCarreraId);
            ViewBag.AreasFiltro = new SelectList(db.Ind_Areas.OrderBy(a => a.Nombre), "AreaID", "Nombre", filtroAreaId);

            // Datos para los modales de Nueva Carrera / Editar Carrera.
            ViewBag.TiposCarreraList = new SelectList(db.TiposCarreras.OrderBy(t => t.Nombre), "TipoCarreraID", "Nombre");
            ViewBag.AreasList = new SelectList(db.Ind_Areas.Where(a => a.Activo).OrderBy(a => a.Nombre), "AreaID", "Nombre");
            ViewBag.PuedeCrearCarreras = db.Ind_Areas.Any();

            // Pestaña "Áreas": lista propia, paginada y con búsqueda por nombre.
            var areasQuery = db.Ind_Areas.AsQueryable();
            if (!string.IsNullOrWhiteSpace(areaSearch))
            {
                var areaTerm = areaSearch.Trim().ToLower();
                areasQuery = areasQuery.Where(a => a.Nombre.ToLower().Contains(areaTerm));
            }
            areasQuery = areasQuery.OrderBy(a => a.Nombre);
            ViewBag.AreasResult = PagedResult<Ind_Area>.Create(areasQuery, areaPage, areaPageSize);
            ViewBag.AreaSearch = areaSearch;

            // Cuántas carreras tiene cada área (para mostrarlo en la tabla de Áreas).
            ViewBag.CarrerasPorArea = db.Carreras
                .Where(c => c.AreaID.HasValue)
                .GroupBy(c => c.AreaID.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            PermissionHelper.AsignarFlagsVista(ViewBag, db, (int)Session["UsuarioID"], 1, "GestionCarreras");

            return View(result);
        }

        // POST: /Admin/CreateCarrera
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionCarreras", Accion.Crear)]
        public ActionResult CreateCarrera(Carrera carrera)
        {
            ModelState.Remove("Ind_Materias");
            ModelState.Remove("Usuarios");
            ModelState.Remove("TipoCarrera");
            ModelState.Remove("Area");

            if (!db.Ind_Areas.Any())
            {
                TempData["Error"] = "No se puede crear una carrera: primero debes crear al menos un Área.";
                return RedirectToAction("GestionCarreras", new { tab = "carreras" });
            }

            if (!carrera.AreaID.HasValue)
            {
                ModelState.AddModelError("", "Selecciona un Área.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (db.Carreras.Any(c => c.Nombre == carrera.Nombre))
                    {
                        TempData["Error"] = "Ya existe una carrera con este nombre.";
                    }
                    else
                    {
                        carrera.Activo = true;
                        db.Carreras.Add(carrera);
                        db.SaveChanges();

                        TempData["Success"] = $"Carrera '{carrera.Nombre}' creada exitosamente.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al guardar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo crear la carrera. Verifica los datos.";
            }

            return RedirectToAction("GestionCarreras", new { tab = "carreras" });
        }

        // POST: /Admin/EditCarrera
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionCarreras", Accion.Editar)]
        public ActionResult EditCarrera(Carrera carrera)
        {
            ModelState.Remove("Ind_Materias");
            ModelState.Remove("Usuarios");
            ModelState.Remove("TipoCarrera");
            ModelState.Remove("Area");

            if (!carrera.AreaID.HasValue)
            {
                ModelState.AddModelError("", "Selecciona un Área.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = db.Carreras.Find(carrera.CarreraID);
                    if (existing == null)
                    {
                        TempData["Error"] = "Carrera no encontrada.";
                        return RedirectToAction("GestionCarreras", new { tab = "carreras" });
                    }

                    if (db.Carreras.Any(c => c.Nombre == carrera.Nombre && c.CarreraID != carrera.CarreraID))
                    {
                        TempData["Error"] = "Ya existe otra carrera con este nombre.";
                        return RedirectToAction("GestionCarreras", new { tab = "carreras" });
                    }

                    existing.Nombre = carrera.Nombre;
                    existing.Nomenclatura = carrera.Nomenclatura;
                    existing.TipoCarreraID = carrera.TipoCarreraID;
                    existing.AreaID = carrera.AreaID;

                    db.SaveChanges();

                    TempData["Success"] = $"Carrera '{existing.Nombre}' actualizada exitosamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al actualizar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar la carrera. Verifica los datos.";
            }

            return RedirectToAction("GestionCarreras", new { tab = "carreras" });
        }

        // POST: /Admin/ToggleActivoCarrera/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionCarreras", Accion.Eliminar)]
        public ActionResult ToggleActivoCarrera(int id)
        {
            try
            {
                var carrera = db.Carreras.Find(id);
                if (carrera == null)
                {
                    TempData["Error"] = "Carrera no encontrada.";
                    return RedirectToAction("GestionCarreras", new { tab = "carreras" });
                }

                carrera.Activo = !carrera.Activo;
                db.SaveChanges();

                TempData["Success"] = carrera.Activo
                    ? $"Carrera '{carrera.Nombre}' reactivada exitosamente."
                    : $"Carrera '{carrera.Nombre}' desactivada exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
            }

            return RedirectToAction("GestionCarreras", new { tab = "carreras" });
        }

        // POST: /Admin/DeleteCarrera/5
        // Borrado permanente. Solo se permite si la carrera ya no está en uso
        // (usuarios o materias que la tengan asignada) — si lo está, hay que
        // reasignar primero o usar "Desactivar" para dejar de ofrecerla sin
        // perder ese historial. El try/catch de DbUpdateException es una red de
        // seguridad extra por si el resto del sistema de captación (fuera del
        // módulo de inducción) también la está usando en alguna otra tabla.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionCarreras", Accion.Eliminar)]
        public ActionResult DeleteCarrera(int id)
        {
            try
            {
                var carrera = db.Carreras.Find(id);
                if (carrera == null)
                {
                    TempData["Error"] = "Carrera no encontrada.";
                    return RedirectToAction("GestionCarreras", new { tab = "carreras" });
                }

                bool enUso =
                    db.Usuarios.Any(u => u.Carreras.Any(c => c.CarreraID == id)) ||
                    db.Ind_Materias.Any(m => m.Carreras.Any(c => c.CarreraID == id));

                if (enUso)
                {
                    TempData["Error"] = $"No se puede eliminar la carrera '{carrera.Nombre}': todavía está en uso (usuarios o materias la tienen asignada). Reasígnalos primero, o usa \"Desactivar\" para dejar de ofrecerla sin eliminarla.";
                    return RedirectToAction("GestionCarreras", new { tab = "carreras" });
                }

                db.Carreras.Remove(carrera);
                db.SaveChanges();

                TempData["Success"] = $"Carrera '{carrera.Nombre}' eliminada exitosamente.";
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar: esta carrera tiene información relacionada en el sistema. Usa \"Desactivar\" en su lugar.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
            }

            return RedirectToAction("GestionCarreras", new { tab = "carreras" });
        }

        // POST: /Admin/CreateArea
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionCarreras", Accion.Crear)]
        public ActionResult CreateArea(Ind_Area area)
        {
            ModelState.Remove("Usuarios");
            ModelState.Remove("Carreras");

            if (ModelState.IsValid)
            {
                try
                {
                    if (db.Ind_Areas.Any(a => a.Nombre == area.Nombre))
                    {
                        TempData["Error"] = "Ya existe un área con este nombre.";
                    }
                    else
                    {
                        area.Activo = true;
                        db.Ind_Areas.Add(area);
                        db.SaveChanges();

                        TempData["Success"] = $"Área '{area.Nombre}' creada exitosamente.";
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al guardar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo crear el área. Verifica los datos.";
            }

            return RedirectToAction("GestionCarreras", new { tab = "areas" });
        }

        // POST: /Admin/EditArea
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionCarreras", Accion.Editar)]
        public ActionResult EditArea(Ind_Area area)
        {
            ModelState.Remove("Usuarios");
            ModelState.Remove("Carreras");

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = db.Ind_Areas.Find(area.AreaID);
                    if (existing == null)
                    {
                        TempData["Error"] = "Área no encontrada.";
                        return RedirectToAction("GestionCarreras", new { tab = "areas" });
                    }

                    if (db.Ind_Areas.Any(a => a.Nombre == area.Nombre && a.AreaID != area.AreaID))
                    {
                        TempData["Error"] = "Ya existe otra área con este nombre.";
                        return RedirectToAction("GestionCarreras", new { tab = "areas" });
                    }

                    existing.Nombre = area.Nombre;

                    db.SaveChanges();

                    TempData["Success"] = $"Área '{existing.Nombre}' actualizada exitosamente.";
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error al actualizar: {ex.Message}";
                }
            }
            else
            {
                TempData["Error"] = "No se pudo actualizar el área. Verifica los datos.";
            }

            return RedirectToAction("GestionCarreras", new { tab = "areas" });
        }

        // POST: /Admin/ToggleActivoArea/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionCarreras", Accion.Eliminar)]
        public ActionResult ToggleActivoArea(int id)
        {
            var area = db.Ind_Areas.Find(id);
            if (area == null)
            {
                TempData["Error"] = "Área no encontrada.";
                return RedirectToAction("GestionCarreras", new { tab = "areas" });
            }

            try
            {
                area.Activo = !area.Activo;
                db.SaveChanges();

                TempData["Success"] = area.Activo
                    ? $"Área '{area.Nombre}' reactivada exitosamente."
                    : $"Área '{area.Nombre}' desactivada exitosamente.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al actualizar: {ex.Message}";
            }

            return RedirectToAction("GestionCarreras", new { tab = "areas" });
        }

        // POST: /Admin/DeleteArea/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("GestionCarreras", Accion.Eliminar)]
        public ActionResult DeleteArea(int id)
        {
            var area = db.Ind_Areas.Find(id);
            if (area == null)
            {
                TempData["Error"] = "Área no encontrada.";
                return RedirectToAction("GestionCarreras", new { tab = "areas" });
            }

            try
            {
                bool enUso =
                    db.Usuarios.Any(u => u.Ind_AreaID == id) ||
                    db.Carreras.Any(c => c.AreaID == id);

                if (enUso)
                {
                    TempData["Error"] = $"No se puede eliminar el área '{area.Nombre}': todavía tiene usuarios o carreras asignadas. Usa \"Desactivar\" en su lugar.";
                    return RedirectToAction("GestionCarreras", new { tab = "areas" });
                }

                db.Ind_Areas.Remove(area);
                db.SaveChanges();

                TempData["Success"] = $"Área '{area.Nombre}' eliminada exitosamente.";
            }
            catch (System.Data.Entity.Infrastructure.DbUpdateException)
            {
                TempData["Error"] = "No se puede eliminar: esta área tiene información relacionada en el sistema. Usa \"Desactivar\" en su lugar.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al eliminar: {ex.Message}";
            }

            return RedirectToAction("GestionCarreras", new { tab = "areas" });
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
