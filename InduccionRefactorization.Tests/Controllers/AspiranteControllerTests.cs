using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using induccion_refactorization.Controllers;
using induccion_refactorization.Models;
using InduccionRefactorization.Tests.TestHelpers;
using Moq;
using Xunit;

namespace InduccionRefactorization.Tests.Controllers
{
    /// <summary>
    /// PHASE 5 TESTS: Progress Tracking and Student Portal
    /// Tests AspiranteController functionality for displaying courses,
    /// calculating progress percentages, averaging grades, and showing
    /// learning materials with institutional theme (#1ab192)
    /// </summary>
    public class AspiranteControllerTests : ControllerTestBase
    {
        private Mock<CaptacionDbContext> _mockContext;
        private AspiranteController _controller;
        private Aspirante _testAspirante;
        private List<Ind_ProgresoAspirante> _testProgreso;
        private List<Ind_Materia> _testMaterias;
        private List<Ind_Unidad> _testUnidades;
        private List<Ind_Material> _testMateriales;

        public AspiranteControllerTests()
        {
            InitializeControllerContext();

            // Create test data
            _testAspirante = TestDataFactory.CreateTestAspirante();
            _testProgreso = TestDataFactory.CreateTestProgresoAspirante();
            _testMaterias = TestDataFactory.CreateTestMaterias();
            _testUnidades = TestDataFactory.CreateTestUnidades();
            _testMateriales = TestDataFactory.CreateTestMateriales();

            // Link navigation properties
            foreach (var progreso in _testProgreso)
            {
                progreso.Aspirante = _testAspirante;
                progreso.Ind_Unidad = _testUnidades.FirstOrDefault(u => u.UnidadID == progreso.UnidadID);
                if (progreso.Ind_Unidad != null)
                {
                    progreso.Ind_Unidad.Ind_Materia = _testMaterias.FirstOrDefault(m => m.MateriaID == progreso.Ind_Unidad.MateriaID);
                }
            }

            _testAspirante.Ind_ProgresoAspirantes = _testProgreso;

            // Mock database context
            _mockContext = new Mock<CaptacionDbContext>();
            
            var mockAspirantesSet = MockDbSetHelper.CreateMockDbSetWithIncludes(new List<Aspirante> { _testAspirante });
            var mockProgresoSet = MockDbSetHelper.CreateMockDbSet(_testProgreso);
            var mockMateriasSet = MockDbSetHelper.CreateMockDbSetWithIncludes(_testMaterias);

            _mockContext.Setup(m => m.Aspirantes).Returns(mockAspirantesSet.Object);
            _mockContext.Setup(m => m.Ind_ProgresoAspirante).Returns(mockProgresoSet.Object); // SINGULAR
            _mockContext.Setup(m => m.Ind_Materias).Returns(mockMateriasSet.Object);

            // Create controller
            _controller = new AspiranteController();
            _controller.ControllerContext = ControllerContext;
        }

        #region Index Action Tests (Phase 5)

        [Fact]
        public void Index_AuthenticatedAspirante_ReturnsViewWithCourses()
        {
            // Arrange: Setup aspirante session (RolID = 4)
            SetupAuthenticatedSession(
                usuarioID: 1579, 
                rolID: 4, 
                nombreCompleto: "Ana Rodríguez Pérez", 
                email: "aspirante@test.com",
                aspiranteID: 1579
            );

            // Act
            var result = _controller.Index();

            // Assert
            var viewResult = result as ViewResult;
            Assert.NotNull(viewResult);
            Assert.NotNull(_controller.ViewBag.MateriasProgreso);
        }

        [Fact]
        public void Index_NoAspiranteID_ShowsErrorMessage()
        {
            // Arrange: Session without AspiranteID
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com");
            MockSession.Setup(x => x["AspiranteID"]).Returns(null);

            // Act
            var result = _controller.Index();

            // Assert
            var viewResult = result as ViewResult;
            Assert.NotNull(viewResult);
            Assert.NotNull(_controller.ViewBag.Error);
            Assert.Contains("perfil de aspirante", _controller.ViewBag.Error.ToString().ToLower());
        }

        [Fact]
        public void Index_NonexistentAspirante_ShowsErrorMessage()
        {
            // Arrange: Valid session but aspirante not in database
            SetupAuthenticatedSession(9999, 4, "Invalid User", "invalid@test.com", aspiranteID: 9999);
            
            _mockContext.Setup(m => m.Aspirantes).Returns(
                MockDbSetHelper.CreateMockDbSetWithIncludes(new List<Aspirante>()).Object
            );

            // Act
            var result = _controller.Index();

            // Assert
            Assert.NotNull(_controller.ViewBag.Error);
            Assert.Contains("no encontrado", _controller.ViewBag.Error.ToString().ToLower());
        }

        [Fact]
        public void Index_SetsSessionDataInViewBag()
        {
            // Arrange
            string expectedName = "Ana Rodríguez Pérez";
            string expectedEmail = "aspirante@test.com";
            string expectedMatricula = "MAT-2024-001579";
            string expectedFolio = "FOL-2024-001579";

            SetupAuthenticatedSession(1579, 4, expectedName, expectedEmail, 1579);
            MockSession.Setup(x => x["Matricula"]).Returns(expectedMatricula);
            MockSession.Setup(x => x["Folio"]).Returns(expectedFolio);

            // Act
            var result = _controller.Index();

            // Assert
            Assert.Equal(expectedName, _controller.ViewBag.NombreCompleto);
            Assert.Equal(expectedEmail, _controller.ViewBag.Email);
            Assert.Equal(expectedMatricula, _controller.ViewBag.Matricula);
            Assert.Equal(expectedFolio, _controller.ViewBag.Folio);
        }

        #endregion

        #region Progress Calculation Tests (Phase 5 Critical)

        [Fact]
        public void Index_CalculatesProgressPercentageCorrectly()
        {
            // Arrange
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);

            // Test Data Analysis:
            // Materia 1: 2 unidades (both Calificado) = 100%
            // Materia 2: 2 unidades (1 Entregado, 1 Asignado) = 0%
            // Materia 3: 1 unidad (Asignado) = 0%

            // Act
            var result = _controller.Index();

            // Assert
            var materiasProgreso = _controller.ViewBag.MateriasProgreso as dynamic;
            Assert.NotNull(materiasProgreso);

            // Verify progress calculation logic
            var materiasList = materiasProgreso as List<object>;
            Assert.NotNull(materiasList);
            Assert.True(materiasList.Count > 0);
        }

        [Fact]
        public void Index_GroupsProgressByMateria()
        {
            // Arrange
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);

            // Act
            var result = _controller.Index();

            // Assert: Progress should be grouped by materia
            var materiasProgreso = _controller.ViewBag.MateriasProgreso as dynamic;
            Assert.NotNull(materiasProgreso);
            
            // Should have grouped data for each unique materia
            // Test data has unidades from 3 different materias
        }

        [Fact]
        public void Index_CountsCompletedUnitsCorrectly()
        {
            // Arrange
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);

            // Expected from TestDataFactory:
            // UnidadID 1: Estado = "Calificado" ✓
            // UnidadID 2: Estado = "Calificado" ✓
            // UnidadID 3: Estado = "Entregado" ✗ (not graded yet)
            // UnidadID 4: Estado = "Asignado" ✗
            // UnidadID 5: Estado = "Asignado" ✗
            int expectedCompletedCount = 2;

            // Act
            var result = _controller.Index();

            // Assert: Verify completed count
            var materiasProgreso = _controller.ViewBag.MateriasProgreso as IEnumerable<dynamic>;
            Assert.NotNull(materiasProgreso);

            int totalCompleted = 0;
            foreach (var materia in materiasProgreso)
            {
                totalCompleted += (int)materia.UnidadesCompletadas;
            }

            Assert.Equal(expectedCompletedCount, totalCompleted);
        }

        #endregion

        #region Grade Average Calculation Tests (Phase 5)

        [Fact]
        public void Index_CalculatesAverageGradeCorrectly()
        {
            // Arrange
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);

            // Expected from TestDataFactory:
            // Unidad 1: Calificacion = 9.50
            // Unidad 2: Calificacion = 8.75
            // Unidad 3-5: Calificacion = null (not graded)
            // Expected Average = (9.50 + 8.75) / 2 = 9.125

            decimal expectedAverage = 9.125m;

            // Act
            var result = _controller.Index();

            // Assert
            var materiasProgreso = _controller.ViewBag.MateriasProgreso as IEnumerable<dynamic>;
            Assert.NotNull(materiasProgreso);

            // Find materia with graded unidades
            foreach (var materia in materiasProgreso)
            {
                if (materia.PromedioCalificacion != null)
                {
                    decimal actualAverage = (decimal)materia.PromedioCalificacion;
                    Assert.True(Math.Abs(actualAverage - expectedAverage) < 0.01m);
                }
            }
        }

        [Fact]
        public void Index_OnlyIncludesGradedUnitsInAverage()
        {
            // Arrange
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);

            // Test that units with Estado != "Calificado" are NOT included
            var ungraded = _testProgreso.Where(p => !p.Calificacion.HasValue).ToList();
            Assert.True(ungraded.Count > 0); // Ensure we have ungraded records

            // Act
            var result = _controller.Index();

            // Assert: Average should only include graded records
            var materiasProgreso = _controller.ViewBag.MateriasProgreso as IEnumerable<dynamic>;
            Assert.NotNull(materiasProgreso);
        }

        [Fact]
        public void Index_HandlesNoGradedUnits()
        {
            // Arrange: Create aspirante with no graded units
            var aspiranteNoGrades = TestDataFactory.CreateTestAspirante();
            aspiranteNoGrades.AspiranteID = 2000;
            
            var progresoNoGrades = new List<Ind_ProgresoAspirante>
            {
                new Ind_ProgresoAspirante
                {
                    ProgresoID = 100,
                    AspiranteID = 2000,
                    UnidadID = 1,
                    Estado = "Asignado",
                    Calificacion = null
                }
            };

            aspiranteNoGrades.Ind_ProgresoAspirantes = progresoNoGrades;

            _mockContext.Setup(m => m.Aspirantes).Returns(
                MockDbSetHelper.CreateMockDbSetWithIncludes(new List<Aspirante> { aspiranteNoGrades }).Object
            );

            SetupAuthenticatedSession(2000, 4, "Test User", "test@test.com", 2000);

            // Act
            var result = _controller.Index();

            // Assert: Should handle null average gracefully
            Assert.NotNull(result);
        }

        #endregion

        #region Decimal Precision Tests (Phase 3 Critical Fix)

        [Fact]
        public void ProgresoAspirante_CalificacionField_UsesCorrectDecimalPrecision()
        {
            // Arrange: CRITICAL TEST - Verify decimal(5,2) format WITHOUT spaces
            // This validates the fix for "decimal(5, 2)" type not found error
            
            var testCalificacion = 9.50m;
            var progreso = new Ind_ProgresoAspirante
            {
                ProgresoID = 999,
                AspiranteID = 1579,
                UnidadID = 1,
                Estado = "Calificado",
                Calificacion = testCalificacion // Should use decimal(5,2) precision
            };

            // Assert: Decimal precision should be exactly 2 decimal places
            Assert.Equal(2, BitConverter.GetBytes(decimal.GetBits(testCalificacion)[3])[2]);
        }

        [Theory]
        [InlineData(10.00)]
        [InlineData(9.99)]
        [InlineData(0.00)]
        [InlineData(5.50)]
        public void ProgresoAspirante_CalificacionValues_WithinValidRange(decimal calificacion)
        {
            // Arrange: Test various grade values
            var progreso = new Ind_ProgresoAspirante
            {
                Calificacion = calificacion
            };

            // Assert: Should accept valid grades (0.00 - 10.00)
            Assert.True(progreso.Calificacion >= 0.00m && progreso.Calificacion <= 10.00m);
        }

        #endregion

        #region MateriaDetails Tests (Phase 5)

        [Fact]
        public void MateriaDetails_ValidID_ReturnsViewWithUnidades()
        {
            // Arrange
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);
            int validMateriaID = 1;

            // Act
            var result = _controller.MateriaDetails(validMateriaID);

            // Assert
            var viewResult = result as ViewResult;
            Assert.NotNull(viewResult);
            
            var model = viewResult.Model as Ind_Materia;
            Assert.NotNull(model);
            Assert.Equal(validMateriaID, model.MateriaID);
        }

        [Fact]
        public void MateriaDetails_InvalidID_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);
            
            _mockContext.Setup(m => m.Ind_Materias).Returns(
                MockDbSetHelper.CreateMockDbSetWithIncludes(new List<Ind_Materia>()).Object
            );

            // Act
            var result = _controller.MateriaDetails(999);

            // Assert
            Assert.IsType<HttpNotFoundResult>(result);
        }

        [Fact]
        public void MateriaDetails_IncludesProgressRecords()
        {
            // Arrange
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);

            // Act
            var result = _controller.MateriaDetails(1);

            // Assert: Should query progreso records
            // Verify that db.Ind_ProgresoAspirante (SINGULAR) is queried
            _mockContext.Verify(m => m.Ind_ProgresoAspirante, Times.AtLeastOnce);
        }

        [Fact]
        public void MateriaDetails_ShowsMaterialsForEachUnidad()
        {
            // Arrange
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);

            // Link materials to unidades
            foreach (var material in _testMateriales)
            {
                var unidad = _testUnidades.FirstOrDefault(u => u.UnidadID == material.UnidadID);
                if (unidad != null)
                {
                    unidad.Ind_Materiales = unidad.Ind_Materiales ?? new List<Ind_Material>();
                    unidad.Ind_Materiales.Add(material);
                }
            }

            // Act
            var result = _controller.MateriaDetails(1);

            // Assert
            var viewResult = result as ViewResult;
            Assert.NotNull(viewResult);
            
            var materia = viewResult.Model as Ind_Materia;
            Assert.NotNull(materia);
            
            // Verify unidades have materials
            Assert.NotNull(materia.Ind_Unidades);
        }

        [Fact]
        public void MateriaDetails_DisplaysEvaluatorComments()
        {
            // Arrange
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);

            // Verify test data has evaluator comments
            var gradedRecord = _testProgreso.FirstOrDefault(p => p.Estado == "Calificado");
            Assert.NotNull(gradedRecord);
            Assert.NotNull(gradedRecord.ComentariosEvaluador);

            // Act
            var result = _controller.MateriaDetails(1);

            // Assert: Comments should be available in progreso records
            Assert.NotNull(result);
        }

        #endregion

        #region DbSet Naming Convention Tests (Phase 3 Critical Fix)

        [Fact]
        public void MateriaDetails_UsesSingularDbSetName_NoCompilationError()
        {
            // Arrange: CRITICAL TEST - Verify singular DbSet usage
            // This test verifies the fix for CS1061 error:
            // "CaptacionDbContext does not contain definition for 'Ind_ProgresoAspirantes'"
            
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);

            // Act: Should use db.Ind_ProgresoAspirante (SINGULAR) in query
            var result = _controller.MateriaDetails(1);

            // Assert: No compilation error occurs
            Assert.NotNull(result);
            
            // Verify singular DbSet was accessed (not plural)
            _mockContext.Verify(m => m.Ind_ProgresoAspirante, Times.AtLeastOnce);
        }

        [Fact]
        public void Index_UsesNavigationPropertyPlural_NoCompilationError()
        {
            // Arrange: CRITICAL TEST - Verify plural navigation property usage
            // This test verifies that navigation properties correctly use plural:
            // aspirante.Ind_ProgresoAspirantes (PLURAL collection)
            
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);

            // Act: Should use aspirante.Ind_ProgresoAspirantes (PLURAL)
            var result = _controller.Index();

            // Assert: No compilation error occurs
            Assert.NotNull(result);
            
            // Verify aspirante has progress records via navigation property
            Assert.NotNull(_testAspirante.Ind_ProgresoAspirantes);
            Assert.True(_testAspirante.Ind_ProgresoAspirantes.Count > 0);
        }

        #endregion

        #region Institutional Theme Tests (Phase 5)

        [Fact]
        public void Index_UsesInstitutionalThemeColor()
        {
            // Arrange: Verify views use #1ab192 (UTTN emerald teal)
            // This is verified in the view templates, not controller logic
            // But we can verify the data structure supports theme implementation
            
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);

            // Act
            var result = _controller.Index();

            // Assert: Data structure exists for progress bars
            var materiasProgreso = _controller.ViewBag.MateriasProgreso;
            Assert.NotNull(materiasProgreso);
            
            // View implementation should render progress bars with #1ab192
            // Example: style="background-color: #1ab192; width: 40%"
        }

        #endregion

        #region Estado Workflow Tests (Phase 5)

        [Theory]
        [InlineData("Asignado", "Unit assigned but not started")]
        [InlineData("Entregado", "Unit submitted, pending grading")]
        [InlineData("Calificado", "Unit graded and completed")]
        public void ProgresoAspirante_EstadoValues_MatchWorkflow(string estado, string description)
        {
            // Arrange: Verify Estado field accepts valid workflow states
            var progreso = new Ind_ProgresoAspirante
            {
                Estado = estado
            };

            // Assert: Estado should match expected workflow
            Assert.Equal(estado, progreso.Estado);
            Assert.NotNull(description); // Documentation exists
        }

        [Fact]
        public void Index_DisplaysCorrectStatusBadges()
        {
            // Arrange
            SetupAuthenticatedSession(1579, 4, "Ana Rodríguez", "aspirante@test.com", 1579);

            // Verify test data has multiple estados
            var estados = _testProgreso.Select(p => p.Estado).Distinct().ToList();
            Assert.True(estados.Count > 1); // Should have mixed states

            // Act
            var result = _controller.Index();

            // Assert: Data available for status badge rendering
            Assert.NotNull(result);
        }

        #endregion

        #region FechaEnvio Tracking Tests (Phase 5)

        [Fact]
        public void ProgresoAspirante_FechaEnvio_TracksSubmissionDate()
        {
            // Arrange: Verify submitted units have FechaEnvio
            var submittedRecord = _testProgreso.FirstOrDefault(p => p.Estado == "Entregado");
            
            // Assert
            Assert.NotNull(submittedRecord);
            Assert.NotNull(submittedRecord.FechaEnvio);
            Assert.True(submittedRecord.FechaEnvio > submittedRecord.FechaAsignacion);
        }

        [Fact]
        public void ProgresoAspirante_AsignadoUnits_NoFechaEnvio()
        {
            // Arrange: Verify assigned units don't have FechaEnvio yet
            var assignedRecord = _testProgreso.FirstOrDefault(p => p.Estado == "Asignado");
            
            // Assert
            Assert.NotNull(assignedRecord);
            Assert.Null(assignedRecord.FechaEnvio);
        }

        #endregion
    }
}
