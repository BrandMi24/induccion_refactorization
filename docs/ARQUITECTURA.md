# Arquitectura y flujos

## Autenticación y enrutamiento por rol

- El login (`AccountController.Login`) valida contra `Usuarios.CorreoElectronico` + `Contrasena`, crea un `FormsAuthenticationTicket` y guarda en `Session`: `UsuarioID`, `RolID`, `NombreCompleto`, `Email` (y para Aspirantes también `AspiranteID`, `Matricula`, `Folio`).
- Tras el login, `RedirectByRole` manda a cada quien a su panel: `Admin/Index`, `Director/Index`, `Coordinador/Index` o `Aspirante/Index`, según `RolID` (1/2/3/4).
- No hay página pública de inicio: `HomeController.Index` redirige directo a `/Account/Login`.
- Cada controlador de rol está decorado con `[RoleAuthorize(n)]` (filtro propio en `Filters/RoleAuthorizeAttribute.cs`, lee `Session["RolID"]`). `PerfilController` usa `[Authorize]` normal porque aplica a cualquier rol autenticado.
- `Usuarios.UltimoAcceso` se actualiza en cada login exitoso.

## Flujo del Aspirante

1. `Aspirante/Index` — dashboard con sus materias asignadas (vía `Ind_ProgresoAspirante`) y progreso general.
2. `Aspirante/MateriaDetails/{id}` — detalle de una materia: sus unidades, materiales de cada unidad, y entregables si los hay.
3. Dos formas de completar una unidad, según si tiene entregable asociado o no:
   - **Sin entregable** — `MarcarEntregado`: el aspirante simplemente marca la unidad como `Entregado` (`Ind_ProgresoAspirante.Estado`); el Coordinador la califica después.
   - **Con entregable** — `UploadEntregable`: el aspirante sube un archivo (PDF/DOCX/XLSX/JPG/PNG, máx. 10 MB, validado en `Helpers/FileUploadValidator.cs`). Esto crea/actualiza una fila en `Ind_Submisiones` y, en paralelo, una fila en la tabla `Documentos` ya existente en `CaptacionDB` (ver docs/BASE_DE_DATOS.md) con metadata completa (hash SHA-256, tamaño, tipo MIME, versión). Reenviar un archivo incrementa la versión y reinicia el estado a `Pendiente`.
4. `DownloadSubmission` — descarga su propio archivo entregado (valida que la submisión le pertenezca).

## Flujo del Coordinador

- `Coordinador/Index` — resumen: entregas pendientes, revisadas, rechazadas, materias activas, progreso pendiente de calificar.
- `Coordinador/RevisarEntregas` — cola de archivos subidos con `Estado = Pendiente`, con búsqueda, filtro por materia, columnas ordenables y paginado.
- `Coordinador/Calificar` — aprobar (`Revisado` + calificación) o rechazar (`Rechazado`) una entrega.
- `Coordinador/RevisarProgreso` — cola de unidades marcadas como `Entregado` (sin archivo), mismo tratamiento de búsqueda/filtro/orden/paginado.
- `Coordinador/CalificarProgreso` — asignar calificación y comentarios a una unidad.
- El Coordinador también tiene acceso a `InductionMaintenance` (gestión de contenido), compartido con Admin.

## Gestión de contenido (Admin + Coordinador) — `InductionMaintenanceController`

- CRUD de **Materias** (`Ind_Materias`): nombre, descripción, periodo, y a qué carreras aplica.
  - Una materia puede asignarse a **una o varias carreras** (tabla puente `Ind_MateriaCarreras`) o marcarse como **visible para todas las carreras** (`Ind_Materias.TodasLasCarreras = 1`), lo que omite la selección individual.
- CRUD de **Unidades** dentro de una materia (`Ind_Unidades`, con `Orden` para el orden de despliegue).
- Dentro de cada unidad: **Materiales** de estudio (`Ind_Materiales` — PDF/Video/Enlace con URL externa) y **Entregables** (`Ind_Entregables` — tarea que requiere subir un archivo, con fecha límite y ponderación).
- Los borrados son *soft delete* (`Activo = 0`) para Materias y Entregables; Unidades y Materiales se eliminan físicamente.

## Gestión de usuarios y periodos (solo Admin) — `AdminController`

- `GestionUsuarios` — crear/editar usuarios, cambiar rol, activar/desactivar. Búsqueda, filtro por rol/estado, orden y paginado.
- `GestionPeriodos` — CRUD de periodos académicos (`Periodos`: fecha inicio/fin, activo). Los periodos que crea el Admin aquí son los que aparecen en el selector de periodo al crear una Materia.
- `Reportes` — estadísticas agregadas (usuarios por rol, entregas por estado, progreso por materia).
- `Permisos` — página de referencia (no editable) con la matriz de permisos por rol.

## Flujo del Director

- Solo lectura: `Director/Index` muestra estadísticas generales (aspirantes, materias, entregables) y el estado de las entregas/progreso, sin acciones de edición.

## Convenciones del código

- **Paginado/orden/filtrado**: patrón reutilizado en todas las listas administrativas (`GestionUsuarios`, `InductionMaintenance/Index`, `RevisarEntregas`, `RevisarProgreso`) vía `ViewModels/PagedResult.cs`, `ViewModels/SortableHeaderViewModel.cs` + partials `Views/Shared/_Pager.cshtml` y `_SortableHeader.cshtml`.
- **Mensajes al usuario**: `TempData["Success"]`/`TempData["Error"]`, renderizados por `Views/Shared/_Alerts.cshtml`.
- **Razor**: una vez dentro de un bloque de código ya abierto (`@if`, `@foreach`, `@using`), las sentencias de C# simples no necesitan `@`; pero en cuanto aparece contenido de markup como hijo directo de una etiqueta, sí se necesita `@` para volver a código. Si ves un error de "Parser Error" o código C# renderizado como texto literal en una vista, es casi siempre este patrón.

## Evaluación de arquitectura

Lo sólido:

- Estructura MVC5 convencional (Controllers/Models/Views/ViewModels/Helpers/Filters), fácil de navegar, sin capas exóticas.
- Un controlador por rol en vez de un controlador gigante con `if` de rol adentro — mantiene la superficie de cada rol pequeña y acotada.
- `[RoleAuthorize]` centraliza la decisión de "quién puede entrar aquí" en un solo filtro, en vez de repetir el chequeo en cada acción.
- `WillCascadeOnDelete(false)` explícito en todas las relaciones — decisión deliberada y correcta al estar montado sobre una base de datos de producción real: nada se borra en cascada por accidente.
- El patrón de paginado/orden/filtrado (`PagedResult<T>` + `SortableHeaderViewModel` + `_Pager`/`_SortableHeader`) ya es consistente en todas las pantallas administrativas.

Lo que vale la pena tener en el radar (nada urgente, son *trade-offs*, no errores):

1. **No hay capa de servicio/repositorio** — los controladores hablan directo con `CaptacionDbContext` y traen lógica de negocio real adentro (ej. `AspiranteController.UploadEntregable` hace I/O de archivos, hashing y versionado de `Documento` directo en la acción). Funciona bien al tamaño actual; si el proyecto crece, esa lógica se vuelve difícil de reutilizar.
2. **`ViewBag` y `ViewModel` conviven sin un criterio único** — algunas pantallas pasan todo por un ViewModel fuerte, otras usan `ViewBag` suelto para el mismo tipo de dato (estado de búsqueda/orden/página), incluso dentro de la misma pantalla. No es un bug, es inconsistencia de estilo.
3. **Sin `async`/`await`** — todas las consultas a la base de datos son síncronas. No es un problema al tamaño actual (un solo plantel, uso interno), pero es lo primero que tocaría si la concurrencia creciera mucho.
4. **Convención de borrado inconsistente** — Materias y Entregables usan borrado suave (`Activo = 0`), pero Unidades y Materiales se borran físicamente. Esto significa que borrar una Unidad sí elimina en cascada su historial de Materiales/Entregables, mientras que "borrar" una Materia solo la oculta. Conviene decidir una sola convención.
5. **Sin logging** — las excepciones se muestran al usuario (con `ex.Message`) pero no se registran en ningún lado. Ya está anotado en `SEGURIDAD.md` como pendiente de producción, pero es tan arquitectónico como de seguridad: sin esto, depurar en producción es difícil sin volver a prender `customErrors=Off` para todos.
6. **Sin inyección de dependencias** — `new CaptacionDbContext()` se instancia directo en cada controlador. Normal para MVC5 sin contenedor de DI, pero significa que sustituir el contexto (para pruebas, por ejemplo) requeriría tocar cada controlador.

En resumen: es una arquitectura convencional y apropiada para el tamaño real del proyecto (portal interno de un solo plantel), con decisiones deliberadas y correctas donde más importaba (no-cascada, autorización por rol). Lo primero que yo atacaría antes de que el proyecto crezca más: agregar algún tipo de logging, y unificar el criterio de borrado.
