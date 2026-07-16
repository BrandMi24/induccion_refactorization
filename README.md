# Sistema de Inducción — UTTN

Portal web de inducción/onboarding para aspirantes de la Universidad Tecnológica de Tamaulipas Norte (UTTN). Permite a Administradores y Coordinadores publicar contenido de inducción (materias, unidades, materiales y entregables), a los Aspirantes consumirlo y entregar tareas, y a Coordinadores/Directores dar seguimiento al progreso.

Este módulo se integra sobre una base de datos (`CaptacionDB`) y un esquema de captación de aspirantes ya existentes; el sistema de inducción añade sus propias tablas (prefijo `Ind_`) sin modificar el esquema original de captación.

## Índice

- [Stack técnico](#stack-técnico)
- [Roles y permisos](#roles-y-permisos)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Puesta en marcha](#puesta-en-marcha)
- [Documentación adicional](#documentación-adicional)

## Stack técnico

- **ASP.NET MVC 5** sobre **.NET Framework 4.8**
- **Entity Framework 6** (Code-First, mapeado contra una base de datos ya existente)
- **SQL Server LocalDB** (desarrollo) / SQL Server (producción)
- **Razor** para las vistas, **Bootstrap 5** + **Font Awesome 6** para la interfaz
- Autenticación por **Forms Authentication** + `Session`, con un `[RoleAuthorize]` propio que restringe cada controlador por rol

## Roles y permisos

El rol se define en `Usuarios.RolID`:

| RolID | Rol            | Puede hacer |
|-------|----------------|-------------|
| 1     | Administrador  | Panel de administración · Gestión de contenido (materias/unidades/entregables) · Gestión de usuarios (crear, editar, activar/desactivar, cambiar rol) · Gestión de periodos · Ver reportes y permisos · Editar su propio perfil |
| 2     | Director       | Panel de dirección de solo lectura · Estadísticas generales y progreso por materia · Editar su propio perfil |
| 3     | Coordinador    | Gestión de contenido (materias/unidades/entregables) · Revisar y calificar entregables subidos · Revisar y calificar unidades marcadas como entregadas · Editar su propio perfil |
| 4     | Aspirante      | Ver sus materias asignadas y su progreso · Ver el contenido de cada unidad · Marcar unidades como entregadas · Subir archivos para entregables · Editar su propio perfil |

La referencia visible dentro de la app está en `/Admin/Permisos`. La restricción real vive en cada controlador vía el atributo `[RoleAuthorize(n)]` (o `[Authorize]` simple para lo que aplica a cualquier rol autenticado, como el perfil).

## Estructura del proyecto

```
induccion_refactorization/
├── Controllers/          Un controlador por rol (Admin, Director, Coordinador, Aspirante)
│                          + Account (login/logout), Perfil, InductionMaintenance (contenido, compartido Admin/Coordinador)
├── Models/                Entidades EF Code-First (mapeadas 1:1 contra las tablas reales)
├── ViewModels/             DTOs para vistas que combinan datos de varias entidades
├── Views/                  Razor views, una carpeta por controlador + Shared (layout, partials)
├── Helpers/                Utilidades sin estado: validación de archivos, hashing de contraseñas, integración con Documentos
├── Filters/                RoleAuthorizeAttribute (autorización por rol vía Session)
├── Content/                CSS (Bootstrap + Site.css propio) y assets estáticos
├── Scripts/                Scripts SQL de referencia y de instalación (ver docs/BASE_DE_DATOS.md)
└── App_Data/Uploads/       Archivos subidos por los aspirantes (no accesible por HTTP directo)
```

## Puesta en marcha

1. **Base de datos**: la app espera una base `CaptacionDB` con el esquema de captación ya existente (ver `Scripts/Databasenew.sql` como referencia de ese esquema). Para agregar las tablas del módulo de inducción y datos de prueba, ejecuta:

   ```
   Scripts/SistemaInduccion_SetupCompleto.sql
   ```

   Es un solo script, idempotente (se puede correr varias veces sin duplicar nada) que crea todas las tablas `Ind_*`, las integra con la tabla `Documentos` existente, y siembra un usuario de prueba por rol. Ver [docs/BASE_DE_DATOS.md](docs/BASE_DE_DATOS.md) para el detalle del esquema.

2. **Cadena de conexión**: revisa `Web.config` → `connectionStrings` → `CaptacionDbContext`. Por defecto apunta a `(localdb)\MSSQLLocalDB`.

3. **Ejecutar**: abre `induccion_refactorization.sln`/`.csproj` en Visual Studio y presiona F5 (IIS Express). La app redirige directo a `/Account/Login` (no hay landing page pública).

4. **Credenciales de prueba** (después de correr el script de instalación):

   | Correo | Contraseña | Rol |
   |---|---|---|
   | admin@test.com | Password123! | Administrador |
   | director@test.com | Password123! | Director |
   | coordinador@test.com | Password123! | Coordinador |
   | aspirante@test.com | Password123! | Aspirante |

   Las contraseñas se convierten automáticamente a un hash seguro (PBKDF2) la primera vez que cada usuario inicia sesión — no es necesario hashearlas a mano.

## Documentación adicional

- [docs/ARQUITECTURA.md](docs/ARQUITECTURA.md) — flujo de cada rol, decisiones de diseño, convenciones del código, y evaluación de la arquitectura
- [docs/BASE_DE_DATOS.md](docs/BASE_DE_DATOS.md) — esquema completo del módulo de inducción y cómo se relaciona con el esquema de captación existente
- [docs/SEGURIDAD.md](docs/SEGURIDAD.md) — controles de seguridad implementados y pendientes antes de producción
- [docs/RECURSOS.md](docs/RECURSOS.md) — librerías/paquetes usados e inventario completo de tablas (qué siembra el script y qué debe existir de antes)
