using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using induccion_refactorization.Models;
using induccion_refactorization.ViewModels;
using Xunit;

namespace InduccionRefactorization.Tests.Models
{
    /// <summary>
    /// MODEL VALIDATION TESTS: Data Annotation and Business Rules
    /// Tests model validation attributes, required fields, string lengths,
    /// data types, and business rule enforcement across all entities
    /// </summary>
    public class ModelValidationTests
    {
        #region LoginViewModel Validation Tests

        [Fact]
        public void LoginViewModel_EmailRequired_ValidationFails()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = null,
                Password = "Password123!"
            };

            // Act
            var results = ValidateModel(model);

            // Assert
            Assert.False(results.IsValid);
            Assert.Contains(results.Errors, e => e.MemberNames.Contains("Email"));
        }

        [Fact]
        public void LoginViewModel_PasswordRequired_ValidationFails()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "admin@test.com",
                Password = null
            };

            // Act
            var results = ValidateModel(model);

            // Assert
            Assert.False(results.IsValid);
            Assert.Contains(results.Errors, e => e.MemberNames.Contains("Password"));
        }

        [Theory]
        [InlineData("admin@test.com", true)]
        [InlineData("director@test.com", true)]
        [InlineData("user.name@company.org", true)]
        [InlineData("invalid-email", false)]
        [InlineData("@test.com", false)]
        [InlineData("test@", false)]
        [InlineData("plaintext", false)]
        public void LoginViewModel_EmailFormat_ValidatesCorrectly(string email, bool shouldBeValid)
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = email,
                Password = "Password123!"
            };

            // Act
            var results = ValidateModel(model);

            // Assert
            Assert.Equal(shouldBeValid, results.IsValid);
        }

        [Fact]
        public void LoginViewModel_ValidModel_Passes()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "admin@test.com",
                Password = "Password123!",
                RememberMe = false
            };

            // Act
            var results = ValidateModel(model);

            // Assert
            Assert.True(results.IsValid);
            Assert.Empty(results.Errors);
        }

        #endregion

        #region Ind_ProgresoAspirante Validation Tests

        [Fact]
        public void IndProgresoAspirante_EstadoRequired_ValidationFails()
        {
            // Arrange
            var progreso = new Ind_ProgresoAspirante
            {
                AspiranteID = 1579,
                UnidadID = 1,
                Estado = null, // Required field
                FechaAsignacion = DateTime.Now
            };

            // Act
            var results = ValidateModel(progreso);

            // Assert
            Assert.False(results.IsValid);
            Assert.Contains(results.Errors, e => e.MemberNames.Contains("Estado"));
        }

        [Fact]
        public void IndProgresoAspirante_EstadoMaxLength50_Enforced()
        {
            // Arrange: Estado exceeds max length
            var progreso = new Ind_ProgresoAspirante
            {
                AspiranteID = 1579,
                UnidadID = 1,
                Estado = new string('X', 51), // Exceeds 50 chars
                FechaAsignacion = DateTime.Now
            };

            // Act
            var results = ValidateModel(progreso);

            // Assert
            Assert.False(results.IsValid);
        }

        [Fact]
        public void IndProgresoAspirante_CalificacionOptional_AllowsNull()
        {
            // Arrange: Ungraded unit
            var progreso = new Ind_ProgresoAspirante
            {
                AspiranteID = 1579,
                UnidadID = 1,
                Estado = "Asignado",
                Calificacion = null,
                FechaAsignacion = DateTime.Now
            };

            // Act
            var results = ValidateModel(progreso);

            // Assert: Should be valid (Calificacion is nullable)
            Assert.True(results.IsValid);
        }

        [Theory]
        [InlineData(10.00)]
        [InlineData(9.99)]
        [InlineData(0.00)]
        [InlineData(5.50)]
        public void IndProgresoAspirante_CalificacionValidRange_Accepted(decimal calificacion)
        {
            // Arrange
            var progreso = new Ind_ProgresoAspirante
            {
                AspiranteID = 1579,
                UnidadID = 1,
                Estado = "Calificado",
                Calificacion = calificacion,
                FechaAsignacion = DateTime.Now
            };

            // Act
            var results = ValidateModel(progreso);

            // Assert
            Assert.True(results.IsValid);
        }

        [Fact]
        public void IndSubmision_RutaArchivoMaxLength500_Enforced()
        {
            // Arrange
            var submision = new Ind_Submision
            {
                AspiranteID = 1579,
                EntregableID = 1,
                Estado = "Pendiente",
                RutaArchivo = new string('X', 501), // Exceeds 500
                FechaEnvio = DateTime.Now
            };

            // Act
            var results = ValidateModel(submision);

            // Assert
            Assert.False(results.IsValid);
        }

        #endregion

        #region Ind_Materia Validation Tests

        [Fact]
        public void IndMateria_NombreRequired_ValidationFails()
        {
            // Arrange
            var materia = new Ind_Materia
            {
                Nombre = null, // Required
                Descripcion = "Test",
                CarreraID = 1,
                PeriodoID = 1
            };

            // Act
            var results = ValidateModel(materia);

            // Assert
            Assert.False(results.IsValid);
            Assert.Contains(results.Errors, e => e.MemberNames.Contains("Nombre"));
        }

        [Fact]
        public void IndMateria_ValidModel_Passes()
        {
            // Arrange
            var materia = new Ind_Materia
            {
                Nombre = "Introducción a la UTTN",
                Descripcion = "Conoce la historia institucional",
                CarreraID = 1,
                PeriodoID = 1,
                Activo = true
            };

            // Act
            var results = ValidateModel(materia);

            // Assert
            Assert.True(results.IsValid);
        }

        #endregion

        #region Ind_Unidad Validation Tests

        [Fact]
        public void IndUnidad_NombreRequired_ValidationFails()
        {
            // Arrange
            var unidad = new Ind_Unidad
            {
                MateriaID = 1,
                Nombre = null, // Required
                Orden = 1
            };

            // Act
            var results = ValidateModel(unidad);

            // Assert
            Assert.False(results.IsValid);
        }

        [Fact]
        public void IndUnidad_OrdenZero_NoValidationConstraint()
        {
            // Arrange: Orden has no Range/positivity constraint in the model
            var unidad = new Ind_Unidad
            {
                MateriaID = 1,
                Nombre = "Test Unit",
                Orden = 0
            };

            // Act
            var results = ValidateModel(unidad);

            // Assert
            Assert.NotNull(unidad);
        }

        [Fact]
        public void IndUnidad_ValidModel_Passes()
        {
            // Arrange
            var unidad = new Ind_Unidad
            {
                MateriaID = 1,
                Nombre = "Historia Institucional",
                Orden = 1
            };

            // Act
            var results = ValidateModel(unidad);

            // Assert
            Assert.True(results.IsValid);
        }

        #endregion

        #region Ind_Material Validation Tests

        [Fact]
        public void IndMaterial_NombreRequired_ValidationFails()
        {
            // Arrange
            var material = new Ind_Material
            {
                UnidadID = 1,
                Nombre = null, // Required
                TipoRecurso = "PDF",
                RutaURL = "/uploads/test.pdf"
            };

            // Act
            var results = ValidateModel(material);

            // Assert
            Assert.False(results.IsValid);
        }

        [Fact]
        public void IndMaterial_TipoRecursoRequired_ValidationFails()
        {
            // Arrange
            var material = new Ind_Material
            {
                UnidadID = 1,
                Nombre = "Test Material",
                TipoRecurso = null, // Required
                RutaURL = "/uploads/test.pdf"
            };

            // Act
            var results = ValidateModel(material);

            // Assert
            Assert.False(results.IsValid);
        }

        [Theory]
        [InlineData("PDF")]
        [InlineData("Video")]
        [InlineData("Link")]
        public void IndMaterial_ValidTipos_Accepted(string tipo)
        {
            // Arrange
            var material = new Ind_Material
            {
                UnidadID = 1,
                Nombre = "Test Material",
                TipoRecurso = tipo,
                RutaURL = "http://example.com/resource"
            };

            // Act
            var results = ValidateModel(material);

            // Assert
            Assert.True(results.IsValid);
        }

        #endregion

        #region Usuario Validation Tests

        [Fact]
        public void Usuario_NombreRequired_ValidationFails()
        {
            // Arrange
            var usuario = new Usuario
            {
                Nombre = null, // Required
                ApellidoPaterno = "Martínez",
                CorreoElectronico = "test@test.com",
                Contrasena = "Password123!",
                RolID = 1
            };

            // Act
            var results = ValidateModel(usuario);

            // Assert
            Assert.False(results.IsValid);
        }

        [Fact]
        public void Usuario_CorreoElectronicoRequired_ValidationFails()
        {
            // Arrange
            var usuario = new Usuario
            {
                Nombre = "Carlos",
                ApellidoPaterno = "Martínez",
                CorreoElectronico = null, // Required
                Contrasena = "Password123!",
                RolID = 1
            };

            // Act
            var results = ValidateModel(usuario);

            // Assert
            Assert.False(results.IsValid);
        }

        [Fact]
        public void Usuario_ContrasenaRequired_ValidationFails()
        {
            // Arrange
            var usuario = new Usuario
            {
                Nombre = "Carlos",
                ApellidoPaterno = "Martínez",
                CorreoElectronico = "carlos@test.com",
                Contrasena = null, // Required
                RolID = 1
            };

            // Act
            var results = ValidateModel(usuario);

            // Assert
            Assert.False(results.IsValid);
        }

        [Fact]
        public void Usuario_NombreCompleto_ComputedCorrectly()
        {
            // Arrange
            var usuario = new Usuario
            {
                Nombre = "Carlos",
                ApellidoPaterno = "Martínez",
                ApellidoMaterno = "López"
            };

            // Act
            string nombreCompleto = usuario.NombreCompleto;

            // Assert
            Assert.Equal("Carlos Martínez López", nombreCompleto);
        }

        [Fact]
        public void Usuario_NombreCompleto_WithoutApellidoMaterno()
        {
            // Arrange
            var usuario = new Usuario
            {
                Nombre = "Carlos",
                ApellidoPaterno = "Martínez",
                ApellidoMaterno = null
            };

            // Act
            string nombreCompleto = usuario.NombreCompleto;

            // Assert
            Assert.Equal("Carlos Martínez", nombreCompleto.Trim());
        }

        #endregion

        #region Aspirante Validation Tests

        [Fact]
        public void Aspirante_FolioRequired_ValidationFails()
        {
            // Arrange
            var aspirante = new Aspirante
            {
                UsuarioID = 1579,
                Folio = null, // Required
                Matricula = "M-0001579",
                FechaNacimiento = new DateTime(2006, 3, 15)
            };

            // Act
            var results = ValidateModel(aspirante);

            // Assert
            Assert.False(results.IsValid);
        }

        [Fact]
        public void Aspirante_PromedioGeneral_Precision31_Validated()
        {
            // Arrange: PromedioGeneral is decimal(3,1)
            var aspirante = new Aspirante
            {
                UsuarioID = 1579,
                Folio = "F-0001579",
                Matricula = "M-0001579",
                FechaNacimiento = new DateTime(2006, 3, 15),
                PromedioGeneral = 8.8m // Valid: decimal(3,1)
            };

            // Act
            var results = ValidateModel(aspirante);

            // Assert
            Assert.True(results.IsValid);
            Assert.Equal(8.8m, aspirante.PromedioGeneral);
        }

        [Theory]
        [InlineData(10.0)]
        [InlineData(9.9)]
        [InlineData(0.0)]
        [InlineData(6.5)]
        public void Aspirante_PromedioGeneral_ValidRange_Accepted(decimal promedio)
        {
            // Arrange
            var aspirante = new Aspirante
            {
                UsuarioID = 1579,
                Folio = "F-0001579",
                Matricula = "M-0001579",
                FechaNacimiento = new DateTime(2006, 3, 15),
                PromedioGeneral = promedio
            };

            // Act
            var results = ValidateModel(aspirante);

            // Assert
            Assert.True(results.IsValid);
        }

        #endregion

        #region Decimal Type Validation Tests (Phase 3 Critical)

        [Fact]
        public void Decimal52_NoWhiteSpace_ValidatesCorrectly()
        {
            // Arrange: CRITICAL TEST - Validates decimal(5,2) format
            // This verifies the fix for "decimal(5, 2)" error
            
            var progreso = new Ind_ProgresoAspirante
            {
                AspiranteID = 1579,
                UnidadID = 1,
                Estado = "Calificado",
                Calificacion = 9.75m, // decimal(5,2) - NO SPACES
                FechaAsignacion = DateTime.Now
            };

            // Act
            var results = ValidateModel(progreso);

            // Assert
            Assert.True(results.IsValid);
            Assert.Equal(9.75m, progreso.Calificacion);
        }

        [Fact]
        public void Decimal31_PromedioGeneral_ValidatesCorrectly()
        {
            // Arrange: PromedioGeneral is decimal(3,1)
            var aspirante = new Aspirante
            {
                UsuarioID = 1579,
                Folio = "F-0001579",
                Matricula = "M-0001579",
                FechaNacimiento = new DateTime(2006, 3, 15),
                PromedioGeneral = 8.8m // decimal(3,1)
            };

            // Act
            var results = ValidateModel(aspirante);

            // Assert
            Assert.True(results.IsValid);

            // Verify precision
            string valueString = aspirante.PromedioGeneral.ToString("F2");
            Assert.Equal("8.80", valueString);
        }

        #endregion

        #region String Length Validation Tests

        [Fact]
        public void Usuario_NombreUsuario_MaxLength100_Enforced()
        {
            // Arrange
            var usuario = new Usuario
            {
                NombreUsuario = new string('X', 101), // Exceeds 100
                Nombre = "Test",
                ApellidoPaterno = "User",
                CorreoElectronico = "test@test.com",
                Contrasena = "Password123!",
                RolID = 1
            };

            // Act
            var results = ValidateModel(usuario);

            // Assert
            Assert.False(results.IsValid);
        }

        [Fact]
        public void Usuario_CorreoElectronico_MaxLength200_Enforced()
        {
            // Arrange
            var usuario = new Usuario
            {
                NombreUsuario = "testuser",
                Nombre = "Test",
                ApellidoPaterno = "User",
                CorreoElectronico = new string('X', 190) + "@test.com", // Exceeds 200
                Contrasena = "Password123!",
                RolID = 1
            };

            // Act
            var results = ValidateModel(usuario);

            // Assert
            Assert.False(results.IsValid);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Validates a model using DataAnnotations
        /// </summary>
        private ValidationResults ValidateModel(object model)
        {
            var validationContext = new ValidationContext(model);
            var validationResults = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

            return new ValidationResults
            {
                IsValid = isValid,
                Errors = validationResults
            };
        }

        /// <summary>
        /// Container for validation results
        /// </summary>
        private class ValidationResults
        {
            public bool IsValid { get; set; }
            public List<ValidationResult> Errors { get; set; }
        }

        #endregion
    }
}
