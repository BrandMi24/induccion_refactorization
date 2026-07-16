-- ============================================================================
-- SISTEMA DE INDUCCIÓN - SCRIPT ÚNICO DE INSTALACIÓN
-- Base de datos: CaptacionDB
-- ============================================================================
-- Este script reemplaza y consolida todo lo que antes estaba repartido en:
--   Phase6_Entregables_Submisiones.sql, Phase7_DocumentosIntegration.sql,
--   Phase8_MultiCarreraMaterias.sql y SeedInductionData.sql
-- (esos cuatro archivos ya no son necesarios y pueden borrarse).
--
-- Qué hace:
--   PARTE 1 - Esquema: crea las tablas del módulo de inducción (Ind_Materias,
--             Ind_Unidades, Ind_Materiales, Ind_ProgresoAspirante,
--             Ind_Entregables, Ind_Submisiones, Ind_MateriaCarreras) y su
--             integración con la tabla Documentos ya existente en CaptacionDB.
--   PARTE 2 - Datos de prueba: crea un usuario de cada rol (Admin, Director,
--             Coordinador, Aspirante) y algunas materias/unidades/materiales
--             de ejemplo para poder probar la aplicación de inmediato.
--
-- Requisitos previos: la base CaptacionDB ya debe existir con su esquema
-- original (Usuarios, Roles, Aspirantes, Carreras, Periodos, Documentos,
-- TiposDocumentos, EstadosDocumentos, etc. - ver Databasenew.sql).
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

-- 1.5 Ind_ProgresoAspirante (flujo simple de "marcar unidad como hecha") ------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_ProgresoAspirante')
BEGIN
    CREATE TABLE dbo.Ind_ProgresoAspirante (
        ProgresoID           INT IDENTITY(1,1) NOT NULL,
        AspiranteID          INT NOT NULL,
        UnidadID             INT NOT NULL,
        Estado               NVARCHAR(50) NOT NULL CONSTRAINT DF_IndProgreso_Estado DEFAULT ('Asignado'),
        Calificacion         DECIMAL(5, 2) NULL,
        FechaAsignacion      DATETIME NOT NULL CONSTRAINT DF_IndProgreso_FechaAsignacion DEFAULT (GETDATE()),
        FechaEnvio           DATETIME NULL,
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

-- 1.6 Ind_Entregables (definición de tareas/archivos a subir) -----------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Ind_Entregables')
BEGIN
    CREATE TABLE dbo.Ind_Entregables (
        EntregableID   INT IDENTITY(1,1) NOT NULL,
        UnidadID       INT NOT NULL,
        Titulo         NVARCHAR(255) NOT NULL,
        Instrucciones  NVARCHAR(MAX) NULL,
        FechaLimite    DATETIME NULL,
        PonderacionMax DECIMAL(5, 2) NOT NULL CONSTRAINT DF_IndEntregables_PonderacionMax DEFAULT (100),
        Activo         BIT NOT NULL CONSTRAINT DF_IndEntregables_Activo DEFAULT (1),
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
        Calificacion      DECIMAL(5, 2) NULL,
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

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE CorreoElectronico = 'director@test.com')
    INSERT INTO dbo.Usuarios (Nombre, ApellidoPaterno, ApellidoMaterno, NombreUsuario, CorreoElectronico, Contrasena, Activo, FechaRegistro, RolID)
    VALUES ('Director', 'Academico', 'UTTN', 'director01', 'director@test.com', 'Password123!', 1, GETDATE(), 2);
ELSE
    UPDATE dbo.Usuarios SET Contrasena = 'Password123!', RolID = 2, Activo = 1 WHERE CorreoElectronico = 'director@test.com';

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE CorreoElectronico = 'coordinador@test.com')
    INSERT INTO dbo.Usuarios (Nombre, ApellidoPaterno, ApellidoMaterno, NombreUsuario, CorreoElectronico, Contrasena, Activo, FechaRegistro, RolID)
    VALUES ('Sarah', 'Connor', 'Smith', 'coordinador01', 'coordinador@test.com', 'Password123!', 1, GETDATE(), 3);
ELSE
    UPDATE dbo.Usuarios SET Contrasena = 'Password123!', RolID = 3, Activo = 1 WHERE CorreoElectronico = 'coordinador@test.com';

PRINT 'Usuarios de prueba (Admin, Director, Coordinador, Aspirante) listos.';

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

INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL) VALUES (@UnidadID1, 'Historia de la UTTN - PDF', 'PDF', 'https://www.uttn.edu.mx/docs/historia_uttn.pdf');
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL) VALUES (@UnidadID1, 'Video Institucional Misión', 'Video', 'https://www.youtube.com/watch?v=example1');
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL) VALUES (@UnidadID2, 'Guía de Servicios Escolares', 'PDF', 'https://www.uttn.edu.mx/docs/servicios.pdf');
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL) VALUES (@UnidadID3, 'Reglamento General de Alumnos', 'PDF', 'https://www.uttn.edu.mx/docs/reglamento.pdf');
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL) VALUES (@UnidadID4, 'Manual de Álgebra Básica', 'PDF', 'https://www.uttn.edu.mx/docs/algebra.pdf');

-- Un entregable de ejemplo en la primera unidad, para poder probar el flujo de subida
INSERT INTO dbo.Ind_Entregables (UnidadID, Titulo, Instrucciones, FechaLimite, PonderacionMax, Activo)
VALUES (@UnidadID1, 'Prueba de Entregable', 'Sube cualquier documento en PDF como prueba del flujo de entrega.', DATEADD(DAY, 14, GETDATE()), 100, 1);

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
    SET Estado = 'Calificado', Calificacion = 95.00, FechaEnvio = GETDATE(), ComentariosEvaluador = 'Excelente trabajo inicial.'
    WHERE AspiranteID = @TargetAspiranteID AND UnidadID = @UnidadID1;

    UPDATE dbo.Ind_ProgresoAspirante
    SET Estado = 'Calificado', Calificacion = 88.00, FechaEnvio = GETDATE(), ComentariosEvaluador = 'Buen desempeño en la evaluación.'
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
WHERE CorreoElectronico IN ('aspirante@test.com', 'coordinador@test.com', 'director@test.com', 'admin@test.com');
GO
