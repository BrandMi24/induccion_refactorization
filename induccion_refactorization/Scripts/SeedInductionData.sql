-- ============================================================
-- SEED SCRIPT DEFINITIVO V5.1: Sincronización Completa de 4 Roles
-- Base de datos: CaptacionDB
-- Mapeo Real: 1=Admin, 2=Director, 3=Coordinador, 4=Aspirante
-- ============================================================

USE CaptacionDB;
GO

-- ============================================================
-- 1. LIMPIEZA DE NUESTRAS TABLAS DE INDUCCIÓN
-- ============================================================
DELETE FROM dbo.Ind_ProgresoAspirante;
DELETE FROM dbo.Ind_Materiales;
DELETE FROM dbo.Ind_Unidades;
DELETE FROM dbo.Ind_Materias;

IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.Ind_Materias'))
    DBCC CHECKIDENT ('dbo.Ind_Materias', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.Ind_Unidades'))
    DBCC CHECKIDENT ('dbo.Ind_Unidades', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.Ind_Materiales'))
    DBCC CHECKIDENT ('dbo.Ind_Materiales', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('dbo.Ind_ProgresoAspirante'))
    DBCC CHECKIDENT ('dbo.Ind_ProgresoAspirante', RESEED, 0);

-- ============================================================
-- 2. ASIGNAR/CONFIGURAR EL ASPIRANTE DE PRUEBAS (RolID = 4)
-- ============================================================
DECLARE @TargetAspiranteID INT;
DECLARE @RealAspiranteUserID INT;

-- Reutilizamos el Aspirante válido del respaldo para heredar sus llaves foráneas complejas
SELECT TOP 1 @TargetAspiranteID = AspiranteID, @RealAspiranteUserID = UsuarioID 
FROM dbo.Aspirantes 
ORDER BY AspiranteID ASC;

IF @TargetAspiranteID IS NOT NULL
BEGIN
    -- Forzamos a que este usuario use el RolID = 4 (Aspirante Real)
    UPDATE dbo.Usuarios 
    SET CorreoElectronico = 'aspirante@test.com', 
        Contrasena = 'Password123!',
        RolID = 4, -- 4 es Aspirante en tu BD
        Activo = 1
    WHERE UsuarioID = @RealAspiranteUserID;
    
    PRINT '¡Perfecto! Al AspiranteID ' + CAST(@TargetAspiranteID AS VARCHAR) + ' se le asignó el RolID 4 (Aspirante) y correo aspirante@test.com';
END

-- ============================================================
-- 3. CREACIÓN / CONFIGURACIÓN DE LOS OTROS 3 ROLES ADMINISTRATIVOS
-- ============================================================

-- RolID = 1: Administrador
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE CorreoElectronico = 'admin@test.com')
    INSERT INTO dbo.Usuarios (Nombre, ApellidoPaterno, ApellidoMaterno, NombreUsuario, CorreoElectronico, Contrasena, Activo, FechaRegistro, RolID)
    VALUES ('Admin', 'Sistemas', 'UTTN', 'admin01', 'admin@test.com', 'Password123!', 1, GETDATE(), 1);
ELSE
    UPDATE dbo.Usuarios SET Contrasena = 'Password123!', RolID = 1, Activo = 1 WHERE CorreoElectronico = 'admin@test.com';

-- RolID = 2: Director
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE CorreoElectronico = 'director@test.com')
    INSERT INTO dbo.Usuarios (Nombre, ApellidoPaterno, ApellidoMaterno, NombreUsuario, CorreoElectronico, Contrasena, Activo, FechaRegistro, RolID)
    VALUES ('Director', 'Academico', 'UTTN', 'director01', 'director@test.com', 'Password123!', 1, GETDATE(), 2);
ELSE
    UPDATE dbo.Usuarios SET Contrasena = 'Password123!', RolID = 2, Activo = 1 WHERE CorreoElectronico = 'director@test.com';

-- RolID = 3: Coordinador
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE CorreoElectronico = 'coordinador@test.com')
    INSERT INTO dbo.Usuarios (Nombre, ApellidoPaterno, ApellidoMaterno, NombreUsuario, CorreoElectronico, Contrasena, Activo, FechaRegistro, RolID)
    VALUES ('Sarah', 'Connor', 'Smith', 'coordinador01', 'coordinador@test.com', 'Password123!', 1, GETDATE(), 3);
ELSE
    UPDATE dbo.Usuarios SET Contrasena = 'Password123!', RolID = 3, Activo = 1 WHERE CorreoElectronico = 'coordinador@test.com';

PRINT 'Usuarios Administrativos (Admin, Director y Coordinador) mapeados correctamente.';

-- ============================================================
-- 4. INSERCIÓN DINÁMICA DE MATERIAS Y CAPTURA DE ARRAYS DE IDS
-- ============================================================
DECLARE @MateriaID1 INT, @MateriaID2 INT, @MateriaID3 INT;

INSERT INTO dbo.Ind_Materias (CarreraID, PeriodoID, Nombre, Descripcion, Activo)
VALUES (1, 1, 'Introducción a la UTTN', 'Historia, misión, visión y servicios estudiantiles.', 1);
SET @MateriaID1 = SCOPE_IDENTITY();

INSERT INTO dbo.Ind_Materias (CarreraID, PeriodoID, Nombre, Descripcion, Activo)
VALUES (1, 1, 'Nivelación Académica', 'Reforzamiento de matemáticas básicas y comprensión lectora.', 1);
SET @MateriaID2 = SCOPE_IDENTITY();

INSERT INTO dbo.Ind_Materias (CarreraID, PeriodoID, Nombre, Descripcion, Activo)
VALUES (1, 1, 'Desarrollo Socioemocionales', 'Habilidades blandas y adaptabilidad universitaria.', 1);
SET @MateriaID3 = SCOPE_IDENTITY();

-- ============================================================
-- 5. INSERCIÓN DE UNIDADES USANDO LOS IDS CAPTURADOS
-- ============================================================
DECLARE @UnidadID1 INT, @UnidadID2 INT, @UnidadID3 INT, @UnidadID4 INT, @UnidadID5 INT;

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
INSERT INTO dbo.Ind_Unidades (MateriaID, Nombre, Orden) VALUES (@MateriaID3, 'Trabajo Colaborativo', 2);
INSERT INTO dbo.Ind_Unidades (MateriaID, Nombre, Orden) VALUES (@MateriaID3, 'Gestión del Tiempo', 3);

-- ============================================================
-- 6. INSERCIÓN DE MATERIALES EDUCATIVOS
-- ============================================================
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL) VALUES (@UnidadID1, 'Historia de la UTTN - PDF', 'PDF', 'https://www.uttn.edu.mx/docs/historia_uttn.pdf');
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL) VALUES (@UnidadID1, 'Video Institucional Misión', 'Video', 'https://www.youtube.com/watch?v=example1');
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL) VALUES (@UnidadID2, 'Guía de Servicios Escolares', 'PDF', 'https://www.uttn.edu.mx/docs/servicios.pdf');
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL) VALUES (@UnidadID3, 'Reglamento General de Alumnos', 'PDF', 'https://www.uttn.edu.mx/docs/reglamento.pdf');
INSERT INTO dbo.Ind_Materiales (UnidadID, Nombre, TipoRecurso, RutaURL) VALUES (@UnidadID4, 'Manual de Álgebra Básica', 'PDF', 'https://www.uttn.edu.mx/docs/algebra.pdf');

-- ============================================================
-- 7. ASIGNACIÓN DE PROGRESO AL ALUMNO REAL (RolID = 4)
-- ============================================================
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

    PRINT 'Métricas de progreso y avance cargadas al estudiante de pruebas.';
END
GO

-- ============================================================
-- 8. AUDITORÍA FINAL (Corregida con corchetes en el alias)
-- ============================================================
PRINT '========================================';
PRINT 'COMPROBACIÓN DE CREDENCIALES DISPONIBLES:';
PRINT '========================================';
SELECT NombreUsuario, CorreoElectronico, 'Password123!' AS Contrasena, R.Nombre AS [Rol Asignado]
FROM dbo.Usuarios U
INNER JOIN dbo.Roles R ON U.RolID = R.RolID
WHERE CorreoElectronico IN ('aspirante@test.com', 'coordinador@test.com', 'director@test.com', 'admin@test.com');