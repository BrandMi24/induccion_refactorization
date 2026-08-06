-- ============================================================================
-- INDUCCIONDB - SCRIPT COMPLETO DE CREACIÓN (BASE DE DATOS PROPIA E INDEPENDIENTE)
-- ============================================================================
-- A partir de aquí, el Sistema de Inducción deja de compartir base de datos con
-- el sistema de Captación (antes vivía dentro de CaptacionDB, reutilizando sus
-- tablas Usuarios/Roles/Carreras/Periodos/Documentos/etc.). Este script crea
-- una base de datos NUEVA Y PROPIA, "InduccionDB", con únicamente las tablas
-- que el módulo de Inducción realmente usa - nada del resto del esquema de
-- Captación (Aspirantes, Generos, EscuelasProcedencia, NivelesIngles,
-- AreasPorPeriodos, etc.) se replica aquí, porque nunca fue usado por esta app.
--
-- "Aspirante" ya no es una tabla ni una entidad aparte: es simplemente un
-- Usuario con RolID = 4. Los FKs que antes apuntaban a Aspirantes.AspiranteID
-- (progreso, submisiones, documentos) ahora apuntan directo a
-- Usuarios.UsuarioID. El "Folio" de un aspirante es su propio NombreUsuario
-- (10 dígitos consecutivos, con ceros a la izquierda - ver
-- CoordinadorController.GenerarSiguienteFolio).
--
-- Qué hace:
--   PARTE 1 - Esquema: crea la base de datos InduccionDB (si no existe) y
--             TODAS sus tablas desde cero, con sus llaves primarias, foráneas,
--             valores por defecto y restricciones - reflejando exactamente
--             los modelos EF6 actuales en Models/*.cs.
--   PARTE 2 - Datos de prueba: catálogos base (Roles, Permisos, Tipos de
--             Carrera, Áreas, Carreras, un Periodo activo), un usuario de
--             cada rol (Admin, Coordinador, Maestro, Aspirante) y contenido
--             de ejemplo (materias/unidades/materiales/entregables/progreso)
--             para poder probar la aplicación de inmediato.
--
-- Es idempotente: se puede ejecutar más de una vez sin duplicar tablas,
-- catálogos ni restricciones. La PARTE 2 SÍ borra y vuelve a insertar los
-- datos de prueba de contenido (materias/unidades/etc.) cada vez que corre,
-- igual que hacía el script anterior sobre CaptacionDB.
--
-- Las contraseñas de los usuarios de prueba se guardan en texto plano aquí;
-- la aplicación las convierte automáticamente a un hash seguro (PBKDF2) la
-- primera vez que ese usuario inicia sesión, así que no hace falta hashearlas
-- a mano en este script.
--
-- Después de correr este script, actualiza la cadena de conexión
-- "CaptacionDbContext" en Web.config para que apunte a InduccionDB en vez de
-- CaptacionDB (el nombre de la clase DbContext se queda igual, solo cambia a
-- qué base de datos se conecta).
-- ============================================================================

IF DB_ID('InduccionDB') IS NULL
BEGIN
    CREATE DATABASE InduccionDB;
END
GO

USE InduccionDB;
GO

-- ============================================================================
-- PARTE 1: ESQUEMA COMPLETO
-- ============================================================================
-- Orden de creación respetando dependencias de llaves foráneas:
--   Roles, Ind_Areas, TiposCarreras -> Carreras -> Usuarios -> Periodos
--   -> Ind_Materias -> Ind_Unidades -> Ind_Materiales / Ind_Entregables
--   -> Ind_ProgresoAspirante -> TiposDocumentos / EstadosDocumentos
--   -> Documentos -> Ind_Submisiones -> Ind_Permisos -> Ind_RolPermisos
--   -> Ind_UsuarioPermisos -> Ind_MateriaCarreras -> Ind_UsuarioCarreras

-- 1.1 Roles ---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Roles')
BEGIN
    CREATE TABLE dbo.Roles (
        RolID  INT IDENTITY(1,1) NOT NULL,
        Nombre NVARCHAR(50) NOT NULL,
        Activo BIT NOT NULL CONSTRAINT DF_Roles_Activo DEFAULT (1),
        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (RolID ASC)
    );
END
GO

-- 1.2 Ind_Areas (catálogo propio del módulo de inducción) -----------------
-- Es el catálogo "padre": cada Carrera pertenece a un Área (no al revés).
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

-- 1.3 TiposCarreras ---------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TiposCarreras')
BEGIN
    CREATE TABLE dbo.TiposCarreras (
        TipoCarreraID INT IDENTITY(1,1) NOT NULL,
        Nombre        NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_TiposCarreras PRIMARY KEY CLUSTERED (TipoCarreraID ASC)
    );
END
GO

-- 1.4 Carreras ---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Carreras')
BEGIN
    CREATE TABLE dbo.Carreras (
        CarreraID     INT IDENTITY(1,1) NOT NULL,
        Nombre        NVARCHAR(100) NOT NULL,
        Nomenclatura  NVARCHAR(20) NULL,
        TipoCarreraID INT NOT NULL,
        Activo        BIT NOT NULL CONSTRAINT DF_Carreras_Activo DEFAULT (1),
        -- Nullable: una carrera puede quedar temporalmente sin Área asignada.
        AreaID        INT NULL,
        CONSTRAINT PK_Carreras PRIMARY KEY CLUSTERED (CarreraID ASC),
        CONSTRAINT FK_Carreras_TiposCarreras FOREIGN KEY (TipoCarreraID) REFERENCES dbo.TiposCarreras (TipoCarreraID),
        CONSTRAINT FK_Carreras_IndAreas FOREIGN KEY (AreaID) REFERENCES dbo.Ind_Areas (AreaID)
    );
END
GO

-- 1.5 Usuarios -----------------------------------------------------------------
-- Tabla única de cuentas para los 4 roles (Administrador, Director [sin
-- acceso], Coordinador, Maestro, Aspirante). "Aspirante" ya no es más que
-- RolID = 4; el Folio de un aspirante es directamente su NombreUsuario.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Usuarios')
BEGIN
    CREATE TABLE dbo.Usuarios (
        UsuarioID         INT IDENTITY(1,1) NOT NULL,
        RolID             INT NOT NULL,
        Nombre            NVARCHAR(100) NOT NULL,
        ApellidoPaterno   NVARCHAR(80) NOT NULL,
        ApellidoMaterno   NVARCHAR(80) NULL,
        NombreUsuario     NVARCHAR(50) NOT NULL,
        CorreoElectronico NVARCHAR(200) NOT NULL,
        Telefono          NVARCHAR(10) NULL,
        Contrasena        NVARCHAR(255) NOT NULL,
        Activo            BIT NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
        FechaRegistro     DATETIME NULL CONSTRAINT DF_Usuarios_FechaRegistro DEFAULT (GETDATE()),
        UltimoAcceso      DATETIME NULL,
        FotoPerfil        NVARCHAR(MAX) NULL,
        -- Solo se usa para usuarios con rol Aspirante creados por la carga
        -- masiva: el Área se autoasigna de la primera Área activa de su carrera.
        Ind_AreaID        INT NULL,
        CONSTRAINT PK_Usuarios PRIMARY KEY CLUSTERED (UsuarioID ASC),
        CONSTRAINT UQ_Usuarios_NombreUsuario UNIQUE (NombreUsuario),
        CONSTRAINT UQ_Usuarios_CorreoElectronico UNIQUE (CorreoElectronico),
        CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (RolID) REFERENCES dbo.Roles (RolID),
        CONSTRAINT FK_Usuarios_IndAreas FOREIGN KEY (Ind_AreaID) REFERENCES dbo.Ind_Areas (AreaID)
    );
END
GO

-- 1.6 Periodos -----------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Periodos')
BEGIN
    CREATE TABLE dbo.Periodos (
        PeriodoID   INT IDENTITY(1,1) NOT NULL,
        FechaInicio DATE NOT NULL,
        FechaFin    DATE NOT NULL,
        Activo      BIT NOT NULL CONSTRAINT DF_Periodos_Activo DEFAULT (1),
        CONSTRAINT PK_Periodos PRIMARY KEY CLUSTERED (PeriodoID ASC)
    );
END
GO

-- 1.7 Ind_Materias ---------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_Materias')
BEGIN
    CREATE TABLE dbo.Ind_Materias (
        MateriaID        INT IDENTITY(1,1) NOT NULL,
        PeriodoID        INT NOT NULL,
        Nombre           NVARCHAR(255) NOT NULL,
        Descripcion      NVARCHAR(MAX) NULL,
        Activo           BIT NOT NULL CONSTRAINT DF_IndMaterias_Activo DEFAULT (1),
        -- Cuando es true, la materia es visible para todas las carreras sin
        -- importar lo que diga Ind_MateriaCarreras.
        TodasLasCarreras BIT NOT NULL CONSTRAINT DF_IndMaterias_TodasLasCarreras DEFAULT (0),
        CONSTRAINT PK_IndMaterias PRIMARY KEY CLUSTERED (MateriaID ASC),
        CONSTRAINT FK_IndMaterias_Periodos FOREIGN KEY (PeriodoID) REFERENCES dbo.Periodos (PeriodoID)
    );
END
GO

-- 1.8 Ind_Unidades -------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_Unidades')
BEGIN
    CREATE TABLE dbo.Ind_Unidades (
        UnidadID  INT IDENTITY(1,1) NOT NULL,
        MateriaID INT NOT NULL,
        Nombre    NVARCHAR(255) NOT NULL,
        Orden     INT NOT NULL,
        CONSTRAINT PK_IndUnidades PRIMARY KEY CLUSTERED (UnidadID ASC),
        CONSTRAINT FK_IndUnidades_Materias FOREIGN KEY (MateriaID) REFERENCES dbo.Ind_Materias (MateriaID)
    );
END
GO

-- 1.9 Ind_Materiales (recursos educativos) --------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_Materiales')
BEGIN
    CREATE TABLE dbo.Ind_Materiales (
        MaterialID  INT IDENTITY(1,1) NOT NULL,
        UnidadID    INT NOT NULL,
        Nombre      NVARCHAR(255) NOT NULL,
        TipoRecurso NVARCHAR(50) NOT NULL,
        RutaURL     NVARCHAR(MAX) NOT NULL,
        Orden       INT NOT NULL CONSTRAINT DF_IndMateriales_Orden DEFAULT (0),
        CONSTRAINT PK_IndMateriales PRIMARY KEY CLUSTERED (MaterialID ASC),
        CONSTRAINT FK_IndMateriales_Unidades FOREIGN KEY (UnidadID) REFERENCES dbo.Ind_Unidades (UnidadID)
    );
END
GO

-- 1.10 Ind_Entregables (definición de tareas/archivos a subir) ---------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_Entregables')
BEGIN
    CREATE TABLE dbo.Ind_Entregables (
        EntregableID  INT IDENTITY(1,1) NOT NULL,
        UnidadID      INT NOT NULL,
        Titulo        NVARCHAR(255) NOT NULL,
        Instrucciones NVARCHAR(MAX) NULL,
        FechaLimite   DATETIME NULL,
        Activo        BIT NOT NULL CONSTRAINT DF_IndEntregables_Activo DEFAULT (1),
        Orden         INT NOT NULL CONSTRAINT DF_IndEntregables_Orden DEFAULT (0),
        CONSTRAINT PK_IndEntregables PRIMARY KEY CLUSTERED (EntregableID ASC),
        CONSTRAINT FK_IndEntregables_Unidades FOREIGN KEY (UnidadID) REFERENCES dbo.Ind_Unidades (UnidadID)
    );
END
GO

-- 1.11 Ind_ProgresoAspirante (flujo de "marcar unidad como revisada") -------
-- AspiranteID apunta DIRECTO a Usuarios.UsuarioID (ya no existe una tabla
-- Aspirantes aparte); quién es aspirante se determina por Usuarios.RolID.
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
        CONSTRAINT PK_IndProgresoAspirante PRIMARY KEY CLUSTERED (ProgresoID ASC),
        CONSTRAINT FK_IndProgreso_Usuarios FOREIGN KEY (AspiranteID) REFERENCES dbo.Usuarios (UsuarioID),
        CONSTRAINT FK_IndProgreso_Unidades FOREIGN KEY (UnidadID) REFERENCES dbo.Ind_Unidades (UnidadID),
        CONSTRAINT FK_IndProgreso_UsuarioCalificador FOREIGN KEY (UsuarioCalificadorID) REFERENCES dbo.Usuarios (UsuarioID)
    );
END
GO

-- 1.12 TiposDocumentos ------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TiposDocumentos')
BEGIN
    CREATE TABLE dbo.TiposDocumentos (
        TipoDocumentoID INT IDENTITY(1,1) NOT NULL,
        Nombre          NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_TiposDocumentos PRIMARY KEY CLUSTERED (TipoDocumentoID ASC)
    );
END
GO

-- 1.13 EstadosDocumentos -----------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EstadosDocumentos')
BEGIN
    CREATE TABLE dbo.EstadosDocumentos (
        EstadoDocumentoID INT IDENTITY(1,1) NOT NULL,
        Nombre            NVARCHAR(20) NULL,
        CONSTRAINT PK_EstadosDocumentos PRIMARY KEY CLUSTERED (EstadoDocumentoID ASC)
    );
END
GO

-- 1.14 Documentos ---------------------------------------------------------------
-- Repositorio físico/versionado de archivos subidos; cada Ind_Submision
-- opcionalmente apunta a un Documento (ver DocumentoHelper.cs).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Documentos')
BEGIN
    CREATE TABLE dbo.Documentos (
        DocumentoID         INT IDENTITY(1,1) NOT NULL,
        NombreOriginal      NVARCHAR(255) NOT NULL,
        ExtensionArchivo    NVARCHAR(10) NOT NULL,
        TipoMIME            NVARCHAR(100) NOT NULL,
        TamanoArchivoBytes  INT NOT NULL,
        RutaAlmacenamiento  NVARCHAR(500) NOT NULL,
        HashArchivo         NVARCHAR(64) NOT NULL,
        FechaSubida         DATETIME NOT NULL CONSTRAINT DF_Documentos_FechaSubida DEFAULT (GETDATE()),
        FechaRevision       DATETIME NULL,
        NumeroVersion       INT NOT NULL CONSTRAINT DF_Documentos_NumeroVersion DEFAULT (1),
        VersionActual       BIT NOT NULL CONSTRAINT DF_Documentos_VersionActual DEFAULT (1),
        -- Apunta directo a Usuarios.UsuarioID (de quién es el documento).
        AspiranteID         INT NOT NULL,
        TipoDocumentoID     INT NOT NULL,
        EstadoDocumentoID   INT NOT NULL,
        -- Quién lo subió (puede ser el propio aspirante o, en otros flujos, un
        -- Coordinador/Maestro/Admin); NULL si no aplica.
        UsuarioID           INT NULL,
        CONSTRAINT PK_Documentos PRIMARY KEY CLUSTERED (DocumentoID ASC),
        CONSTRAINT FK_Documentos_Usuarios FOREIGN KEY (AspiranteID) REFERENCES dbo.Usuarios (UsuarioID),
        CONSTRAINT FK_Documentos_TiposDocumentos FOREIGN KEY (TipoDocumentoID) REFERENCES dbo.TiposDocumentos (TipoDocumentoID),
        CONSTRAINT FK_Documentos_EstadosDocumentos FOREIGN KEY (EstadoDocumentoID) REFERENCES dbo.EstadosDocumentos (EstadoDocumentoID),
        CONSTRAINT FK_Documentos_UsuariosSubio FOREIGN KEY (UsuarioID) REFERENCES dbo.Usuarios (UsuarioID)
    );
END
GO

-- 1.15 Ind_Submisiones (archivos entregados por los aspirantes) --------------
-- AspiranteID apunta DIRECTO a Usuarios.UsuarioID, igual que en
-- Ind_ProgresoAspirante.
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
        CONSTRAINT CK_IndSubmisiones_Estado CHECK (Estado IN ('Pendiente', 'Revisado', 'Rechazado')),
        CONSTRAINT FK_IndSubmisiones_Usuarios FOREIGN KEY (AspiranteID) REFERENCES dbo.Usuarios (UsuarioID),
        CONSTRAINT FK_IndSubmisiones_Entregables FOREIGN KEY (EntregableID) REFERENCES dbo.Ind_Entregables (EntregableID),
        CONSTRAINT FK_IndSubmisiones_UsuarioRevisor FOREIGN KEY (UsuarioRevisorID) REFERENCES dbo.Usuarios (UsuarioID),
        CONSTRAINT FK_IndSubmisiones_Documentos FOREIGN KEY (DocumentoID) REFERENCES dbo.Documentos (DocumentoID)
    );
END
GO

-- 1.15b Ind_FelicitacionesVistas ------------------------------------------------
-- Registra qué materias completadas ya vio (y descartó permanentemente) cada
-- aspirante en la pantalla de "¡Felicidades!", para no volver a mostrársela.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_FelicitacionesVistas')
BEGIN
    CREATE TABLE dbo.Ind_FelicitacionesVistas (
        AspiranteID INT NOT NULL,
        MateriaID   INT NOT NULL,
        FechaVista  DATETIME NOT NULL CONSTRAINT DF_IndFelicitacionesVistas_FechaVista DEFAULT (GETDATE()),
        CONSTRAINT PK_IndFelicitacionesVistas PRIMARY KEY (AspiranteID, MateriaID),
        CONSTRAINT FK_IndFelicitacionesVistas_Usuarios FOREIGN KEY (AspiranteID) REFERENCES dbo.Usuarios (UsuarioID),
        CONSTRAINT FK_IndFelicitacionesVistas_Materias FOREIGN KEY (MateriaID) REFERENCES dbo.Ind_Materias (MateriaID)
    );
END
GO

-- 1.16 Ind_Permisos (catálogo de secciones/funciones del sistema) ----------
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

-- 1.17 Ind_RolPermisos (qué puede hacer cada rol en cada sección) -----------
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

-- 1.18 Ind_UsuarioPermisos (excepciones por usuario específico, sobre su rol) -
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

-- 1.19 Ind_MateriaCarreras (materia <-> carrera, muchos a muchos) --------------
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

-- 1.20 Ind_UsuarioCarreras (usuario <-> carrera, muchos a muchos) --------------
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

DECLARE @TotalTablas INT = (SELECT COUNT(*) FROM sys.tables);
PRINT 'PARTE 1 completa: esquema de InduccionDB listo (' + CAST(@TotalTablas AS VARCHAR) + ' tablas).';
GO


-- ============================================================================
-- PARTE 2: DATOS DE PRUEBA
-- ============================================================================
-- Mapeo de roles (fijo, coincide con las constantes hardcodeadas en el código
-- C# - ver Helpers/CarreraScopeHelper.cs): 1 = Administrador, 2 = Director
-- (sin acceso, se conserva el hueco por compatibilidad con el esquema
-- original), 3 = Coordinador, 4 = Aspirante, 5 = Maestro.
-- ============================================================================

-- 2.1 Roles ---------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
BEGIN
    INSERT INTO dbo.Roles (Nombre, Activo) VALUES
    ('Administrador', 1),  -- RolID 1
    ('Director', 0),       -- RolID 2 (legado, AccountController bloquea el login)
    ('Coordinador', 1),    -- RolID 3
    ('Aspirante', 1),      -- RolID 4
    ('Maestro', 1);        -- RolID 5
END
GO

-- 2.2 Ind_Permisos (catálogo de las 11 secciones actuales) ------------------
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

-- 2.3 Ind_RolPermisos: reproduce el comportamiento actual del código --------
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
    (3, 'MisMaestros',        1, 1, 0, 0),
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

-- 2.4 TiposCarreras, Ind_Areas, Carreras, Periodos --------------------------
INSERT INTO dbo.TiposCarreras (Nombre)
SELECT v.Nombre FROM (VALUES ('TSU'), ('Ingeniería')) AS v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TiposCarreras t WHERE t.Nombre = v.Nombre);
GO

INSERT INTO dbo.Ind_Areas (Nombre, Activo)
SELECT v.Nombre, 1 FROM (VALUES ('Tecnologías de la Información'), ('Administración')) AS v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Ind_Areas a WHERE a.Nombre = v.Nombre);
GO

INSERT INTO dbo.Carreras (Nombre, Nomenclatura, TipoCarreraID, Activo, AreaID)
SELECT v.Nombre, v.Nomenclatura, tc.TipoCarreraID, 1, a.AreaID
FROM (VALUES
    ('TSU en Desarrollo de Software Multiplataforma',      'DSM', 'TSU',        'Tecnologías de la Información'),
    ('Ingeniería en Tecnologías de la Información',        'ITI', 'Ingeniería', 'Tecnologías de la Información'),
    ('TSU en Gestión de Capital Humano',                   'GCH', 'TSU',        'Administración')
) AS v(Nombre, Nomenclatura, TipoCarreraNombre, AreaNombre)
INNER JOIN dbo.TiposCarreras tc ON tc.Nombre = v.TipoCarreraNombre
INNER JOIN dbo.Ind_Areas a ON a.Nombre = v.AreaNombre
WHERE NOT EXISTS (SELECT 1 FROM dbo.Carreras c WHERE c.Nombre = v.Nombre);
GO

-- Un Periodo activo que cubra todo el año en curso, para poder crear
-- materias de prueba sin importar en qué fecha se corra este script.
IF NOT EXISTS (SELECT 1 FROM dbo.Periodos WHERE Activo = 1)
BEGIN
    INSERT INTO dbo.Periodos (FechaInicio, FechaFin, Activo)
    VALUES (DATEFROMPARTS(YEAR(GETDATE()), 1, 1), DATEFROMPARTS(YEAR(GETDATE()), 12, 31), 1);
END
GO

-- 2.5 TiposDocumentos / EstadosDocumentos -----------------------------------
-- No es estrictamente necesario (DocumentoHelper los crea sobre la marcha con
-- GetOrCreate...), pero se dejan precargados para que Documentos tenga
-- catálogos limpios desde el primer uso.
INSERT INTO dbo.TiposDocumentos (Nombre)
SELECT 'Entregable de Inducción'
WHERE NOT EXISTS (SELECT 1 FROM dbo.TiposDocumentos WHERE Nombre = 'Entregable de Inducción');
GO

INSERT INTO dbo.EstadosDocumentos (Nombre)
SELECT v.Nombre FROM (VALUES ('Pendiente'), ('Aprobado'), ('Rechazado')) AS v(Nombre)
WHERE NOT EXISTS (SELECT 1 FROM dbo.EstadosDocumentos e WHERE e.Nombre = v.Nombre);
GO

-- 2.6 Un usuario de prueba por rol ------------------------------------------
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

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE CorreoElectronico = 'maestro@test.com')
    INSERT INTO dbo.Usuarios (Nombre, ApellidoPaterno, ApellidoMaterno, NombreUsuario, CorreoElectronico, Contrasena, Activo, FechaRegistro, RolID)
    VALUES ('Juan', 'Pérez', 'UTTN', 'maestro01', 'maestro@test.com', 'Password123!', 1, GETDATE(), 5);
ELSE
    UPDATE dbo.Usuarios SET Contrasena = 'Password123!', RolID = 5, Activo = 1 WHERE CorreoElectronico = 'maestro@test.com';

-- El aspirante de prueba usa el primer folio de la secuencia (10 dígitos con
-- ceros a la izquierda), igual que generaría GenerarSiguienteFolio() en una
-- base nueva sin ningún folio real todavía.
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE CorreoElectronico = 'aspirante@test.com')
    INSERT INTO dbo.Usuarios (Nombre, ApellidoPaterno, ApellidoMaterno, NombreUsuario, CorreoElectronico, Contrasena, Activo, FechaRegistro, RolID)
    VALUES ('Daniela', 'Barrientos', 'Torres', '0000000001', 'aspirante@test.com', 'Password123!', 1, GETDATE(), 4);
ELSE
    UPDATE dbo.Usuarios SET Contrasena = 'Password123!', RolID = 4, Activo = 1 WHERE CorreoElectronico = 'aspirante@test.com';
GO

-- 2.7 Asignar una carrera de ejemplo a Coordinador, Maestro y Aspirante -----
DECLARE @CarreraEjemploID INT = (SELECT TOP 1 CarreraID FROM dbo.Carreras ORDER BY CarreraID ASC);
DECLARE @UsuarioCoordinadorID INT = (SELECT UsuarioID FROM dbo.Usuarios WHERE CorreoElectronico = 'coordinador@test.com');
DECLARE @UsuarioMaestroID INT = (SELECT UsuarioID FROM dbo.Usuarios WHERE CorreoElectronico = 'maestro@test.com');
DECLARE @UsuarioAspiranteID INT = (SELECT UsuarioID FROM dbo.Usuarios WHERE CorreoElectronico = 'aspirante@test.com');

IF @CarreraEjemploID IS NOT NULL
BEGIN
    IF @UsuarioCoordinadorID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Ind_UsuarioCarreras WHERE UsuarioID = @UsuarioCoordinadorID)
        INSERT INTO dbo.Ind_UsuarioCarreras (UsuarioID, CarreraID) VALUES (@UsuarioCoordinadorID, @CarreraEjemploID);

    IF @UsuarioMaestroID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Ind_UsuarioCarreras WHERE UsuarioID = @UsuarioMaestroID)
        INSERT INTO dbo.Ind_UsuarioCarreras (UsuarioID, CarreraID) VALUES (@UsuarioMaestroID, @CarreraEjemploID);

    IF @UsuarioAspiranteID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Ind_UsuarioCarreras WHERE UsuarioID = @UsuarioAspiranteID)
        INSERT INTO dbo.Ind_UsuarioCarreras (UsuarioID, CarreraID) VALUES (@UsuarioAspiranteID, @CarreraEjemploID);
END

PRINT 'Usuarios de prueba (Admin, Coordinador, Maestro, Aspirante) listos.';
GO

-- 2.8 Limpieza de contenido de ejemplo insertado por corridas anteriores ----
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

-- 2.9 Materias, unidades y materiales de ejemplo ----------------------------
DECLARE @MateriaID1 INT, @MateriaID2 INT, @MateriaID3 INT, @PeriodoID INT;
DECLARE @UsuarioCoordinadorID INT = (SELECT UsuarioID FROM dbo.Usuarios WHERE CorreoElectronico = 'coordinador@test.com');
DECLARE @UsuarioAspiranteID INT = (SELECT UsuarioID FROM dbo.Usuarios WHERE CorreoElectronico = 'aspirante@test.com');

SELECT TOP 1 @PeriodoID = PeriodoID FROM dbo.Periodos WHERE Activo = 1 ORDER BY PeriodoID ASC;

-- Todas marcadas como "visibles para todas las carreras" para simplificar los
-- datos de prueba (ver Ind_Materias.TodasLasCarreras / Ind_MateriaCarreras).
INSERT INTO dbo.Ind_Materias (PeriodoID, Nombre, Descripcion, Activo, TodasLasCarreras)
VALUES (@PeriodoID, 'Introducción a la UTTN', 'Historia, misión, visión y servicios estudiantiles.', 1, 1);
SET @MateriaID1 = SCOPE_IDENTITY();

INSERT INTO dbo.Ind_Materias (PeriodoID, Nombre, Descripcion, Activo, TodasLasCarreras)
VALUES (@PeriodoID, 'Nivelación Académica', 'Reforzamiento de matemáticas básicas y comprensión lectora.', 1, 1);
SET @MateriaID2 = SCOPE_IDENTITY();

INSERT INTO dbo.Ind_Materias (PeriodoID, Nombre, Descripcion, Activo, TodasLasCarreras)
VALUES (@PeriodoID, 'Desarrollo Socioemocional', 'Habilidades blandas y adaptabilidad universitaria.', 1, 1);
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

-- Un entregable de ejemplo en la primera unidad, para poder probar el flujo de subida.
INSERT INTO dbo.Ind_Entregables (UnidadID, Titulo, Instrucciones, FechaLimite, Activo, Orden)
VALUES (@UnidadID1, 'Prueba de Entregable', 'Sube cualquier documento en PDF como prueba del flujo de entrega.', DATEADD(DAY, 14, GETDATE()), 1, 1);

-- 2.10 Progreso de ejemplo para el aspirante de pruebas ----------------------
IF @UsuarioAspiranteID IS NOT NULL
BEGIN
    INSERT INTO dbo.Ind_ProgresoAspirante (AspiranteID, UnidadID, Estado, FechaAsignacion)
    VALUES
    (@UsuarioAspiranteID, @UnidadID1, 'Asignado', GETDATE()),
    (@UsuarioAspiranteID, @UnidadID2, 'Asignado', GETDATE()),
    (@UsuarioAspiranteID, @UnidadID3, 'Asignado', GETDATE()),
    (@UsuarioAspiranteID, @UnidadID4, 'Asignado', GETDATE()),
    (@UsuarioAspiranteID, @UnidadID5, 'Asignado', GETDATE());

    UPDATE dbo.Ind_ProgresoAspirante
    SET Estado = 'Revisado', FechaEnvio = GETDATE(), FechaRevision = GETDATE(), ComentariosEvaluador = 'Excelente trabajo inicial.', UsuarioCalificadorID = @UsuarioCoordinadorID
    WHERE AspiranteID = @UsuarioAspiranteID AND UnidadID = @UnidadID1;

    UPDATE dbo.Ind_ProgresoAspirante
    SET Estado = 'Revisado', FechaEnvio = GETDATE(), FechaRevision = GETDATE(), ComentariosEvaluador = 'Buen desempeño en la evaluación.', UsuarioCalificadorID = @UsuarioCoordinadorID
    WHERE AspiranteID = @UsuarioAspiranteID AND UnidadID = @UnidadID2;
END

PRINT 'Materias, unidades, materiales y progreso de ejemplo cargados.';
GO


-- ============================================================================
-- VERIFICACIÓN FINAL
-- ============================================================================
PRINT '========================================';
PRINT 'INDUCCIONDB CREADA. CREDENCIALES DE PRUEBA:';
PRINT '(la contraseña se convierte a hash automáticamente al iniciar sesión)';
PRINT '========================================';
SELECT NombreUsuario, CorreoElectronico, 'Password123!' AS ContrasenaInicial, R.Nombre AS RolAsignado
FROM dbo.Usuarios U
INNER JOIN dbo.Roles R ON U.RolID = R.RolID
WHERE CorreoElectronico IN ('aspirante@test.com', 'coordinador@test.com', 'maestro@test.com', 'admin@test.com');
GO
