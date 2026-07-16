# Recursos del proyecto

Inventario de todo lo que usa el proyecto para funcionar: librerías/paquetes, y la lista completa de tablas de base de datos que toca la aplicación, confirmando cuáles quedan cubiertas por `Scripts/SistemaInduccion_SetupCompleto.sql` y cuáles deben existir de antes.

## Runtime

- **.NET Framework 4.7.2** (target del proyecto; el entorno tiene instalado 4.8, compatible)
- **ASP.NET MVC 5** / **Razor** para las vistas
- **SQL Server** — LocalDB en desarrollo (`(localdb)\MSSQLLocalDB`), SQL Server en producción

## Paquetes NuGet (`packages.config`)

| Paquete | Versión | Para qué |
|---|---|---|
| EntityFramework | 6.4.4 | ORM Code-First, acceso a datos |
| Microsoft.AspNet.Mvc | 5.2.9 | Framework MVC |
| Microsoft.AspNet.Razor | 3.2.9 | Motor de vistas |
| Microsoft.AspNet.WebPages | 3.2.9 | Soporte de Razor Pages/helpers |
| Microsoft.AspNet.Web.Optimization | 1.1.3 | Bundling/minificación de CSS y JS |
| Microsoft.Web.Infrastructure | 2.0.0 | Infraestructura compartida de ASP.NET |
| Microsoft.CodeDom.Providers.DotNetCompilerPlatform | 2.0.1 | Compilador Roslyn para vistas Razor |
| Microsoft.jQuery.Unobtrusive.Validation | 3.2.11 | Puente entre validación de datos de MVC y jQuery Validate |
| jQuery.Validation | 1.19.5 | Validación de formularios en el cliente |
| jQuery | 3.7.0 | Utilidades DOM/AJAX en el cliente |
| bootstrap | 5.2.3 | Sistema de diseño / componentes UI |
| Newtonsoft.Json | 13.0.3 | Serialización JSON |
| WebGrease | 1.6.0 | Dependencia interna de Web.Optimization |
| Modernizr | 2.8.3 | Detección de features del navegador |
| Antlr | 3.5.0.2 | Dependencia de Razor (parser) |

## Recursos vía CDN (no NuGet)

- **Font Awesome 6.4.0** — íconos, cargado desde `cdnjs.cloudflare.com` en `Views/Shared/_Layout.cshtml`

## Código propio sin dependencia externa

- **Hashing de contraseñas** (`Helpers/PasswordHasher.cs`) — PBKDF2 vía `System.Security.Cryptography.Rfc2898DeriveBytes`, incluido en .NET Framework, sin paquete adicional (no se usó BCrypt.Net ni ninguna librería de terceros).
- **Hash de archivos** (`Helpers/DocumentoHelper.cs`) — SHA-256 vía `System.Security.Cryptography.SHA256`.

## Inventario completo de tablas

Todas las tablas que la aplicación usa (mapeadas por `[Table("...")]` en `Models/`), y si `Scripts/SistemaInduccion_SetupCompleto.sql` las crea/siembra o si deben existir de antes:

| Tabla | Entidad C# | Origen | ¿La cubre el script? |
|---|---|---|---|
| `Usuarios` | `Usuario` | Esquema de captación (ya existente) | No la crea; el script **sí actualiza/inserta filas** (los 4 usuarios de prueba) |
| `Roles` | `Role` | Esquema de captación (ya existente) | No — debe tener ya los 4 roles (Administrador/Director/Coordinador/Aspirante) |
| `Aspirantes` | `Aspirante` | Esquema de captación (ya existente) | No la crea; el script reutiliza el primer Aspirante existente para el rol de prueba |
| `Carreras` | `Carrera` | Esquema de captación (ya existente) | No — solo se usa como referencia si una Materia se asigna a carreras específicas |
| `Periodos` | `Periodo` | Esquema de captación (ya existente) | No la crea; si no hay ningún periodo activo, el script **crea uno de ejemplo automáticamente** antes de sembrar materias |
| `Documentos` | `Documento` | Esquema de captación (ya existente) | No la crea; la aplicación **inserta filas** ahí cuando un aspirante sube un entregable |
| `TiposDocumentos` | `TipoDocumento` | Esquema de captación (ya existente) | No la crea; la aplicación **inserta la fila** `"Entregable de Inducción"` sola, la primera vez que se necesita |
| `EstadosDocumentos` | `EstadoDocumento` | Esquema de captación (ya existente) | No la crea; la aplicación **inserta la fila** `"Pendiente"` sola, la primera vez que se necesita |
| `Ind_Materias` | `Ind_Materia` | Módulo de inducción | **Sí** — la crea y siembra 3 materias de ejemplo |
| `Ind_MateriaCarreras` | *(solo Fluent API, sin clase dedicada)* | Módulo de inducción | **Sí** — la crea (queda vacía en el seed porque las materias de ejemplo usan `TodasLasCarreras = 1`) |
| `Ind_Unidades` | `Ind_Unidad` | Módulo de inducción | **Sí** — la crea y siembra 8 unidades de ejemplo |
| `Ind_Materiales` | `Ind_Material` | Módulo de inducción | **Sí** — la crea y siembra 5 materiales de ejemplo |
| `Ind_ProgresoAspirante` | `Ind_ProgresoAspirante` | Módulo de inducción | **Sí** — la crea y siembra progreso de ejemplo para el aspirante de pruebas |
| `Ind_Entregables` | `Ind_Entregable` | Módulo de inducción | **Sí** — la crea y siembra 1 entregable de ejemplo |
| `Ind_Submisiones` | `Ind_Submision` | Módulo de inducción | **Sí** — la crea (queda vacía en el seed; se llena cuando un aspirante sube un archivo) |

**Resumen**: de las 15 tablas que toca la aplicación, 6 son nuevas del módulo de inducción y las crea por completo `SistemaInduccion_SetupCompleto.sql`; las otras 9 pertenecen al esquema de captación que ya existía en `CaptacionDB` — el script no las crea (no le corresponde), pero sí siembra datos de prueba dentro de ellas donde hace falta (`Usuarios`, `Periodos` si no hay ninguno, y las filas de catálogo de `TiposDocumentos`/`EstadosDocumentos` que se crean solas desde la aplicación). Para que el proyecto funcione de cero, la única condición externa real es que `CaptacionDB` ya tenga su esquema de captación con al menos los 4 `Roles` cargados (`Databasenew.sql` es la referencia de ese esquema).
