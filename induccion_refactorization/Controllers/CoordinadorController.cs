using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
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

        // El nombre de usuario de un Aspirante ES su folio, igual que el resto del
        // sistema de captación: 10 dígitos rellenados con ceros a la izquierda,
        // consecutivo al último folio real (los folios reales siempre empiezan con
        // '0'; los datos de prueba sueltos que hay en la base tienen 10 dígitos
        // pero sin ese cero inicial, así que se ignoran para este cálculo). Si no
        // hay ningún folio real todavía, se empieza en 0000000001.
        private string GenerarSiguienteFolio()
        {
            var ultimoFolio = db.Usuarios
                .Where(u => u.NombreUsuario.Length == 10)
                .Select(u => u.NombreUsuario)
                .AsEnumerable()
                .Where(n => n[0] == '0' && n.All(char.IsDigit))
                .OrderByDescending(n => n)
                .FirstOrDefault();

            long siguiente = ultimoFolio != null ? long.Parse(ultimoFolio) + 1 : 1;
            return siguiente.ToString().PadLeft(10, '0');
        }

        // El nombre de usuario de un Maestro junta su primer nombre con su
        // apellido paterno (ej. "Brandon Miguel" + "Hernandez" → "brandon.hernandez"),
        // sin acentos y en minúsculas, con un sufijo numérico si ya existe otro
        // usuario con ese mismo handle.
        private string GenerarNombreUsuarioMaestro(string nombre, string apellidoPaterno)
        {
            var primerNombre = QuitarAcentos(nombre).Split(' ')[0];
            var apellido = QuitarAcentos(apellidoPaterno).Split(' ')[0];
            var baseNombre = $"{primerNombre}.{apellido}".ToLowerInvariant();
            baseNombre = new string(baseNombre.Where(c => char.IsLetterOrDigit(c) || c == '.').ToArray());
            if (string.IsNullOrWhiteSpace(baseNombre) || baseNombre == ".")
            {
                baseNombre = "maestro";
            }

            var candidato = baseNombre;
            var sufijo = 1;
            while (db.Usuarios.Any(u => u.NombreUsuario == candidato))
            {
                candidato = $"{baseNombre}{sufijo}";
                sufijo++;
            }
            return candidato;
        }

        private static string QuitarAcentos(string texto)
        {
            if (string.IsNullOrEmpty(texto)) { return texto; }
            var normalizado = texto.Normalize(System.Text.NormalizationForm.FormD);
            var chars = normalizado.Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark);
            return new string(chars.ToArray()).Normalize(System.Text.NormalizationForm.FormC);
        }

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

        // Ya no existe un "Marcar Revisado" manual: como toda unidad debe tener
        // al menos un entregable (regla aplicada en Guardar Cambios de Gestión de
        // Contenido), la unidad se marca como Revisada automáticamente en cuanto
        // se aprueban todos sus entregables (ver acción Revisar más abajo).

        // POST: /Coordinador/Revisar/5
        // Ya no existe una página GET dedicada — se revisa desde un modal dentro
        // de AspiranteDetalle (combina progreso de unidades + entregas en un
        // mismo lugar).
        // Ya no se califica con número: solo se aprueba ("Revisado") o se
        // devuelve para que el aspirante la vuelva a subir ("Rechazado"), con un
        // comentario explicando por qué en ese segundo caso.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("RevisarEntregables", Accion.Editar)]
        public ActionResult Revisar(int submisionId, string estado, string comentarioRevisor)
        {
            try
            {
                var submision = db.Ind_Submisiones
                    .Include(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia.Carreras)
                    .FirstOrDefault(s => s.SubmisionID == submisionId);
                if (submision == null)
                {
                    TempData["Error"] = "Entrega no encontrada.";
                    return RedirectToAction("MisAspirantes");
                }

                if (!CarreraScopeHelper.MateriaEnScope(submision.Ind_Entregable.Ind_Unidad.Ind_Materia, CurrentRolID, CurrentCarreraIds))
                {
                    TempData["Error"] = "No tienes acceso a esta entrega.";
                    return RedirectToAction("MisAspirantes");
                }

                if (estado != "Revisado" && estado != "Rechazado")
                {
                    TempData["Error"] = "Estado no válido.";
                    return RedirectToAction("AspiranteDetalle", new { id = submision.AspiranteID });
                }

                var fechaRevision = DateTime.Now;

                submision.Estado = estado;
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

                // Se guarda ya la revisión de la entrega antes de contar cuántos
                // entregables de la unidad están aprobados — si no, la consulta de
                // abajo seguiría viendo el estado viejo de ESTA MISMA submisión en
                // la base de datos (EF no la resuelve desde el cambio en memoria).
                db.SaveChanges();

                // Como toda unidad debe tener al menos un entregable, la unidad ya
                // no se marca como revisada a mano: en cuanto TODOS sus entregables
                // quedan aprobados, se marca sola.
                if (estado == "Revisado")
                {
                    var unidadId = submision.Ind_Entregable.UnidadID;
                    var entregableIdsUnidad = db.Ind_Entregables
                        .Where(e => e.UnidadID == unidadId && e.Activo)
                        .Select(e => e.EntregableID)
                        .ToList();
                    var revisados = db.Ind_Submisiones.Count(s =>
                        s.AspiranteID == submision.AspiranteID &&
                        entregableIdsUnidad.Contains(s.EntregableID) &&
                        s.Estado == "Revisado");

                    if (revisados == entregableIdsUnidad.Count)
                    {
                        var progreso = db.Ind_ProgresoAspirante
                            .FirstOrDefault(p => p.AspiranteID == submision.AspiranteID && p.UnidadID == unidadId);
                        if (progreso != null)
                        {
                            progreso.Estado = "Revisado";
                            progreso.FechaRevision = fechaRevision;
                            progreso.UsuarioCalificadorID = Session["UsuarioID"] as int?;
                            db.SaveChanges();
                        }
                    }
                }

                TempData["Success"] = estado == "Revisado" ? "Entrega aprobada exitosamente." : "Entrega devuelta para que se vuelva a subir.";
                return RedirectToAction("AspiranteDetalle", new { id = submision.AspiranteID });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al revisar: {ex.Message}";
                return RedirectToAction("MisAspirantes");
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
                return RedirectToAction("MisAspirantes");
            }

            if (!CarreraScopeHelper.MateriaEnScope(submision.Ind_Entregable.Ind_Unidad.Ind_Materia, CurrentRolID, CurrentCarreraIds))
            {
                TempData["Error"] = "No tienes acceso a este archivo.";
                return RedirectToAction("MisAspirantes");
            }

            var rutaArchivo = submision.Documento?.RutaAlmacenamiento ?? submision.RutaArchivo;
            var nombreDescarga = submision.Documento?.NombreOriginal ?? Path.GetFileName(rutaArchivo);
            var tipoMime = submision.Documento?.TipoMIME ?? "application/octet-stream";

            var fullPath = Server.MapPath(rutaArchivo);
            if (!System.IO.File.Exists(fullPath))
            {
                TempData["Error"] = "El archivo ya no está disponible en el servidor.";
                return RedirectToAction("MisAspirantes");
            }

            return File(fullPath, tipoMime, nombreDescarga);
        }

        // GET: /Coordinador/MisAspirantes
        [RequierePermiso("MisAspirantes", Accion.Leer)]
        public ActionResult MisAspirantes(string search, string sortBy, string sortDir, int page = 1, int pageSize = 10)
        {
            var rolId = CurrentRolID;
            var carreraIds = CurrentCarreraIds;

            var aspirantesQuery = db.Usuarios
                .Where(u => u.RolID == CarreraScopeHelper.RolAspirante && u.Carreras.Any(c => carreraIds.Contains(c.CarreraID)));

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                aspirantesQuery = aspirantesQuery.Where(a =>
                    a.Nombre.ToLower().Contains(term) ||
                    a.ApellidoPaterno.ToLower().Contains(term) ||
                    a.NombreUsuario.ToLower().Contains(term));
            }

            var aspirantesList = aspirantesQuery.ToList();
            var aspiranteIds = aspirantesList.Select(a => a.UsuarioID).ToList();

            var progresosPorAspirante = db.Ind_ProgresoAspirante
                .Include(p => p.Ind_Unidad.Ind_Materia)
                .Where(p => aspiranteIds.Contains(p.AspiranteID))
                .ToList()
                .GroupBy(p => p.AspiranteID)
                .ToDictionary(g => g.Key, g => g.ToList());

            var pendientesPorAspirante = db.Ind_Submisiones
                .Where(s => aspiranteIds.Contains(s.AspiranteID) && s.Estado == "Pendiente")
                .GroupBy(s => s.AspiranteID)
                .ToDictionary(g => g.Key, g => g.Count());

            var resumenes = aspirantesList.Select(a =>
            {
                var progresos = progresosPorAspirante.TryGetValue(a.UsuarioID, out var list) ? list : new List<Ind_ProgresoAspirante>();

                var materias = progresos
                    .GroupBy(p => p.Ind_Unidad.Ind_Materia)
                    .Select(g => new MateriaSemaforoViewModel
                    {
                        MateriaID = g.Key.MateriaID,
                        Nombre = g.Key.Nombre,
                        TotalUnidades = g.Count(),
                        UnidadesRevisadas = g.Count(p => p.Estado == "Revisado")
                    })
                    .OrderBy(m => m.Nombre)
                    .ToList();

                return new AspiranteResumenViewModel
                {
                    Aspirante = a,
                    Materias = materias,
                    EntregasPendientes = pendientesPorAspirante.TryGetValue(a.UsuarioID, out var cnt) ? cnt : 0,
                    TotalUnidadesAsignadas = progresos.Count,
                    UnidadesCompletadas = progresos.Count(p => p.Estado == "Revisado")
                };
            }).ToList();

            // Los que tienen entregas pendientes por revisar siempre aparecen
            // primero; el resto (incluyendo los que ya terminaron) queda después.
            var pendientesPrimero = resumenes.OrderBy(r => r.EntregasPendientes > 0 ? 0 : 1);

            bool descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            IOrderedEnumerable<AspiranteResumenViewModel> ordenado;
            switch (sortBy)
            {
                case "Pendientes":
                    ordenado = descending ? pendientesPrimero.ThenByDescending(r => r.EntregasPendientes) : pendientesPrimero.ThenBy(r => r.EntregasPendientes);
                    break;
                case "Progreso":
                    ordenado = descending ? pendientesPrimero.ThenByDescending(r => r.PorcentajeProgreso) : pendientesPrimero.ThenBy(r => r.PorcentajeProgreso);
                    break;
                case "Aspirante":
                    ordenado = descending ? pendientesPrimero.ThenByDescending(r => r.Aspirante.NombreCompleto) : pendientesPrimero.ThenBy(r => r.Aspirante.NombreCompleto);
                    break;
                default:
                    ordenado = pendientesPrimero.ThenBy(r => r.Aspirante.NombreCompleto);
                    break;
            }

            var result = PagedResult<AspiranteResumenViewModel>.Create(ordenado.AsQueryable(), page, pageSize);

            // Estadísticas agregadas sobre todo el alcance (no solo la página actual)
            var todosProgresos = progresosPorAspirante.Values.SelectMany(p => p).ToList();
            ViewBag.TotalAspirantes = aspirantesList.Count;
            ViewBag.PromedioAvance = todosProgresos.Any()
                ? (int)((todosProgresos.Count(p => p.Estado == "Revisado") * 100.0) / todosProgresos.Count)
                : 0;
            ViewBag.EntregasPendientesCalificar = pendientesPorAspirante.Values.Sum();
            ViewBag.EntregasDevueltas = db.Ind_Submisiones.Count(s => aspiranteIds.Contains(s.AspiranteID) && s.Estado == "Rechazado");

            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDir = sortDir;
            PermissionHelper.AsignarFlagsVista(ViewBag, db, CurrentUsuarioID, rolId, "MisAspirantes");

            return View(result);
        }

        // GET: /Coordinador/DescargarPlantillaAspirantes
        [RequierePermiso("MisAspirantes", Accion.Crear)]
        public ActionResult DescargarPlantillaAspirantes()
        {
            var carrerasScope = db.Carreras.Where(c => c.Activo && CurrentCarreraIds.Contains(c.CarreraID))
                .OrderBy(c => c.Nombre).Select(c => c.Nombre).ToList();
            var bytes = ExcelImportHelper.GenerarPlantilla("Aspirantes", carrerasScope);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PlantillaAspirantes.xlsx");
        }

        // GET: /Coordinador/DescargarCredencialesGeneradas
        // Sirve el Excel de credenciales generado por la carga masiva más reciente
        // (aspirantes o maestros) de esta sesión — ver ImportarAspirantesMasivo /
        // ImportarMaestrosMasivo, que lo dejan listo en Session justo antes de
        // redirigir de vuelta a la página.
        public ActionResult DescargarCredencialesGeneradas()
        {
            var bytes = Session["CredencialesGeneradas"] as byte[];
            var nombreArchivo = Session["CredencialesGeneradasNombreArchivo"] as string;
            if (bytes == null || string.IsNullOrEmpty(nombreArchivo))
            {
                TempData["Error"] = "No hay credenciales generadas para descargar.";
                return RedirectToAction("Index", "Coordinador");
            }

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", nombreArchivo);
        }

        // POST: /Coordinador/ImportarAspirantesMasivo
        // Los aspirantes creados aquí NO tocan la tabla de captación dbo.Aspirantes
        // (Folio/CURP/etc.) — se insertan exactamente igual que
        // AdminController.CreateUsuario inserta hoy un usuario con rol Aspirante:
        // solo una fila en Usuarios + carrera(s) asignada(s). El Área se autoasigna
        // de la primera carrera válida de la fila.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequierePermiso("MisAspirantes", Accion.Crear)]
        public ActionResult ImportarAspirantesMasivo(HttpPostedFileBase archivo)
        {
            if (archivo == null || archivo.ContentLength == 0)
            {
                TempData["Error"] = "Selecciona un archivo Excel (.xlsx) para importar.";
                return RedirectToAction("MisAspirantes");
            }

            List<FilaUsuarioImportado> filas;
            try
            {
                filas = ExcelImportHelper.LeerFilas(archivo.InputStream);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"No se pudo leer el archivo: {ex.Message}";
                return RedirectToAction("MisAspirantes");
            }

            if (!filas.Any())
            {
                TempData["Error"] = "El archivo no tiene filas para importar.";
                return RedirectToAction("MisAspirantes");
            }

            // Solo se pueden asignar carreras válidas y dentro del alcance de quien
            // importa (sus propias carreras a cargo), tal como pidió el usuario.
            var carreraIds = CurrentCarreraIds;
            var carrerasScope = db.Carreras.Where(c => c.Activo && carreraIds.Contains(c.CarreraID)).ToList();

            var creados = new List<string>();
            var errores = new List<string>();
            var credenciales = new List<CredencialGenerada>();

            foreach (var fila in filas)
            {
                var prefijo = $"Fila {fila.NumeroFila}";

                if (string.IsNullOrWhiteSpace(fila.Nombre) || string.IsNullOrWhiteSpace(fila.ApellidoPaterno) ||
                    string.IsNullOrWhiteSpace(fila.CorreoElectronico))
                {
                    errores.Add($"{prefijo}: faltan datos obligatorios (Nombre, ApellidoPaterno, CorreoElectronico).");
                    continue;
                }

                if (!fila.NombresCarreras.Any())
                {
                    errores.Add($"{prefijo}: no se indicó ninguna carrera.");
                    continue;
                }

                if (db.Usuarios.Any(u => u.CorreoElectronico == fila.CorreoElectronico))
                {
                    errores.Add($"{prefijo}: ya existe un usuario con ese correo.");
                    continue;
                }

                var carrerasFila = carrerasScope
                    .Where(c => fila.NombresCarreras.Any(n => string.Equals(n, c.Nombre, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (carrerasFila.Count != fila.NombresCarreras.Count)
                {
                    var noValidas = fila.NombresCarreras.Where(n => !carrerasFila.Any(c => string.Equals(n, c.Nombre, StringComparison.OrdinalIgnoreCase)));
                    errores.Add($"{prefijo}: carrera(s) no válida(s) o fuera de tu alcance: {string.Join(", ", noValidas)}.");
                    continue;
                }

                // El Área ahora es propiedad directa de la Carrera (la relación se
                // invirtió: la carrera pertenece a un área, no al revés).
                var primeraCarrera = carrerasFila.First();
                if (!primeraCarrera.AreaID.HasValue)
                {
                    errores.Add($"{prefijo}: la carrera '{primeraCarrera.Nombre}' todavía no tiene un Área asignada. Pide al Administrador que la asigne desde Gestión de Carreras.");
                    continue;
                }

                try
                {
                    var contrasenaTemporal = ExcelImportHelper.GenerarContrasenaTemporal();
                    var usuario = new Usuario
                    {
                        Nombre = fila.Nombre,
                        ApellidoPaterno = fila.ApellidoPaterno,
                        ApellidoMaterno = fila.ApellidoMaterno,
                        NombreUsuario = GenerarSiguienteFolio(),
                        CorreoElectronico = fila.CorreoElectronico,
                        Contrasena = PasswordHasher.Hash(contrasenaTemporal),
                        RolID = 4,
                        Activo = true,
                        FechaRegistro = DateTime.Now,
                        Ind_AreaID = primeraCarrera.AreaID
                    };
                    foreach (var carrera in carrerasFila)
                    {
                        usuario.Carreras.Add(carrera);
                    }
                    db.Usuarios.Add(usuario);
                    db.SaveChanges();

                    creados.Add($"{usuario.NombreUsuario} ({usuario.NombreCompleto}) — contraseña temporal: {contrasenaTemporal}");
                    credenciales.Add(new CredencialGenerada
                    {
                        NombreCompleto = usuario.NombreCompleto,
                        Usuario = usuario.NombreUsuario,
                        Correo = usuario.CorreoElectronico,
                        ContrasenaTemporal = contrasenaTemporal
                    });
                }
                catch (Exception ex)
                {
                    errores.Add($"{prefijo}: error al guardar — {ex.Message}");
                }
            }

            TempData["ImportCreados"] = creados;
            TempData["ImportErrores"] = errores;
            if (creados.Any())
            {
                TempData["Success"] = $"Se crearon {creados.Count} de {filas.Count} aspirantes. Revisa el detalle abajo para las contraseñas temporales.";
                Session["CredencialesGeneradas"] = ExcelImportHelper.GenerarExcelCredenciales("Credenciales", credenciales);
                Session["CredencialesGeneradasNombreArchivo"] = "CredencialesAspirantes.xlsx";
                TempData["CredencialesListas"] = true;
            }
            if (errores.Any())
            {
                TempData["Error"] = $"{errores.Count} fila(s) con errores. Revisa el detalle abajo.";
            }

            return RedirectToAction("MisAspirantes");
        }

        // GET: /Coordinador/AspiranteDetalle/5
        [RequierePermiso("MisAspirantes", Accion.Leer)]
        public ActionResult AspiranteDetalle(int id, string progresoSearch, int progresoPage = 1, int progresoPageSize = 10, string submisionSearch = null, int submisionPage = 1, int submisionPageSize = 10)
        {
            var carreraIds = CurrentCarreraIds;

            var aspirante = db.Usuarios
                .Include(u => u.Carreras)
                .FirstOrDefault(u => u.UsuarioID == id && u.RolID == CarreraScopeHelper.RolAspirante);

            if (aspirante == null || !aspirante.Carreras.Any(c => carreraIds.Contains(c.CarreraID)))
            {
                TempData["Error"] = "No tienes acceso a este aspirante.";
                return RedirectToAction("MisAspirantes");
            }

            var progresosQuery = db.Ind_ProgresoAspirante
                .Include(p => p.Ind_Unidad.Ind_Materia)
                .Where(p => p.AspiranteID == id);

            if (!string.IsNullOrWhiteSpace(progresoSearch))
            {
                progresosQuery = progresosQuery.Where(p =>
                    p.Ind_Unidad.Ind_Materia.Nombre.Contains(progresoSearch) ||
                    p.Ind_Unidad.Nombre.Contains(progresoSearch) ||
                    p.Estado.Contains(progresoSearch));
            }

            progresosQuery = progresosQuery.OrderBy(p => p.Ind_Unidad.Ind_Materia.Nombre).ThenBy(p => p.Ind_Unidad.Orden);

            var submisionesQuery = db.Ind_Submisiones
                .Include(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia)
                .Where(s => s.AspiranteID == id);

            if (!string.IsNullOrWhiteSpace(submisionSearch))
            {
                submisionesQuery = submisionesQuery.Where(s =>
                    s.Ind_Entregable.Ind_Unidad.Ind_Materia.Nombre.Contains(submisionSearch) ||
                    s.Ind_Entregable.Ind_Unidad.Nombre.Contains(submisionSearch) ||
                    s.Ind_Entregable.Titulo.Contains(submisionSearch) ||
                    s.Estado.Contains(submisionSearch));
            }

            submisionesQuery = submisionesQuery.OrderByDescending(s => s.FechaEnvio);

            var progresosResult = PagedResult<Ind_ProgresoAspirante>.Create(progresosQuery, progresoPage, progresoPageSize);

            ViewBag.ProgresosResult = progresosResult;
            ViewBag.SubmisionesResult = PagedResult<Ind_Submision>.Create(submisionesQuery, submisionPage, submisionPageSize);
            ViewBag.ProgresoSearch = progresoSearch;
            ViewBag.SubmisionSearch = submisionSearch;
            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.PuedeRevisarEntregas = PermissionHelper.TieneAcceso(db, CurrentUsuarioID, CurrentRolID, "RevisarEntregables", Accion.Editar);
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
            var aspirantesScope = db.Usuarios
                .Where(a => a.RolID == CarreraScopeHelper.RolAspirante && (!scoped || a.Carreras.Any(c => carreraIds.Contains(c.CarreraID))))
                .OrderBy(a => a.Nombre)
                .ToList();

            var yaAsignados = new HashSet<int>(db.Ind_ProgresoAspirante
                .Where(p => p.UnidadID == unidadId)
                .Select(p => p.AspiranteID));

            var aspirantes = aspirantesScope.Select(a => new
            {
                id = a.UsuarioID,
                nombre = a.NombreCompleto,
                folio = a.NombreUsuario,
                yaAsignado = yaAsignados.Contains(a.UsuarioID)
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
        public ActionResult AsignarUnidad(int[] unidadIds, string modo, int[] aspiranteIds, bool reasignar = false)
        {
            var rolId = CurrentRolID;
            var carreraIds = CurrentCarreraIds;

            unidadIds = (unidadIds ?? new int[0]).Distinct().ToArray();
            var unidades = db.Ind_Unidades.Include(u => u.Ind_Materia.Carreras)
                .Where(u => unidadIds.Contains(u.UnidadID)).ToList();

            if (unidades.Count == 0 || unidades.Any(u => !CarreraScopeHelper.MateriaEnScope(u.Ind_Materia, rolId, carreraIds)))
            {
                TempData["Error"] = "No tienes acceso a una o más de las unidades seleccionadas.";
                return RedirectToAction("Index", "InductionMaintenance");
            }
            // Todas las unidades seleccionadas pertenecen a la misma materia (el
            // modal solo ofrece las unidades de la materia que se está gestionando).
            var materiaId = unidades.First().MateriaID;

            // IsScopedRole se evalúa aquí (no dentro del Where) porque EF6/LINQ to
            // Entities no puede traducir una llamada a un método C# arbitrario a SQL.
            bool scoped = CarreraScopeHelper.IsScopedRole(rolId);
            var aspirantesScopeIds = db.Usuarios
                .Where(a => a.RolID == CarreraScopeHelper.RolAspirante && (!scoped || a.Carreras.Any(c => carreraIds.Contains(c.CarreraID))))
                .Select(a => a.UsuarioID)
                .ToList();

            // Sin importar lo que se haya mandado en el POST, el destino siempre se
            // restringe a aspirantes dentro del propio alcance de carrera.
            List<int> targetIds = string.Equals(modo, "todos", StringComparison.OrdinalIgnoreCase)
                ? aspirantesScopeIds
                : (aspiranteIds ?? new int[0]).Where(aid => aspirantesScopeIds.Contains(aid)).ToList();

            if (targetIds.Count == 0)
            {
                TempData["Error"] = "No se seleccionó ningún aspirante.";
                return RedirectToAction("ManageUnidades", "InductionMaintenance", new { id = materiaId });
            }

            try
            {
                var existentes = db.Ind_ProgresoAspirante
                    .Where(p => unidadIds.Contains(p.UnidadID) && targetIds.Contains(p.AspiranteID))
                    .ToList();
                var existentesPorClave = existentes.ToDictionary(p => (p.UnidadID, p.AspiranteID));

                int nuevos = 0, reasignados = 0;

                foreach (var unidadId in unidadIds)
                {
                    foreach (var aspiranteId in targetIds)
                    {
                        if (existentesPorClave.TryGetValue((unidadId, aspiranteId), out var progreso))
                        {
                            if (reasignar)
                            {
                                progreso.Estado = "Asignado";
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
                }

                db.SaveChanges();

                TempData["Success"] = $"Unidad(es) asignada(s): {nuevos} nuevo(s), {reasignados} reasignado(s).";
                return RedirectToAction("ManageUnidades", "InductionMaintenance", new { id = materiaId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error al asignar: {ex.Message}";
                return RedirectToAction("ManageUnidades", "InductionMaintenance", new { id = materiaId });
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
                    EntregasCalificadasPorEl = db.Ind_Submisiones.Count(s => s.UsuarioRevisorID == m.UsuarioID)
                };
            }).ToList();

            bool descending = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
            switch (sortBy)
            {
                case "Materias":
                    resumenes = (descending ? resumenes.OrderByDescending(r => r.TotalMateriasCompartidas) : resumenes.OrderBy(r => r.TotalMateriasCompartidas)).ToList();
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
            ViewBag.TotalEntregasCalificadas = resumenes.Sum(r => r.EntregasCalificadasPorEl);

            ViewBag.NombreCompleto = Session["NombreCompleto"];
            ViewBag.Search = search;
            ViewBag.SortBy = sortBy;
            ViewBag.SortDir = sortDir;
            PermissionHelper.AsignarFlagsVista(ViewBag, db, CurrentUsuarioID, CurrentRolID, "MisMaestros");

            return View(result);
        }

        // GET: /Coordinador/DescargarPlantillaMaestros
        [RoleAuthorize(CarreraScopeHelper.RolCoordinador)]
        [RequierePermiso("MisMaestros", Accion.Crear)]
        public ActionResult DescargarPlantillaMaestros()
        {
            var carrerasScope = db.Carreras.Where(c => c.Activo && CurrentCarreraIds.Contains(c.CarreraID))
                .OrderBy(c => c.Nombre).Select(c => c.Nombre).ToList();
            var bytes = ExcelImportHelper.GenerarPlantilla("Maestros", carrerasScope);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "PlantillaMaestros.xlsx");
        }

        // POST: /Coordinador/ImportarMaestrosMasivo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize(CarreraScopeHelper.RolCoordinador)]
        [RequierePermiso("MisMaestros", Accion.Crear)]
        public ActionResult ImportarMaestrosMasivo(HttpPostedFileBase archivo)
        {
            if (archivo == null || archivo.ContentLength == 0)
            {
                TempData["Error"] = "Selecciona un archivo Excel (.xlsx) para importar.";
                return RedirectToAction("MisMaestros");
            }

            List<FilaUsuarioImportado> filas;
            try
            {
                filas = ExcelImportHelper.LeerFilas(archivo.InputStream);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"No se pudo leer el archivo: {ex.Message}";
                return RedirectToAction("MisMaestros");
            }

            if (!filas.Any())
            {
                TempData["Error"] = "El archivo no tiene filas para importar.";
                return RedirectToAction("MisMaestros");
            }

            var carreraIds = CurrentCarreraIds;
            var carrerasScope = db.Carreras.Where(c => c.Activo && carreraIds.Contains(c.CarreraID)).ToList();

            var creados = new List<string>();
            var errores = new List<string>();
            var credenciales = new List<CredencialGenerada>();

            foreach (var fila in filas)
            {
                var prefijo = $"Fila {fila.NumeroFila}";

                if (string.IsNullOrWhiteSpace(fila.Nombre) || string.IsNullOrWhiteSpace(fila.ApellidoPaterno) ||
                    string.IsNullOrWhiteSpace(fila.CorreoElectronico))
                {
                    errores.Add($"{prefijo}: faltan datos obligatorios (Nombre, ApellidoPaterno, CorreoElectronico).");
                    continue;
                }

                if (!fila.NombresCarreras.Any())
                {
                    errores.Add($"{prefijo}: no se indicó ninguna carrera.");
                    continue;
                }

                if (db.Usuarios.Any(u => u.CorreoElectronico == fila.CorreoElectronico))
                {
                    errores.Add($"{prefijo}: ya existe un usuario con ese correo.");
                    continue;
                }

                var carrerasFila = carrerasScope
                    .Where(c => fila.NombresCarreras.Any(n => string.Equals(n, c.Nombre, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                if (carrerasFila.Count != fila.NombresCarreras.Count)
                {
                    var noValidas = fila.NombresCarreras.Where(n => !carrerasFila.Any(c => string.Equals(n, c.Nombre, StringComparison.OrdinalIgnoreCase)));
                    errores.Add($"{prefijo}: carrera(s) no válida(s) o fuera de tu alcance: {string.Join(", ", noValidas)}.");
                    continue;
                }

                try
                {
                    var contrasenaTemporal = ExcelImportHelper.GenerarContrasenaTemporal();
                    var usuario = new Usuario
                    {
                        Nombre = fila.Nombre,
                        ApellidoPaterno = fila.ApellidoPaterno,
                        ApellidoMaterno = fila.ApellidoMaterno,
                        NombreUsuario = GenerarNombreUsuarioMaestro(fila.Nombre, fila.ApellidoPaterno),
                        CorreoElectronico = fila.CorreoElectronico,
                        Contrasena = PasswordHasher.Hash(contrasenaTemporal),
                        RolID = CarreraScopeHelper.RolMaestro,
                        Activo = true,
                        FechaRegistro = DateTime.Now
                    };
                    foreach (var carrera in carrerasFila)
                    {
                        usuario.Carreras.Add(carrera);
                    }
                    db.Usuarios.Add(usuario);
                    db.SaveChanges();

                    creados.Add($"{usuario.NombreUsuario} ({usuario.NombreCompleto}) — contraseña temporal: {contrasenaTemporal}");
                    credenciales.Add(new CredencialGenerada
                    {
                        NombreCompleto = usuario.NombreCompleto,
                        Usuario = usuario.NombreUsuario,
                        Correo = usuario.CorreoElectronico,
                        ContrasenaTemporal = contrasenaTemporal
                    });
                }
                catch (Exception ex)
                {
                    errores.Add($"{prefijo}: error al guardar — {ex.Message}");
                }
            }

            TempData["ImportCreados"] = creados;
            TempData["ImportErrores"] = errores;
            if (creados.Any())
            {
                TempData["Success"] = $"Se crearon {creados.Count} de {filas.Count} maestros. Revisa el detalle abajo para las contraseñas temporales.";
                Session["CredencialesGeneradas"] = ExcelImportHelper.GenerarExcelCredenciales("Credenciales", credenciales);
                Session["CredencialesGeneradasNombreArchivo"] = "CredencialesMaestros.xlsx";
                TempData["CredencialesListas"] = true;
            }
            if (errores.Any())
            {
                TempData["Error"] = $"{errores.Count} fila(s) con errores. Revisa el detalle abajo.";
            }

            return RedirectToAction("MisMaestros");
        }

        // GET: /Coordinador/MaestroDetalle/5
        [RoleAuthorize(CarreraScopeHelper.RolCoordinador)]
        [RequierePermiso("MisMaestros", Accion.Leer)]
        public ActionResult MaestroDetalle(int id, string materiaSearch, int materiaPage = 1, int materiaPageSize = 10, string entregaSearch = null, int entregaPage = 1, int entregaPageSize = 10)
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
                    CarreraScopeHelper.RolMaestro, carrerasCompartidas);

            if (!string.IsNullOrWhiteSpace(materiaSearch))
            {
                materiasQuery = materiasQuery.Where(m => m.Nombre.Contains(materiaSearch));
            }

            materiasQuery = materiasQuery.OrderBy(m => m.Nombre);

            var entregasQuery = db.Ind_Submisiones
                .Include(s => s.AspiranteUsuario)
                .Include(s => s.Ind_Entregable.Ind_Unidad.Ind_Materia)
                .Where(s => s.UsuarioRevisorID == id);

            if (!string.IsNullOrWhiteSpace(entregaSearch))
            {
                entregasQuery = entregasQuery.Where(s =>
                    s.AspiranteUsuario.Nombre.Contains(entregaSearch) ||
                    s.AspiranteUsuario.ApellidoPaterno.Contains(entregaSearch) ||
                    s.AspiranteUsuario.ApellidoMaterno.Contains(entregaSearch) ||
                    s.Ind_Entregable.Ind_Unidad.Ind_Materia.Nombre.Contains(entregaSearch) ||
                    s.Ind_Entregable.Ind_Unidad.Nombre.Contains(entregaSearch) ||
                    s.Ind_Entregable.Titulo.Contains(entregaSearch));
            }

            entregasQuery = entregasQuery.OrderByDescending(s => s.SubmisionID);

            ViewBag.MateriasResult = PagedResult<Ind_Materia>.Create(materiasQuery, materiaPage, materiaPageSize);
            ViewBag.EntregasResult = PagedResult<Ind_Submision>.Create(entregasQuery, entregaPage, entregaPageSize);
            ViewBag.MateriaSearch = materiaSearch;
            ViewBag.EntregaSearch = entregaSearch;
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
