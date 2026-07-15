using System;
using System.Collections.Generic;
using induccion_refactorization.Models;
using induccion_refactorization.ViewModels;

namespace InduccionRefactorization.Tests.TestHelpers
{
    /// <summary>
    /// Factory class to create test data matching the seeded database structure
    /// Coordinates with scripts/SeedInductionData.sql
    /// </summary>
    public static class TestDataFactory
    {
        #region Role Test Data (Phase 1 & 2)

        /// <summary>
        /// Creates test usuarios matching the 4-role matrix
        /// Matches: admin@test.com, director@test.com, coordinador@test.com, aspirante@test.com
        /// </summary>
        public static List<Usuario> CreateTestUsuarios()
        {
            return new List<Usuario>
            {
                // RolID = 1: Administrador
                new Usuario
                {
                    UsuarioID = 1,
                    NombreUsuario = "admin.sistema",
                    Nombre = "Carlos",
                    ApellidoPaterno = "Martínez",
                    ApellidoMaterno = "López",
                    CorreoElectronico = "admin@test.com",
                    Contrasena = "Password123!", // Plain text for testing
                    RolID = 1,
                    Activo = true,
                    FechaRegistro = DateTime.Now.AddMonths(-6)
                },
                // RolID = 2: Director
                new Usuario
                {
                    UsuarioID = 2,
                    NombreUsuario = "director.academico",
                    Nombre = "María",
                    ApellidoPaterno = "González",
                    ApellidoMaterno = "Ramírez",
                    CorreoElectronico = "director@test.com",
                    Contrasena = "Password123!",
                    RolID = 2,
                    Activo = true,
                    FechaRegistro = DateTime.Now.AddMonths(-5)
                },
                // RolID = 3: Coordinador
                new Usuario
                {
                    UsuarioID = 3,
                    NombreUsuario = "coordinador.induccion",
                    Nombre = "Juan",
                    ApellidoPaterno = "Hernández",
                    ApellidoMaterno = "García",
                    CorreoElectronico = "coordinador@test.com",
                    Contrasena = "Password123!",
                    RolID = 3,
                    Activo = true,
                    FechaRegistro = DateTime.Now.AddMonths(-4)
                },
                // RolID = 4: Aspirante (linked to AspiranteID = 1579 in production)
                new Usuario
                {
                    UsuarioID = 1579,
                    NombreUsuario = "aspirante.estudiante",
                    Nombre = "Ana",
                    ApellidoPaterno = "Rodríguez",
                    ApellidoMaterno = "Pérez",
                    CorreoElectronico = "aspirante@test.com",
                    Contrasena = "Password123!",
                    RolID = 4,
                    Activo = true,
                    FechaRegistro = DateTime.Now.AddMonths(-2)
                },
                // Inactive user for negative testing
                new Usuario
                {
                    UsuarioID = 999,
                    NombreUsuario = "usuario.inactivo",
                    Nombre = "Pedro",
                    ApellidoPaterno = "Sánchez",
                    ApellidoMaterno = "Torres",
                    CorreoElectronico = "inactivo@test.com",
                    Contrasena = "Password123!",
                    RolID = 4,
                    Activo = false,
                    FechaRegistro = DateTime.Now.AddYears(-1)
                }
            };
        }

        /// <summary>
        /// Creates test roles matching the 4-role system
        /// </summary>
        public static List<Role> CreateTestRoles()
        {
            return new List<Role>
            {
                new Role { RolID = 1, Nombre = "Administrador" },
                new Role { RolID = 2, Nombre = "Director" },
                new Role { RolID = 3, Nombre = "Coordinador" },
                new Role { RolID = 4, Nombre = "Aspirante" }
            };
        }

        #endregion

        #region Induction Content Test Data (Phase 3 & 4)

        /// <summary>
        /// Creates test materias for induction system
        /// Matches: Introducción a la UTTN, Servicios y Normatividad, Modelo Educativo
        /// </summary>
        public static List<Ind_Materia> CreateTestMaterias()
        {
            return new List<Ind_Materia>
            {
                new Ind_Materia
                {
                    MateriaID = 1,
                    Nombre = "Introducción a la UTTN",
                    Descripcion = "Conoce la historia, misión y valores de nuestra universidad",
                    CarreraID = 1,
                    PeriodoID = 1,
                    Activo = true
                },
                new Ind_Materia
                {
                    MateriaID = 2,
                    Nombre = "Servicios y Normatividad",
                    Descripcion = "Aprende sobre los servicios estudiantiles y reglamentos institucionales",
                    CarreraID = 1,
                    PeriodoID = 1,
                    Activo = true
                },
                new Ind_Materia
                {
                    MateriaID = 3,
                    Nombre = "Modelo Educativo",
                    Descripcion = "Comprende el sistema de enseñanza basado en competencias",
                    CarreraID = 2,
                    PeriodoID = 1,
                    Activo = true
                }
            };
        }

        /// <summary>
        /// Creates test unidades (learning units) for materias
        /// Each materia has 2-3 unidades
        /// </summary>
        public static List<Ind_Unidad> CreateTestUnidades()
        {
            return new List<Ind_Unidad>
            {
                // Materia 1: Introducción a la UTTN
                new Ind_Unidad
                {
                    UnidadID = 1,
                    MateriaID = 1,
                    Nombre = "Historia y Valores Institucionales",
                    Orden = 1
                },
                new Ind_Unidad
                {
                    UnidadID = 2,
                    MateriaID = 1,
                    Nombre = "Instalaciones y Campus",
                    Orden = 2
                },
                // Materia 2: Servicios y Normatividad
                new Ind_Unidad
                {
                    UnidadID = 3,
                    MateriaID = 2,
                    Nombre = "Servicios Estudiantiles",
                    Orden = 1
                },
                new Ind_Unidad
                {
                    UnidadID = 4,
                    MateriaID = 2,
                    Nombre = "Reglamento Académico",
                    Orden = 2
                },
                // Materia 3: Modelo Educativo
                new Ind_Unidad
                {
                    UnidadID = 5,
                    MateriaID = 3,
                    Nombre = "Enfoque por Competencias",
                    Orden = 1
                }
            };
        }

        /// <summary>
        /// Creates test materiales (learning resources) for unidades
        /// Types: PDF, Video, Link
        /// </summary>
        public static List<Ind_Material> CreateTestMateriales()
        {
            return new List<Ind_Material>
            {
                new Ind_Material
                {
                    MaterialID = 1,
                    UnidadID = 1,
                    Nombre = "Historia UTTN - Documento PDF",
                    TipoRecurso = "PDF",
                    RutaURL = "/uploads/induccion/historia_uttn.pdf"
                },
                new Ind_Material
                {
                    MaterialID = 2,
                    UnidadID = 1,
                    Nombre = "Video: Bienvenida del Rector",
                    TipoRecurso = "Video",
                    RutaURL = "https://www.youtube.com/watch?v=example1"
                },
                new Ind_Material
                {
                    MaterialID = 3,
                    UnidadID = 2,
                    Nombre = "Tour Virtual del Campus",
                    TipoRecurso = "Video",
                    RutaURL = "https://www.youtube.com/watch?v=example2"
                },
                new Ind_Material
                {
                    MaterialID = 4,
                    UnidadID = 3,
                    Nombre = "Guía de Servicios PDF",
                    TipoRecurso = "PDF",
                    RutaURL = "/uploads/induccion/servicios.pdf"
                },
                new Ind_Material
                {
                    MaterialID = 5,
                    UnidadID = 4,
                    Nombre = "Reglamento Estudiantil",
                    TipoRecurso = "Link",
                    RutaURL = "https://uttn.edu.mx/reglamento"
                }
            };
        }

        #endregion

        #region Progress Tracking Test Data (Phase 5)

        /// <summary>
        /// Creates test aspirante profile linked to UsuarioID 1579
        /// Matches production backup with Folio and Matricula
        /// </summary>
        public static Aspirante CreateTestAspirante()
        {
            return new Aspirante
            {
                AspiranteID = 1579,
                UsuarioID = 1579,
                Folio = "F-0001579",
                Matricula = "M-0001579",
                FechaNacimiento = new DateTime(2006, 3, 15),
                Direccion = "Calle Principal 123, Ciudad",
                PuntajeEXANI = 1150,
                PromedioGeneral = 8.8m,
                PrimeraOpcionAreaID = 1
            };
        }

        /// <summary>
        /// Creates test progress records for aspirante
        /// CRITICAL: Uses singular table name and decimal(5,2) precision
        /// States: Asignado, Entregado, Calificado
        /// </summary>
        public static List<Ind_ProgresoAspirante> CreateTestProgresoAspirante()
        {
            return new List<Ind_ProgresoAspirante>
            {
                // Unidad 1: Completed and graded
                new Ind_ProgresoAspirante
                {
                    ProgresoID = 1,
                    AspiranteID = 1579,
                    UnidadID = 1,
                    Estado = "Calificado",
                    Calificacion = 9.50m, // decimal(5,2)
                    FechaAsignacion = DateTime.Now.AddDays(-10),
                    FechaEnvio = DateTime.Now.AddDays(-5),
                    UsuarioCalificadorID = 3,
                    ComentariosEvaluador = "Excelente comprensión de la historia institucional"
                },
                // Unidad 2: Completed and graded
                new Ind_ProgresoAspirante
                {
                    ProgresoID = 2,
                    AspiranteID = 1579,
                    UnidadID = 2,
                    Estado = "Calificado",
                    Calificacion = 8.75m,
                    FechaAsignacion = DateTime.Now.AddDays(-10),
                    FechaEnvio = DateTime.Now.AddDays(-4),
                    UsuarioCalificadorID = 3,
                    ComentariosEvaluador = "Buen trabajo en el recorrido virtual"
                },
                // Unidad 3: Submitted, pending grading
                new Ind_ProgresoAspirante
                {
                    ProgresoID = 3,
                    AspiranteID = 1579,
                    UnidadID = 3,
                    Estado = "Entregado",
                    Calificacion = null,
                    FechaAsignacion = DateTime.Now.AddDays(-8),
                    FechaEnvio = DateTime.Now.AddDays(-2),
                    ComentariosEvaluador = null
                },
                // Unidad 4: Assigned, not started
                new Ind_ProgresoAspirante
                {
                    ProgresoID = 4,
                    AspiranteID = 1579,
                    UnidadID = 4,
                    Estado = "Asignado",
                    Calificacion = null,
                    FechaAsignacion = DateTime.Now.AddDays(-8),
                    FechaEnvio = null,
                    ComentariosEvaluador = null
                },
                // Unidad 5: Assigned, not started
                new Ind_ProgresoAspirante
                {
                    ProgresoID = 5,
                    AspiranteID = 1579,
                    UnidadID = 5,
                    Estado = "Asignado",
                    Calificacion = null,
                    FechaAsignacion = DateTime.Now.AddDays(-6),
                    FechaEnvio = null,
                    ComentariosEvaluador = null
                }
            };
        }

        #endregion

        #region View Models

        /// <summary>
        /// Creates valid login view models for testing
        /// </summary>
        public static LoginViewModel CreateValidLoginViewModel(int rolID)
        {
            switch (rolID)
            {
                case 1:
                    return new LoginViewModel
                    {
                        Email = "admin@test.com",
                        Password = "Password123!",
                        RememberMe = false
                    };
                case 2:
                    return new LoginViewModel
                    {
                        Email = "director@test.com",
                        Password = "Password123!",
                        RememberMe = false
                    };
                case 3:
                    return new LoginViewModel
                    {
                        Email = "coordinador@test.com",
                        Password = "Password123!",
                        RememberMe = false
                    };
                case 4:
                    return new LoginViewModel
                    {
                        Email = "aspirante@test.com",
                        Password = "Password123!",
                        RememberMe = true
                    };
                default:
                    throw new ArgumentException("Invalid RolID");
            }
        }

        /// <summary>
        /// Creates invalid login view models for negative testing
        /// </summary>
        public static LoginViewModel CreateInvalidLoginViewModel()
        {
            return new LoginViewModel
            {
                Email = "nonexistent@test.com",
                Password = "WrongPassword!",
                RememberMe = false
            };
        }

        #endregion

        #region Calculation Helpers

        /// <summary>
        /// Calculates expected progress percentage based on test data
        /// Formula: (completedUnits / totalUnits) * 100
        /// </summary>
        public static decimal CalculateExpectedProgressPercentage(int completedUnits, int totalUnits)
        {
            if (totalUnits == 0) return 0m;
            return Math.Round((decimal)completedUnits / totalUnits * 100, 2);
        }

        /// <summary>
        /// Calculates expected average grade from progreso records
        /// Only includes records with Estado = "Calificado"
        /// </summary>
        public static decimal? CalculateExpectedAverageGrade(List<Ind_ProgresoAspirante> progreso)
        {
            var gradedRecords = progreso.Where(p => p.Calificacion.HasValue).ToList();
            if (!gradedRecords.Any()) return null;
            return Math.Round(gradedRecords.Average(p => p.Calificacion.Value), 2);
        }

        #endregion
    }
}
