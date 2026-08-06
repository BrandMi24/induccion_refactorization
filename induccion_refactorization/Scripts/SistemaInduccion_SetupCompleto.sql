-- ============================================================================
-- SISTEMA DE INDUCCIÓN - SCRIPT ÚNICO DE INSTALACIÓN
-- ============================================================================
-- Este script reemplaza y consolida todo lo que antes estaba repartido en:
--   Phase6_Entregables_Submisiones.sql, Phase7_DocumentosIntegration.sql,
--   Phase8_MultiCarreraMaterias.sql y SeedInductionData.sql
-- (esos cuatro archivos ya no son necesarios y pueden borrarse).
--
-- Qué hace:
--   PARTE 1 - Esquema: crea las tablas del módulo de inducción (Ind_Materias,
--             Ind_Unidades, Ind_Materiales, Ind_ProgresoAspirante,
--             Ind_Entregables, Ind_Submisiones, Ind_MateriaCarreras,
--             Ind_UsuarioCarreras) y su integración con la tabla Documentos
--             ya existente en la base. También agrega el rol "Maestro" a la
--             tabla Roles (el rol Director ya no tiene acceso al sistema;
--             ver AccountController.cs).
--   PARTE 2 - Datos de prueba: crea un usuario de cada rol vigente (Admin,
--             Coordinador, Maestro, Aspirante), les asigna una carrera de
--             ejemplo, y crea algunas materias/unidades/materiales de
--             ejemplo para poder probar la aplicación de inmediato.
--
-- Requisitos previos: la base de datos ya debe existir con su esquema
-- original (Usuarios, Roles, Aspirantes, Carreras, Periodos, Documentos,
-- TiposDocumentos, EstadosDocumentos, etc. - ver Databasenew.sql).
--
-- IMPORTANTE: este script NO trae un "USE <basededatos>" fijo a propósito,
-- porque el nombre de la base cambia según el entorno (CaptacionDB en local,
-- BolsaEgresadosUTTN en el servidor real, etc.). Antes de ejecutarlo, conecta
-- SSMS/Azure Data Studio a la base correcta (selecciónala en el desplegable
-- de la barra de herramientas, o agrega tu propio "USE <basededatos>; GO" al
-- principio) para no correrlo por accidente contra la base equivocada.
--
-- Es idempotente: se puede ejecutar más de una vez sin duplicar tablas ni
-- restricciones. La PARTE 2 SÍ borra y vuelve a insertar los datos de
-- prueba del módulo de inducción cada vez que se ejecuta (ver sección 2.1).
--
-- Las contraseñas de los usuarios de prueba se guardan en texto plano aquí;
-- la aplicación las convierte automáticamente a un hash seguro (PBKDF2) la
-- primera vez que ese usuario inicia sesión, así que no hace falta hashearlas
-- a mano en este script.
-- ============================================================================
USE CaptacionDB;
GO
-- ============================================================================
-- PARTE 1: ESQUEMA DEL MÓDULO DE INDUCCIÓN
-- ============================================================================

-- 1.1 Ind_Materias ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_Materias')
BEGIN
    CREATE TABLE dbo.Ind_Materias (
        MateriaID        INT IDENTITY(1,1) NOT NULL,
        PeriodoID         INT NOT NULL,
        Nombre            NVARCHAR(255) NOT NULL,
        Descripcion       NVARCHAR(MAX) NULL,
        Activo            BIT NOT NULL CONSTRAINT DF_IndMaterias_Activo DEFAULT (1),
        TodasLasCarreras  BIT NOT NULL CONSTRAINT DF_IndMaterias_TodasLasCarreras DEFAULT (0),
        CONSTRAINT PK_IndMaterias PRIMARY KEY CLUSTERED (MateriaID ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IndMaterias_Periodos')
BEGIN
    ALTER TABLE dbo.Ind_Materias WITH CHECK ADD CONSTRAINT FK_IndMaterias_Periodos
        FOREIGN KEY (PeriodoID) REFERENCES dbo.Periodos (PeriodoID);
END
GO

-- 1.2 Ind_MateriaCarreras (materia <-> carrera, muchos a muchos) --------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_MateriaCarreras')
BEGIN
    CREATE TABLE dbo.Ind_MateriaCarreras (
        MateriaID INT NOT NULL,
        CarreraID INT NOT NULL,
        CONSTRAINT PK_Ind_MateriaCarreras PRIMARY KEY (MateriaID, CarreraID),
        CONSTRAINT FK_IndMateriaCarreras_Materias FOREIGN KEY (MateriaID)
            REFERENCES dbo.Ind_Materias (MateriaID) ON DELETE CASCADE,
        CONSTRAINT FK_IndMateriaCarreras_Carreras FOREIGN KEY (CarreraID)
            REFERENCES dbo.Carreras (CarreraID)
    );
END
GO

-- 1.3 Ind_Unidades -------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_Unidades')
BEGIN
    CREATE TABLE dbo.Ind_Unidades (
        UnidadID  INT IDENTITY(1,1) NOT NULL,
        MateriaID INT NOT NULL,
        Nombre    NVARCHAR(255) NOT NULL,
        Orden     INT NOT NULL,
        CONSTRAINT PK_IndUnidades PRIMARY KEY CLUSTERED (UnidadID ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IndUnidades_Materias')
BEGIN
    ALTER TABLE dbo.Ind_Unidades WITH CHECK ADD CONSTRAINT FK_IndUnidades_Materias
        FOREIGN KEY (MateriaID) REFERENCES dbo.Ind_Materias (MateriaID);
END
GO

-- 1.4 Ind_Materiales (recursos educativos) ------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_Materiales')
BEGIN
    CREATE TABLE dbo.Ind_Materiales (
        MaterialID  INT IDENTITY(1,1) NOT NULL,
        UnidadID    INT NOT NULL,
        Nombre      NVARCHAR(255) NOT NULL,
        TipoRecurso NVARCHAR(50) NOT NULL,
        RutaURL     NVARCHAR(MAX) NOT NULL,
        Orden       INT NOT NULL CONSTRAINT DF_IndMateriales_Orden DEFAULT (0),
        CONSTRAINT PK_IndMateriales PRIMARY KEY CLUSTERED (MaterialID ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IndMateriales_Unidades')
BEGIN
    ALTER TABLE dbo.Ind_Materiales WITH CHECK ADD CONSTRAINT FK_IndMateriales_Unidades
        FOREIGN KEY (UnidadID) REFERENCES dbo.Ind_Unidades (UnidadID);
END
GO

-- Bases ya existentes que no tenían esta columna: se agrega y se rellena con el
-- orden de creación actual (MaterialID ascendente dentro de cada Unidad).
-- (El ALTER TABLE y el UPDATE que usa la columna nueva van en lotes/GO
-- separados: SQL Server no permite referenciar una columna recién agregada
-- dentro del mismo lote que la agrega.)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ind_Materiales') AND name = 'Orden')
BEGIN
    ALTER TABLE dbo.Ind_Materiales ADD Orden INT NOT NULL CONSTRAINT DF_IndMateriales_Orden DEFAULT (0);
END
GO

IF EXISTS (SELECT 1 FROM dbo.Ind_Materiales WHERE Orden = 0)
BEGIN
    ;WITH Numerado AS (
        SELECT MaterialID, ROW_NUMBER() OVER (PARTITION BY UnidadID ORDER BY MaterialID) AS Posicion
        FROM dbo.Ind_Materiales
        WHERE Orden = 0
    )
    UPDATE m
    SET m.Orden = n.Posicion
    FROM dbo.Ind_Materiales m
    INNER JOIN Numerado n ON n.MaterialID = m.MaterialID;
END
GO

-- 1.5 Ind_ProgresoAspirante (flujo simple de "marcar unidad como hecha") ------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_ProgresoAspirante')
BEGIN
    CREATE TABLE dbo.Ind_ProgresoAspirante (
        ProgresoID           INT IDENTITY(1,1) NOT NULL,
        AspiranteID          INT NOT NULL,
        UnidadID             INT NOT NULL,
        Estado               NVARCHAR(50) NOT NULL CONSTRAINT DF_IndProgreso_Estado DEFAULT ('Asignado'),
        FechaAsignacion      DATETIME NOT NULL CONSTRAINT DF_IndProgreso_FechaAsignacion DEFAULT (GETDATE()),
        FechaEnvio           DATETIME NULL,
        FechaRevision        DATETIME NULL,
        UsuarioCalificadorID INT NULL,
        ComentariosEvaluador NVARCHAR(MAX) NULL,
        CONSTRAINT PK_IndProgresoAspirante PRIMARY KEY CLUSTERED (ProgresoID ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IndProgreso_Aspirantes')
BEGIN
    ALTER TABLE dbo.Ind_ProgresoAspirante WITH CHECK ADD CONSTRAINT FK_IndProgreso_Aspirantes
        FOREIGN KEY (AspiranteID) REFERENCES dbo.Aspirantes (AspiranteID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IndProgreso_Unidades')
BEGIN
    ALTER TABLE dbo.Ind_ProgresoAspirante WITH CHECK ADD CONSTRAINT FK_IndProgreso_Unidades
        FOREIGN KEY (UnidadID) REFERENCES dbo.Ind_Unidades (UnidadID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IndProgreso_UsuarioCalificador')
BEGIN
    ALTER TABLE dbo.Ind_ProgresoAspirante WITH CHECK ADD CONSTRAINT FK_IndProgreso_UsuarioCalificador
        FOREIGN KEY (UsuarioCalificadorID) REFERENCES dbo.Usuarios (UsuarioID);
END
GO

-- Bases ya existentes que sí tenían Calificacion: ya no se va a calificar con
-- número las unidades, solo se marcan como revisadas.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ind_ProgresoAspirante') AND name = 'Calificacion')
BEGIN
    ALTER TABLE dbo.Ind_ProgresoAspirante DROP COLUMN Calificacion;
END
GO

-- El estado "Calificado" ya no existe (se renombró a "Revisado" porque ya no
-- hay número que asignar, solo aprobar).
IF EXISTS (SELECT 1 FROM dbo.Ind_ProgresoAspirante WHERE Estado = 'Calificado')
BEGIN
    UPDATE dbo.Ind_ProgresoAspirante SET Estado = 'Revisado' WHERE Estado = 'Calificado';
END
GO

-- Bases ya existentes que no tenían esta columna: guarda cuándo se aprobó la
-- unidad (se llena en automático cuando se revisan todos sus entregables).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ind_ProgresoAspirante') AND name = 'FechaRevision')
BEGIN
    ALTER TABLE dbo.Ind_ProgresoAspirante ADD FechaRevision DATETIME NULL;
END
GO

-- 1.6 Ind_Entregables (definición de tareas/archivos a subir) -----------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_Entregables')
BEGIN
    CREATE TABLE dbo.Ind_Entregables (
        EntregableID   INT IDENTITY(1,1) NOT NULL,
        UnidadID       INT NOT NULL,
        Titulo         NVARCHAR(255) NOT NULL,
        Instrucciones  NVARCHAR(MAX) NULL,
        FechaLimite    DATETIME NULL,
        Activo         BIT NOT NULL CONSTRAINT DF_IndEntregables_Activo DEFAULT (1),
        Orden          INT NOT NULL CONSTRAINT DF_IndEntregables_Orden DEFAULT (0),
        CONSTRAINT PK_IndEntregables PRIMARY KEY CLUSTERED (EntregableID ASC)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IndEntregables_Unidades')
BEGIN
    ALTER TABLE dbo.Ind_Entregables WITH CHECK ADD CONSTRAINT FK_IndEntregables_Unidades
        FOREIGN KEY (UnidadID) REFERENCES dbo.Ind_Unidades (UnidadID);
END
GO

-- Bases ya existentes que no tenían esta columna: se agrega y se rellena con el
-- orden de creación actual (EntregableID ascendente dentro de cada Unidad).
-- (El ALTER TABLE y el UPDATE que usa la columna nueva van en lotes/GO
-- separados: SQL Server no permite referenciar una columna recién agregada
-- dentro del mismo lote que la agrega.)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ind_Entregables') AND name = 'Orden')
BEGIN
    ALTER TABLE dbo.Ind_Entregables ADD Orden INT NOT NULL CONSTRAINT DF_IndEntregables_Orden DEFAULT (0);
END
GO

IF EXISTS (SELECT 1 FROM dbo.Ind_Entregables WHERE Orden = 0)
BEGIN
    ;WITH Numerado AS (
        SELECT EntregableID, ROW_NUMBER() OVER (PARTITION BY UnidadID ORDER BY EntregableID) AS Posicion
        FROM dbo.Ind_Entregables
        WHERE Orden = 0
    )
    UPDATE e
    SET e.Orden = n.Posicion
    FROM dbo.Ind_Entregables e
    INNER JOIN Numerado n ON n.EntregableID = e.EntregableID;
END
GO

-- Bases ya existentes que sí tenían PonderacionMax: ya no se va a calificar
-- con número, así que se quita la columna (primero su default constraint,
-- luego la columna, en lotes/GO separados).
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ind_Entregables') AND name = 'PonderacionMax')
BEGIN
    DECLARE @ConstraintName NVARCHAR(200) = (
        SELECT dc.name FROM sys.default_constraints dc
        INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        WHERE dc.parent_object_id = OBJECT_ID('dbo.Ind_Entregables') AND c.name = 'PonderacionMax'
    );
    IF @ConstraintName IS NOT NULL
        EXEC('ALTER TABLE dbo.Ind_Entregables DROP CONSTRAINT ' + @ConstraintName);
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ind_Entregables') AND name = 'PonderacionMax')
BEGIN
    ALTER TABLE dbo.Ind_Entregables DROP COLUMN PonderacionMax;
END
GO

-- 1.7 Ind_Submisiones (archivos entregados por los aspirantes) ----------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_Submisiones')
BEGIN
    CREATE TABLE dbo.Ind_Submisiones (
        SubmisionID       INT IDENTITY(1,1) NOT NULL,
        AspiranteID       INT NOT NULL,
        EntregableID      INT NOT NULL,
        RutaArchivo       NVARCHAR(500) NOT NULL,
        DocumentoID       INT NULL,
        FechaEnvio        DATETIME NOT NULL CONSTRAINT DF_IndSubmisiones_FechaEnvio DEFAULT (GETDATE()),
        Estado            NVARCHAR(50) NOT NULL CONSTRAINT DF_IndSubmisiones_Estado DEFAULT ('Pendiente'),
        ComentarioRevisor NVARCHAR(MAX) NULL,
        UsuarioRevisorID  INT NULL,
        FechaRevision     DATETIME NULL,
        CONSTRAINT PK_IndSubmisiones PRIMARY KEY CLUSTERED (SubmisionID ASC),
        CONSTRAINT CK_IndSubmisiones_Estado CHECK (Estado IN ('Pendiente', 'Revisado', 'Rechazado'))
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IndSubmisiones_Aspirantes')
BEGIN
    ALTER TABLE dbo.Ind_Submisiones WITH CHECK ADD CONSTRAINT FK_IndSubmisiones_Aspirantes
        FOREIGN KEY (AspiranteID) REFERENCES dbo.Aspirantes (AspiranteID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IndSubmisiones_Entregables')
BEGIN
    ALTER TABLE dbo.Ind_Submisiones WITH CHECK ADD CONSTRAINT FK_IndSubmisiones_Entregables
        FOREIGN KEY (EntregableID) REFERENCES dbo.Ind_Entregables (EntregableID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IndSubmisiones_Usuarios')
BEGIN
    ALTER TABLE dbo.Ind_Submisiones WITH CHECK ADD CONSTRAINT FK_IndSubmisiones_Usuarios
        FOREIGN KEY (UsuarioRevisorID) REFERENCES dbo.Usuarios (UsuarioID);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Submisiones_Documentos')
BEGIN
    ALTER TABLE dbo.Ind_Submisiones WITH CHECK ADD CONSTRAINT FK_Submisiones_Documentos
        FOREIGN KEY (DocumentoID) REFERENCES dbo.Documentos (DocumentoID);
END
GO

-- Bases ya existentes que sí tenían Calificacion: ya no se va a calificar con
-- número las entregas, solo aprobar ("Revisado") o devolver ("Rechazado").
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ind_Submisiones') AND name = 'Calificacion')
BEGIN
    ALTER TABLE dbo.Ind_Submisiones DROP COLUMN Calificacion;
END
GO

-- 1.8 Rol Maestro --------------------------------------------------------------
-- Se agrega como un rol nuevo (no se reutiliza el RolID del Director eliminado,
-- ya que Roles es una tabla compartida con el resto del sistema de captación).
-- RolID es IDENTITY, así que el valor exacto lo asigna SQL Server; si en tu base
-- ya existían más de 4 roles, revisa el PRINT de abajo y ajusta los números
-- hardcodeados en el código C# ([RoleAuthorize], switches de rol) si no da 5.
IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Nombre = 'Maestro')
BEGIN
    INSERT INTO dbo.Roles (Nombre) VALUES ('Maestro');
    PRINT 'Rol "Maestro" creado con RolID ' + CAST(SCOPE_IDENTITY() AS VARCHAR) + '.';
END
GO

-- 1.9 Ind_UsuarioCarreras (usuario <-> carrera, muchos a muchos) ---------------
-- A qué carrera(s) está asignado un Coordinador, Maestro o Aspirante.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_UsuarioCarreras')
BEGIN
    CREATE TABLE dbo.Ind_UsuarioCarreras (
        UsuarioID INT NOT NULL,
        CarreraID INT NOT NULL,
        CONSTRAINT PK_Ind_UsuarioCarreras PRIMARY KEY (UsuarioID, CarreraID),
        CONSTRAINT FK_IndUsuarioCarreras_Usuarios FOREIGN KEY (UsuarioID)
            REFERENCES dbo.Usuarios (UsuarioID) ON DELETE CASCADE,
        CONSTRAINT FK_IndUsuarioCarreras_Carreras FOREIGN KEY (CarreraID)
            REFERENCES dbo.Carreras (CarreraID)
    );
END
GO

-- 1.10 Roles.Activo -------------------------------------------------------------
-- Permite "desactivar" un rol personalizado sin romper las FKs de los usuarios
-- que ya lo tengan asignado.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Roles') AND name = 'Activo')
BEGIN
    ALTER TABLE dbo.Roles ADD Activo BIT NOT NULL CONSTRAINT DF_Roles_Activo DEFAULT (1);
END
GO

-- 1.10b Carreras.Activo ----------------------------------------------------------
-- Permite "desactivar" una carrera (deja de ofrecerse para asignar a usuarios o
-- materias nuevas) sin borrarla ni romper el historial de quien ya la tiene.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Carreras') AND name = 'Activo')
BEGIN
    ALTER TABLE dbo.Carreras ADD Activo BIT NOT NULL CONSTRAINT DF_Carreras_Activo DEFAULT (1);
END
GO

-- 1.11 Ind_Permisos (catálogo de secciones/funciones del sistema) --------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_Permisos')
BEGIN
    CREATE TABLE dbo.Ind_Permisos (
        PermisoID   INT IDENTITY(1,1) NOT NULL,
        Clave       NVARCHAR(50) NOT NULL,
        Nombre      NVARCHAR(150) NOT NULL,
        Descripcion NVARCHAR(300) NULL,
        CONSTRAINT PK_IndPermisos PRIMARY KEY CLUSTERED (PermisoID ASC),
        CONSTRAINT UQ_IndPermisos_Clave UNIQUE (Clave)
    );
END
GO

-- 1.12 Ind_RolPermisos (qué puede hacer cada rol en cada sección) --------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_RolPermisos')
BEGIN
    CREATE TABLE dbo.Ind_RolPermisos (
        RolID         INT NOT NULL,
        PermisoID     INT NOT NULL,
        PuedeLeer     BIT NOT NULL CONSTRAINT DF_IndRolPermisos_Leer DEFAULT (0),
        PuedeCrear    BIT NOT NULL CONSTRAINT DF_IndRolPermisos_Crear DEFAULT (0),
        PuedeEditar   BIT NOT NULL CONSTRAINT DF_IndRolPermisos_Editar DEFAULT (0),
        PuedeEliminar BIT NOT NULL CONSTRAINT DF_IndRolPermisos_Eliminar DEFAULT (0),
        CONSTRAINT PK_IndRolPermisos PRIMARY KEY (RolID, PermisoID),
        CONSTRAINT FK_IndRolPermisos_Roles FOREIGN KEY (RolID) REFERENCES dbo.Roles (RolID),
        CONSTRAINT FK_IndRolPermisos_Permisos FOREIGN KEY (PermisoID) REFERENCES dbo.Ind_Permisos (PermisoID)
    );
END
GO

-- 1.13 Ind_UsuarioPermisos (excepciones por usuario específico, sobre su rol) --
-- NULL en cualquier columna = "hereda del rol"; 1/0 = permite/deniega sin
-- importar lo que diga el rol.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_UsuarioPermisos')
BEGIN
    CREATE TABLE dbo.Ind_UsuarioPermisos (
        UsuarioID     INT NOT NULL,
        PermisoID     INT NOT NULL,
        PuedeLeer     BIT NULL,
        PuedeCrear    BIT NULL,
        PuedeEditar   BIT NULL,
        PuedeEliminar BIT NULL,
        CONSTRAINT PK_IndUsuarioPermisos PRIMARY KEY (UsuarioID, PermisoID),
        CONSTRAINT FK_IndUsuarioPermisos_Usuarios FOREIGN KEY (UsuarioID) REFERENCES dbo.Usuarios (UsuarioID),
        CONSTRAINT FK_IndUsuarioPermisos_Permisos FOREIGN KEY (PermisoID) REFERENCES dbo.Ind_Permisos (PermisoID)
    );
END
GO

-- 1.14 Semilla de Ind_Permisos (catálogo de las 11 secciones actuales) --------
-- Idempotente: solo inserta las claves que todavía no existan.
INSERT INTO dbo.Ind_Permisos (Clave, Nombre, Descripcion)
SELECT v.Clave, v.Nombre, v.Descripcion
FROM (VALUES
    ('GestionContenido',   'Gestión de Contenido',         'Materias, unidades, materiales y entregables.'),
    ('GestionCarreras',    'Gestión de Carreras',          'Alta, edición y activación/desactivación de carreras.'),
    ('GestionUsuarios',    'Gestión de Usuarios',          'Alta, edición y activación/desactivación de usuarios.'),
    ('GestionPeriodos',    'Gestión de Periodos',          'Alta, edición y activación/desactivación de periodos.'),
    ('GestionRoles',       'Gestión de Roles y Permisos',  'Crear roles y definir sus permisos por sección.'),
    ('Reportes',           'Reportes',                     'Reportes con filtros por carrera, periodo y calificador.'),
    ('RevisarEntregables', 'Revisar Entregables',          'Revisar archivos subidos por los aspirantes (aprobar o devolver).'),
    ('MisAspirantes',      'Mis Aspirantes',               'Ver y asignar/reasignar unidades a los aspirantes.'),
    ('MisMaestros',        'Mis Maestros',                 'Ver los Maestros de la carrera y su actividad.'),
    ('MiEspacio',          'Mi Espacio',                   'Ver cursos, progreso y marcar unidades como entregadas.'),
    ('SubirEntregables',   'Subir Entregables',            'Subir o reemplazar archivos de entregables.')
) AS v(Clave, Nombre, Descripcion)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ind_Permisos p WHERE p.Clave = v.Clave);
GO

-- 1.15 Semilla de Ind_RolPermisos: reproduce EXACTAMENTE el comportamiento de
-- hoy (los [RoleAuthorize] ya existentes en el código), para que desplegar esto
-- no cambie nada hasta que un Admin edite algo desde la nueva pantalla de
-- Permisos por Rol. RolID hardcodeado igual que en CarreraScopeHelper.cs:
-- 1 = Administrador, 3 = Coordinador, 4 = Aspirante, 5 = Maestro.
INSERT INTO dbo.Ind_RolPermisos (RolID, PermisoID, PuedeLeer, PuedeCrear, PuedeEditar, PuedeEliminar)
SELECT v.RolID, p.PermisoID, v.Leer, v.Crear, v.Editar, v.Eliminar
FROM (VALUES
    -- Administrador: control total sobre sus secciones.
    (1, 'GestionContenido',   1, 1, 1, 1),
    (1, 'GestionCarreras',    1, 1, 1, 1),
    (1, 'GestionUsuarios',    1, 1, 1, 1),
    (1, 'GestionPeriodos',    1, 1, 1, 1),
    (1, 'GestionRoles',       1, 1, 1, 1),
    (1, 'Reportes',           1, 1, 1, 1),
    -- Coordinador: contenido + revisión + sus aspirantes + sus maestros (esto
    -- último exclusivo de Coordinador, no de Maestro).
    (3, 'GestionContenido',   1, 1, 1, 1),
    (3, 'RevisarEntregables', 1, 1, 1, 1),
    (3, 'MisAspirantes',      1, 1, 1, 1),
    (3, 'MisMaestros',        1, 0, 0, 0),
    -- Maestro: igual que Coordinador pero sin "Mis Maestros".
    (5, 'GestionContenido',   1, 1, 1, 1),
    (5, 'RevisarEntregables', 1, 1, 1, 1),
    (5, 'MisAspirantes',      1, 1, 1, 1),
    -- Aspirante: su propio espacio y subir entregables.
    (4, 'MiEspacio',          1, 0, 1, 0),
    (4, 'SubirEntregables',   0, 1, 0, 0)
) AS v(RolID, Clave, Leer, Crear, Editar, Eliminar)
INNER JOIN dbo.Ind_Permisos p ON p.Clave = v.Clave
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Ind_RolPermisos rp WHERE rp.RolID = v.RolID AND rp.PermisoID = p.PermisoID
);
GO

-- 1.16 Limpieza de "RevisarUnidades": ya no existe (ahora solo se revisan
-- entregas; las unidades sin entregable se marcan como revisadas desde el
-- detalle de "Mis Aspirantes", bajo el permiso MisAspirantes). Se quita el
-- catálogo y sus asignaciones si quedaron de una instalación anterior.
IF EXISTS (SELECT 1 FROM dbo.Ind_Permisos WHERE Clave = 'RevisarUnidades')
BEGIN
    DECLARE @PermisoRevisarUnidadesID INT = (SELECT PermisoID FROM dbo.Ind_Permisos WHERE Clave = 'RevisarUnidades');
    DELETE FROM dbo.Ind_UsuarioPermisos WHERE PermisoID = @PermisoRevisarUnidadesID;
    DELETE FROM dbo.Ind_RolPermisos WHERE PermisoID = @PermisoRevisarUnidadesID;
    DELETE FROM dbo.Ind_Permisos WHERE PermisoID = @PermisoRevisarUnidadesID;
END
GO

-- 1.17 Ind_Areas (catálogo propio del módulo de inducción, NO el dbo.Areas de
-- captación) — es el catálogo "padre": cada Carrera pertenece a un Área (no al
-- revés), gestionado por Admin desde Gestión de Carreras. Se usa para
-- autoasignar el Área de los aspirantes creados por la carga masiva (Fase 7).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_Areas')
BEGIN
    CREATE TABLE dbo.Ind_Areas (
        AreaID INT IDENTITY(1,1) NOT NULL,
        Nombre NVARCHAR(150) NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_IndAreas_Activo DEFAULT (1),
        CONSTRAINT PK_IndAreas PRIMARY KEY CLUSTERED (AreaID ASC)
    );
END
GO

-- 1.18 Usuarios.Ind_AreaID -------------------------------------------------------
-- Nullable: solo se usa para usuarios con rol Aspirante creados por la carga
-- masiva (Fase 7); el resto de usuarios la deja en NULL.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Usuarios') AND name = 'Ind_AreaID')
BEGIN
    ALTER TABLE dbo.Usuarios ADD Ind_AreaID INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Usuarios_IndAreas')
BEGIN
    ALTER TABLE dbo.Usuarios ADD CONSTRAINT FK_Usuarios_IndAreas FOREIGN KEY (Ind_AreaID) REFERENCES dbo.Ind_Areas (AreaID);
END
GO

-- 1.20 Carreras.AreaID -----------------------------------------------------------
-- Cada carrera pertenece a un Área (nullable: las carreras existentes de antes
-- de este cambio, o recién creadas antes de asignarles una, quedan en NULL
-- hasta que un Admin la edite; el código bloquea crear carreras nuevas sin
-- Área si ya no queda ninguna Área en el sistema).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Carreras') AND name = 'AreaID')
BEGIN
    ALTER TABLE dbo.Carreras ADD AreaID INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Carreras_IndAreas')
BEGIN
    ALTER TABLE dbo.Carreras ADD CONSTRAINT FK_Carreras_IndAreas FOREIGN KEY (AreaID) REFERENCES dbo.Ind_Areas (AreaID);
END
GO

-- 1.21 Migración: si Ind_Areas todavía tiene la columna CarreraID de una
-- instalación anterior a este cambio (Área dependía de Carrera), se migran
-- esos datos a Carreras.AreaID (la carrera toma la primera Área que tenía) y
-- se quita la columna vieja, invirtiendo la relación definitivamente.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ind_Areas') AND name = 'CarreraID')
BEGIN
    UPDATE c
    SET c.AreaID = (SELECT MIN(a.AreaID) FROM dbo.Ind_Areas a WHERE a.CarreraID = c.CarreraID)
    FROM dbo.Carreras c
    WHERE c.AreaID IS NULL
      AND EXISTS (SELECT 1 FROM dbo.Ind_Areas a WHERE a.CarreraID = c.CarreraID);
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ind_Areas') AND name = 'CarreraID')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_IndAreas_Carreras')
    BEGIN
        ALTER TABLE dbo.Ind_Areas DROP CONSTRAINT FK_IndAreas_Carreras;
    END
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Ind_Areas') AND name = 'CarreraID')
BEGIN
    ALTER TABLE dbo.Ind_Areas DROP COLUMN CarreraID;
END
GO

-- 1.22 Habilitar "Crear" en MisMaestros para Coordinador ------------------------
-- Antes "Mis Maestros" era de solo lectura; ahora también permite la carga
-- masiva de maestros (Fase 7), que requiere el permiso Crear.
UPDATE dbo.Ind_RolPermisos
SET PuedeCrear = 1
WHERE RolID = 3
  AND PermisoID = (SELECT PermisoID FROM dbo.Ind_Permisos WHERE Clave = 'MisMaestros')
  AND PuedeCrear = 0;
GO

PRINT 'PARTE 1 completa: esquema del módulo de inducción listo.';
GO


-- ============================================================================
-- PARTE 2: DATOS DE PRUEBA
-- ============================================================================
-- Crea un usuario de cada rol y contenido de ejemplo. Se puede ejecutar varias
-- veces: primero limpia lo que había insertado la vez anterior.
-- Mapeo de roles: 1 = Administrador, 2 = Director, 3 = Coordinador, 4 = Aspirante
-- ============================================================================

-- 2.1 Limpieza de datos de inducción insertados por este script ---------------
DELETE FROM dbo.Ind_ProgresoAspirante;
DELETE FROM dbo.Ind_Submisiones;
DELETE FROM dbo.Ind_Entregables;
DELETE FROM dbo.Ind_Materiales;
DELETE FROM dbo.Ind_MateriaCarreras;
DELETE FROM dbo.Ind_Unidades;
DELETE FROM dbo.Ind_Materias;

IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.Ind_Materias'))
    DBCC CHECKIDENT ('dbo.Ind_Materias', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.Ind_Unidades'))
    DBCC CHECKIDENT ('dbo.Ind_Unidades', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.Ind_Materiales'))
    DBCC CHECKIDENT ('dbo.Ind_Materiales', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.Ind_Entregables'))
    DBCC CHECKIDENT ('dbo.Ind_Entregables', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.Ind_Submisiones'))
    DBCC CHECKIDENT ('dbo.Ind_Submisiones', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.Ind_ProgresoAspirante'))
    DBCC CHECKIDENT ('dbo.Ind_ProgresoAspirante', RESEED, 0);
GO

-- 2.2 Un usuario de prueba por rol --------------------------------------------
DECLARE @TargetAspiranteID INT;
DECLARE @RealAspiranteUserID INT;

-- Reutilizamos el primer Aspirante real (con sus llaves foráneas ya resueltas)
-- y lo forzamos a usar el rol/correo de prueba, en vez de crear uno desde cero.
SELECT TOP 1 @TargetAspiranteID = AspiranteID, @RealAspiranteUserID = UsuarioID
FROM dbo.Aspirantes
ORDER BY AspiranteID ASC;

IF @TargetAspiranteID IS NOT NULL
BEGIN
    UPDATE dbo.Usuarios
    SET CorreoElectronico = 'aspirante@test.com',
        Contrasena = 'Password123!',
        RolID = 4,
        Activo = 1
    WHERE UsuarioID = @RealAspiranteUserID;

    PRINT 'AspiranteID ' + CAST(@TargetAspiranteID AS VARCHAR) + ' asignado como aspirante@test.com (RolID 4).';
END

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE CorreoElectronico = 'admin@test.com')
    INSERT INTO dbo.Usuarios (Nombre, ApellidoPaterno, ApellidoMaterno, NombreUsuario, CorreoElectronico, Contrasena, Activo, FechaRegistro, RolID)
    VALUES ('Admin', 'Sistemas', 'UTTN', 'admin01', 'admin@test.com', 'Password123!', 1, GETDATE(), 1);
ELSE
    UPDATE dbo.Usuarios SET Contrasena = 'Password123!', RolID = 1, Activo = 1 WHERE CorreoElectronico = 'admin@test.com';

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE CorreoElectronico = 'coordinador@test.com')
    INSERT INTO dbo.Usuarios (Nombre, ApellidoPaterno, ApellidoMaterno, NombreUsuario, CorreoElectronico, Contrasena, Activo, FechaRegistro, RolID)
    VALUES ('Sarah', 'Connor', 'Smith', 'coordinador01', 'coordinador@test.com', 'Password123!', 1, GETDATE(), 3);
ELSE
    UPDATE dbo.Usuarios SET Contrasena = 'Password123!', RolID = 3, Activo = 1 WHERE CorreoElectronico = 'coordinador@test.com';

DECLARE @RolMaestroID INT = (SELECT TOP 1 RolID FROM dbo.Roles WHERE Nombre = 'Maestro');
IF @RolMaestroID IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE CorreoElectronico = 'maestro@test.com')
        INSERT INTO dbo.Usuarios (Nombre, ApellidoPaterno, ApellidoMaterno, NombreUsuario, CorreoElectronico, Contrasena, Activo, FechaRegistro, RolID)
        VALUES ('Juan', 'Pérez', 'UTTN', 'maestro01', 'maestro@test.com', 'Password123!', 1, GETDATE(), @RolMaestroID);
    ELSE
        UPDATE dbo.Usuarios SET Contrasena = 'Password123!', RolID = @RolMaestroID, Activo = 1 WHERE CorreoElectronico = 'maestro@test.com';
END

-- Asignamos una carrera de ejemplo a los usuarios de prueba que ya tienen ese
-- concepto (Coordinador, Maestro, Aspirante), reutilizando la primera Carrera
-- que exista en la base.
DECLARE @CarreraEjemploID INT = (SELECT TOP 1 CarreraID FROM dbo.Carreras ORDER BY CarreraID ASC);
IF @CarreraEjemploID IS NOT NULL
BEGIN
    DECLARE @UsuarioCoordinadorID INT = (SELECT UsuarioID FROM dbo.Usuarios WHERE CorreoElectronico = 'coordinador@test.com');
    DECLARE @UsuarioMaestroID INT = (SELECT UsuarioID FROM dbo.Usuarios WHERE CorreoElectronico = 'maestro@test.com');
    DECLARE @UsuarioAspiranteID INT = @RealAspiranteUserID;

    IF @UsuarioCoordinadorID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Ind_UsuarioCarreras WHERE UsuarioID = @UsuarioCoordinadorID)
        INSERT INTO dbo.Ind_UsuarioCarreras (UsuarioID, CarreraID) VALUES (@UsuarioCoordinadorID, @CarreraEjemploID);

    IF @UsuarioMaestroID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Ind_UsuarioCarreras WHERE UsuarioID = @UsuarioMaestroID)
        INSERT INTO dbo.Ind_UsuarioCarreras (UsuarioID, CarreraID) VALUES (@UsuarioMaestroID, @CarreraEjemploID);

    IF @UsuarioAspiranteID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Ind_UsuarioCarreras WHERE UsuarioID = @UsuarioAspiranteID)
        INSERT INTO dbo.Ind_UsuarioCarreras (UsuarioID, CarreraID) VALUES (@UsuarioAspiranteID, @CarreraEjemploID);
END

PRINT 'Usuarios de prueba (Admin, Coordinador, Maestro, Aspirante) listos.';

-- 2.3 Materias, unidades y materiales de ejemplo ------------------------------
-- (sin GO antes de esta sección: @TargetAspiranteID se sigue usando en el
-- paso 2.4 más abajo, y las variables locales no sobreviven a un GO)
DECLARE @MateriaID1 INT, @MateriaID2 INT, @MateriaID3 INT, @PeriodoID INT;

-- Reutilizamos un Periodo activo ya existente; si no hay ninguno (base nueva
-- sin pasar por /Admin/GestionPeriodos todavía), se crea uno de ejemplo.
SELECT TOP 1 @PeriodoID = PeriodoID FROM dbo.Periodos WHERE Activo = 1 ORDER BY PeriodoID ASC;

IF @PeriodoID IS NULL
BEGIN
    INSERT INTO dbo.Periodos (FechaInicio, FechaFin, Activo)
    VALUES (DATEFROMPARTS(YEAR(GETDATE()), 1, 1), DATEFROMPARTS(YEAR(GETDATE()), 6, 30), 1);
    SET @PeriodoID = SCOPE_IDENTITY();
    PRINT 'No había ningún Periodo activo: se creó uno de ejemplo (PeriodoID ' + CAST(@PeriodoID AS VARCHAR) + ').';
END

-- Todas marcadas como "visibles para todas las carreras" para simplificar los
-- datos de prueba (ver Ind_Materias.TodasLasCarreras / Ind_MateriaCarreras).
INSERT INTO dbo.Ind_Materias (PeriodoID, Nombre, Descripcion, Activo, TodasLasCarreras)
VALUES (@PeriodoID, 'Introducción a la UTTN', 'Historia, misión, visión y servicios estudiantiles.', 1, 1);
SET @MateriaID1 = SCOPE_IDENTITY();

INSERT INTO dbo.Ind_Materias (PeriodoID, Nombre, Descripcion, Activo, TodasLasCarreras)
VALUES (@PeriodoID, 'Nivelación Académica', 'Reforzamiento de matemáticas básicas y comprensión lectora.', 1, 1);
SET @MateriaID2 = SCOPE_IDENTITY();

INSERT INTO dbo.Ind_Materias (PeriodoID, Nombre, Descripcion, Activo, TodasLasCarreras)
VALUES (@PeriodoID, 'Desarrollo Socioemocionales', 'Habilidades blandas y adaptabilidad universitaria.', 1, 1);
SET @MateriaID3 = SCOPE_IDENTITY();

DECLARE @UnidadID1 INT, @UnidadID2 INT, @UnidadID3 INT, @UnidadID4 INT, @UnidadID5 INT, @UnidadID6 INT;

INSERT INTO dbo.Ind_Unidades (MateriaID, Nombre, Orden) VALUES (@MateriaID1, 'Historia y Filosofía Institucional', 1);
SET @UnidadID1 = SCOPE_IDENTITY();
INSERT INTO dbo.Ind_Unidades (MateriaID, Nombre, Orden) VALUES (@MateriaID1, 'Servicios y Recursos Estudiantiles', 2);
SET @UnidadID2 = SCOPE_IDENTITY();
INSERT INTO dbo.Ind_Unidades (MateriaID, Nombre, Orden) VALUES (@MateriaID1, 'Reglamento y Normatividad', 3);
SET @UnidadID3 = SCOPE_IDENTITY();

INSERT INTO dbo.Ind_Unidades (MateriaID, Nombre, Orden) VALUES (@MateriaID2, 'Matemáticas Fundamentales', 1);
SET @UnidadID4 = SCOPE_IDENTITY();
INSERT INTO dbo.Ind_Unidades (MateriaID, Nombre, Orden) VALUES (@MateriaID2, 'Comprensión Lectora y Redacción', 2);
SET @UnidadID5 = SCOPE_IDENTITY();

INSERT INTO dbo.Ind_Unidades (MateriaID, Nombre, Orden) VALUES (@MateriaID3, 'Inteligencia Emocional', 1);
SET @UnidadID6 = SCOPE_IDENTITY();
INSERT INTO dbo.Ind_Unidades (MateriaID, Nombre, Orden) VALUES (@MateriaID3, 'Trabajo Colaborativo', 2);
INSERT INTO dbo.Ind_Unidades (MateriaID, Nombre, Orden) VALUES (@MateriaID3, 'Gestión del Tiempo', 3);

INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL, Orden) VALUES (@UnidadID1, 'Historia de la UTTN - PDF', 'PDF', 'https://www.uttn.edu.mx/docs/historia_uttn.pdf', 1);
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL, Orden) VALUES (@UnidadID1, 'Video Institucional Misión', 'Video', 'https://www.youtube.com/watch?v=example1', 2);
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL, Orden) VALUES (@UnidadID2, 'Guía de Servicios Escolares', 'PDF', 'https://www.uttn.edu.mx/docs/servicios.pdf', 1);
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL, Orden) VALUES (@UnidadID3, 'Reglamento General de Alumnos', 'PDF', 'https://www.uttn.edu.mx/docs/reglamento.pdf', 1);
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL, Orden) VALUES (@UnidadID4, 'Manual de Álgebra Básica', 'PDF', 'https://www.uttn.edu.mx/docs/algebra.pdf', 1);

-- Un entregable de ejemplo en la primera unidad, para poder probar el flujo de subida
INSERT INTO dbo.Ind_Entregables (UnidadID, Titulo, Instrucciones, FechaLimite, Activo, Orden)
VALUES (@UnidadID1, 'Prueba de Entregable', 'Sube cualquier documento en PDF como prueba del flujo de entrega.', DATEADD(DAY, 14, GETDATE()), 1, 1);

-- 2.4 Progreso de ejemplo para el aspirante de pruebas -------------------------
IF @TargetAspiranteID IS NOT NULL
BEGIN
    INSERT INTO dbo.Ind_ProgresoAspirante (AspiranteID, UnidadID, Estado, FechaAsignacion)
    VALUES
    (@TargetAspiranteID, @UnidadID1, 'Asignado', GETDATE()),
    (@TargetAspiranteID, @UnidadID2, 'Asignado', GETDATE()),
    (@TargetAspiranteID, @UnidadID3, 'Asignado', GETDATE()),
    (@TargetAspiranteID, @UnidadID4, 'Asignado', GETDATE()),
    (@TargetAspiranteID, @UnidadID5, 'Asignado', GETDATE());

    UPDATE dbo.Ind_ProgresoAspirante
    SET Estado = 'Revisado', FechaEnvio = GETDATE(), ComentariosEvaluador = 'Excelente trabajo inicial.', UsuarioCalificadorID = @UsuarioCoordinadorID
    WHERE AspiranteID = @TargetAspiranteID AND UnidadID = @UnidadID1;

    UPDATE dbo.Ind_ProgresoAspirante
    SET Estado = 'Revisado', FechaEnvio = GETDATE(), ComentariosEvaluador = 'Buen desempeño en la evaluación.', UsuarioCalificadorID = @UsuarioCoordinadorID
    WHERE AspiranteID = @TargetAspiranteID AND UnidadID = @UnidadID2;

    PRINT 'Materias, unidades, materiales y progreso de ejemplo cargados.';
END
GO


-- ============================================================================
-- VERIFICACIÓN FINAL
-- ============================================================================
PRINT '========================================';
PRINT 'CREDENCIALES DE PRUEBA DISPONIBLES:';
PRINT '(la contraseña se convierte a hash automáticamente al iniciar sesión)';
PRINT '========================================';
SELECT NombreUsuario, CorreoElectronico, 'Password123!' AS ContrasenaInicial, R.Nombre AS RolAsignado
FROM dbo.Usuarios U
INNER JOIN dbo.Roles R ON U.RolID = R.RolID
WHERE CorreoElectronico IN ('aspirante@test.com', 'coordinador@test.com', 'maestro@test.com', 'admin@test.com');
GO
