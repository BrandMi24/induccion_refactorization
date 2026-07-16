# Esquema de base de datos

Todo vive en una sola base de datos, **CaptacionDB**. Se distinguen dos partes:

- **Esquema de captación** (ya existente, no se modifica): `Usuarios`, `Roles`, `Aspirantes`, `Carreras`, `Periodos`, `Documentos`, `TiposDocumentos`, `EstadosDocumentos`, y otras tablas del proceso de admisión. Referencia completa exportada en `Scripts/Databasenew.sql`.
- **Módulo de inducción** (`Ind_*`, agregado por este proyecto): se instala/actualiza con el script único `Scripts/SistemaInduccion_SetupCompleto.sql`.

## Tablas del módulo de inducción

```
Ind_Materias
 ├─ MateriaID          PK
 ├─ PeriodoID          FK -> Periodos
 ├─ Nombre, Descripcion
 ├─ Activo             (soft delete)
 └─ TodasLasCarreras   bit — si es 1, la materia es visible para todas las
                        carreras y se ignora Ind_MateriaCarreras

Ind_MateriaCarreras (tabla puente, muchos a muchos)
 ├─ MateriaID  FK -> Ind_Materias (ON DELETE CASCADE)
 └─ CarreraID  FK -> Carreras

Ind_Unidades
 ├─ UnidadID   PK
 ├─ MateriaID  FK -> Ind_Materias
 ├─ Nombre
 └─ Orden      int — orden de despliegue dentro de la materia

Ind_Materiales (recursos de estudio)
 ├─ MaterialID   PK
 ├─ UnidadID     FK -> Ind_Unidades
 ├─ Nombre
 ├─ TipoRecurso  nvarchar(50) — "PDF" | "Video" | "Enlace" | ...
 └─ RutaURL      URL externa del recurso

Ind_ProgresoAspirante (unidades sin entregable — solo "marcar como hecho")
 ├─ ProgresoID            PK
 ├─ AspiranteID           FK -> Aspirantes
 ├─ UnidadID              FK -> Ind_Unidades
 ├─ Estado                "Asignado" | "Entregado" | "Calificado"
 ├─ Calificacion          decimal(5,2) NULL
 ├─ FechaAsignacion, FechaEnvio
 ├─ UsuarioCalificadorID  FK -> Usuarios NULL
 └─ ComentariosEvaluador

Ind_Entregables (unidades que requieren subir un archivo)
 ├─ EntregableID   PK
 ├─ UnidadID       FK -> Ind_Unidades
 ├─ Titulo, Instrucciones
 ├─ FechaLimite    datetime NULL
 ├─ PonderacionMax decimal(5,2)
 └─ Activo         (soft delete)

Ind_Submisiones (archivos subidos por el aspirante)
 ├─ SubmisionID        PK
 ├─ AspiranteID        FK -> Aspirantes
 ├─ EntregableID       FK -> Ind_Entregables
 ├─ RutaArchivo        ruta física (legado/fallback, ver abajo)
 ├─ DocumentoID        FK -> Documentos NULL — fuente de verdad de metadata del archivo
 ├─ FechaEnvio
 ├─ Estado             CHECK: "Pendiente" | "Revisado" | "Rechazado"
 ├─ Calificacion, ComentarioRevisor
 └─ UsuarioRevisorID, FechaRevision
```

## Integración con `Documentos`

`Documentos` es una tabla que ya existía en `CaptacionDB` para el proceso de captación (actas, CURP, comprobantes, etc.), con metadata mucho más rica que un simple path: nombre original, extensión, tipo MIME, tamaño en bytes, ruta de almacenamiento, hash SHA-256, número de versión, versión actual, y quién/cuándo lo subió.

Cuando un aspirante sube un entregable de inducción (`AspiranteController.UploadEntregable`):

1. Se guarda el archivo físico en `App_Data/Uploads/{AspiranteID}/{EntregableID}/` (esta carpeta **no es accesible por HTTP** — `App_Data` está protegido por convención de ASP.NET).
2. Se calcula el hash SHA-256 del contenido (`Helpers/DocumentoHelper.ComputeSha256Hash`).
3. Se crea una fila en `Documentos`, usando (o creando si no existe todavía) un `TipoDocumento` = *"Entregable de Inducción"* y un `EstadoDocumento` = *"Pendiente"* — estos se resuelven en tiempo de ejecución (`DocumentoHelper.GetOrCreateTipoDocumento/EstadoDocumento`), no requieren datos semilla.
4. Si es un reenvío, el `Documento` anterior se marca `VersionActual = 0` y el nuevo hereda `NumeroVersion + 1`.
5. `Ind_Submisiones.DocumentoID` apunta a esa fila.

Las descargas (`AspiranteController.DownloadSubmission`, `CoordinadorController.DownloadSubmission`) usan `Documento.RutaAlmacenamiento` y `Documento.NombreOriginal` cuando existen, y caen de vuelta a `Ind_Submisiones.RutaArchivo` para submisiones antiguas que no tengan un `DocumentoID` asociado (columna que se conserva como *legado*, no se volvió a usar activamente para nuevas subidas más allá de mantener compatibilidad).

## Instalación / actualización

Un solo script hace todo: `Scripts/SistemaInduccion_SetupCompleto.sql`.

- **Parte 1 — Esquema**: crea (si no existen) todas las tablas `Ind_*` de arriba, con sus llaves foráneas, valores por defecto y el `CHECK` de `Ind_Submisiones.Estado`. Es seguro correrlo varias veces: cada `CREATE TABLE`/`ALTER TABLE` está protegido con `IF NOT EXISTS`.
- **Parte 2 — Datos de prueba**: limpia y vuelve a insertar un usuario de cada rol + materias/unidades/materiales/progreso de ejemplo. Esta parte **sí se re-ejecuta completa cada vez** (borra lo anterior primero), a diferencia de la Parte 1.

Este script sustituye a los antiguos `Phase6_Entregables_Submisiones.sql`, `Phase7_DocumentosIntegration.sql`, `Phase8_MultiCarreraMaterias.sql` y `SeedInductionData.sql` (ya no existen en el repo). Si tu base de datos ya tenía esas fases aplicadas por separado, el script sigue siendo seguro de correr: reconoce los mismos nombres de tabla/constraint y no duplica nada.

`Database.sql` y `Databasenew.sql` son exportaciones completas del esquema de captación tal cual existía en producción; se conservan como referencia, no como scripts de instalación.
