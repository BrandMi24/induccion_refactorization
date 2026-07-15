Here is a comprehensive database-focused analysis and the T-SQL design to support the Induction module seamlessly integrated into your existing CaptacionDB.

1. New Table Design for the Induction Module

This normalized design ties directly to your existing core tables (Carreras, Periodos, Aspirantes, and Usuarios), guaranteeing accurate structural integrity.



-- Created by GitHub Copilot in SSMS - review carefully before executing



-- 1. Ind_Materias: Induction Subjects tied to Careers and Periods

CREATE TABLE dbo.Ind_Materias (

    MateriaID INT IDENTITY(1,1) NOT NULL,

    CarreraID INT NOT NULL,

    PeriodoID INT NOT NULL,

    Nombre NVARCHAR(255) NOT NULL,

    Descripcion NVARCHAR(MAX) NULL,

    Activo BIT NOT NULL CONSTRAINT DF_IndMaterias_Activo DEFAULT 1,

    CONSTRAINT PK_IndMaterias PRIMARY KEY CLUSTERED (MateriaID),

    CONSTRAINT FK_IndMaterias_Carreras FOREIGN KEY (CarreraID) REFERENCES dbo.Carreras(CarreraID),

    CONSTRAINT FK_IndMaterias_Periodos FOREIGN KEY (PeriodoID) REFERENCES dbo.Periodos(PeriodoID)

);



-- 2. Ind_Unidades: Modular breakdown of each Subject

CREATE TABLE dbo.Ind_Unidades (

    UnidadID INT IDENTITY(1,1) NOT NULL,

    MateriaID INT NOT NULL,

    Nombre NVARCHAR(255) NOT NULL,

    Orden INT NOT NULL CONSTRAINT DF_IndUnidades_Orden DEFAULT 1,

    CONSTRAINT PK_IndUnidades PRIMARY KEY CLUSTERED (UnidadID),

    CONSTRAINT FK_IndUnidades_Materias FOREIGN KEY (MateriaID) REFERENCES dbo.Ind_Materias(MateriaID)

);



-- 3. Ind_Materiales: Content resources for each Unit

CREATE TABLE dbo.Ind_Materiales (

    MaterialID INT IDENTITY(1,1) NOT NULL,

    UnidadID INT NOT NULL,

    Nombre NVARCHAR(255) NOT NULL,

    TipoRecurso NVARCHAR(50) NOT NULL, -- e.g., 'PDF', 'Video', 'Enlace'

    RutaURL NVARCHAR(MAX) NOT NULL,

    CONSTRAINT PK_IndMateriales PRIMARY KEY CLUSTERED (MaterialID),

    CONSTRAINT FK_IndMateriales_Unidades FOREIGN KEY (UnidadID) REFERENCES dbo.Ind_Unidades(UnidadID)

);



-- 4. Ind_ProgresoAspirantes: Critical Pivot tracking Assignments, Submissions, and Grades

CREATE TABLE dbo.Ind_ProgresoAspirantes (

    ProgresoID INT IDENTITY(1,1) NOT NULL,

    AspiranteID INT NOT NULL,

    UnidadID INT NOT NULL,

    Estado NVARCHAR(50) NOT NULL CONSTRAINT DF_ProgresoAspirantes_Estado DEFAULT 'Asignado', -- Options: Asignado, Entregado, Calificado

    Calificacion DECIMAL(5,2) NULL,

    FechaAsignacion DATETIME NOT NULL CONSTRAINT DF_Progreso_FechaAsignacion DEFAULT GETDATE(),

    FechaEnvio DATETIME NULL,

    UsuarioCalificadorID INT NULL, 

    ComentariosEvaluador NVARCHAR(MAX) NULL,

    CONSTRAINT PK_IndProgresoAspirantes PRIMARY KEY CLUSTERED (ProgresoID),

    CONSTRAINT FK_Progreso_Aspirantes FOREIGN KEY (AspiranteID) REFERENCES dbo.Aspirantes(AspiranteID),

    CONSTRAINT FK_Progreso_Unidades FOREIGN KEY (UnidadID) REFERENCES dbo.Ind_Unidades(UnidadID),

    CONSTRAINT FK_Progreso_UsuarioCalificador FOREIGN KEY (UsuarioCalificadorID) REFERENCES dbo.Usuarios(UsuarioID)

);



2. Mapping and Security for the 4 Roles

In your ASP.NET MVC 5 application, authorization scope must be rigorously checked by cross-referencing dbo.Usuarios and dbo.Roles. The relational structure leverages Foreign Keys to enforce context.

Here is the strategy mapped via SQL JOIN constraints:

• Administrador (Rol 1): Unrestricted access. Allowed to alter Ind_Materias and Ind_Unidades. The C# [Authorize(Roles = "1")] modifier covers this easily without complex context queries.

• Captador (Rol 4) / Coordinador (Rol 3): Can manage tracking data, grade assignments, and bulk assign students. Access is contextualized to induction units.

• Aspirante (Rol 2): Scoped strictly to view only courses dynamically assigned to them. In C#, after authenticating the user session, you retrieve the induction workload ensuring NO crossover, like this:



SELECT 

    m.Nombre AS Materia, 

    u.Nombre AS Unidad, 

    p.Estado, 

    p.Calificacion

FROM dbo.Ind_ProgresoAspirantes p

INNER JOIN dbo.Aspirantes a ON p.AspiranteID = a.AspiranteID

INNER JOIN dbo.Usuarios usr ON a.UsuarioID = usr.UsuarioID

INNER JOIN dbo.Ind_Unidades u ON p.UnidadID = u.UnidadID

INNER JOIN dbo.Ind_Materias m ON u.MateriaID = m.MateriaID

WHERE usr.UsuarioID = @CurrentLoggedUserID 

  AND usr.RolID = 2; -- Guarantee they strictly hold the Aspirante role



3. Data Consistency without Duplication

To guarantee that changes in the core CaptacionDB immediately affect the Induction module with zero duplication of data, follow these architectural rules:

1. Foreign Key Referential Strategy: Notice the tables above do not store Carrera, Periodo, or Aspirante textual data; they refer exclusively to the exact PK identifiers found in dbo.Carreras, dbo.Periodos, and dbo.Usuarios. Therefore, if an administrator updates an Applicant's core profile or fixes a Career name in CaptacionDB, ASP.NET MVC Induction views executing standard SQL JOINs instantly reflect these corrections.

2. No Action Cascading / Real-Time Views: Maintaining a strict constraint relationship implies that core rows (Periodos, Aspirantes) dictate existence. By leveraging these links, you avoid "synchronization" logic entirely.

3. Cross-Module Integrity: When a Captador approves or advances an admission requirement natively in CaptacionDB, checking that flag becomes as simple as adding an AND a.CompletadoCore = 1 condition in your Induction JOIN views. The Induction module behaves as a perfectly mirrored child entity.

(Note: As an AI assistant in read-only mode, I have fully validated these definitions against your current database schema configuration but cannot execute the schema creation dynamically. You can copy the exact generated T-SQL script directly to SSMS execution to seamlessly establish your new framework) 

