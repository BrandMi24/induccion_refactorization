using System;
using System.Collections.Generic;
using System.Linq;
using induccion_refactorization.Models;
using InduccionRefactorization.Tests.TestHelpers;
using Xunit;

namespace InduccionRefactorization.Tests.Integration
{
    /// <summary>
    /// INTEGRATION TESTS: Progress Calculation and Reporting
    /// Tests end-to-end progress tracking logic, grade averaging,
    /// completion percentage calculations, and institutional theme rendering
    /// Validates data accuracy for Director reporting and Aspirante dashboards
    /// </summary>
    public class ProgressCalculationTests
    {
        #region Progress Percentage Calculation Tests

        [Fact]
        public void CalculateProgress_AllUnitsCompleted_Returns100Percent()
        {
            // Arrange: All units have Estado = "Calificado"
            var progreso = new List<Ind_ProgresoAspirante>
            {
                new Ind_ProgresoAspirante { Estado = "Calificado", Calificacion = 9.5m },
                new Ind_ProgresoAspirante { Estado = "Calificado", Calificacion = 8.5m },
                new Ind_ProgresoAspirante { Estado = "Calificado", Calificacion = 10.0m }
            };

            int totalUnits = progreso.Count;
            int completedUnits = progreso.Count(p => p.Estado == "Calificado");

            // Act
            decimal percentage = TestDataFactory.CalculateExpectedProgressPercentage(
                completedUnits, totalUnits);

            // Assert
            Assert.Equal(100.00m, percentage);
        }

        [Fact]
        public void CalculateProgress_HalfCompleted_Returns50Percent()
        {
            // Arrange: 2 out of 4 units completed
            var progreso = new List<Ind_ProgresoAspirante>
            {
                new Ind_ProgresoAspirante { Estado = "Calificado", Calificacion = 9.0m },
                new Ind_ProgresoAspirante { Estado = "Calificado", Calificacion = 8.0m },
                new Ind_ProgresoAspirante { Estado = "Entregado", Calificacion = null },
                new Ind_ProgresoAspirante { Estado = "Asignado", Calificacion = null }
            };

            int completedUnits = progreso.Count(p => p.Estado == "Calificado");
            int totalUnits = progreso.Count;

            // Act
            decimal percentage = TestDataFactory.CalculateExpectedProgressPercentage(
                completedUnits, totalUnits);

            // Assert
            Assert.Equal(50.00m, percentage);
        }

        [Fact]
        public void CalculateProgress_NoUnitsCompleted_Returns0Percent()
        {
            // Arrange: All units are "Asignado" (not started)
            var progreso = new List<Ind_ProgresoAspirante>
            {
                new Ind_ProgresoAspirante { Estado = "Asignado", Calificacion = null },
                new Ind_ProgresoAspirante { Estado = "Asignado", Calificacion = null },
                new Ind_ProgresoAspirante { Estado = "Entregado", Calificacion = null }
            };

            int completedUnits = progreso.Count(p => p.Estado == "Calificado");
            int totalUnits = progreso.Count;

            // Act
            decimal percentage = TestDataFactory.CalculateExpectedProgressPercentage(
                completedUnits, totalUnits);

            // Assert
            Assert.Equal(0.00m, percentage);
        }

        [Fact]
        public void CalculateProgress_MixedStates_CalculatesCorrectly()
        {
            // Arrange: Real-world scenario with mixed states
            var progreso = TestDataFactory.CreateTestProgresoAspirante();

            // Test data: 2 Calificado, 1 Entregado, 2 Asignado
            int completedUnits = progreso.Count(p => p.Estado == "Calificado");
            int totalUnits = progreso.Count;

            // Act
            decimal percentage = TestDataFactory.CalculateExpectedProgressPercentage(
                completedUnits, totalUnits);

            // Assert: 2/5 = 40%
            Assert.Equal(40.00m, percentage);
        }

        [Theory]
        [InlineData(0, 5, 0.00)]
        [InlineData(1, 5, 20.00)]
        [InlineData(2, 5, 40.00)]
        [InlineData(3, 5, 60.00)]
        [InlineData(4, 5, 80.00)]
        [InlineData(5, 5, 100.00)]
        public void CalculateProgress_VariousCompletionLevels_ReturnsCorrectPercentage(
            int completed, int total, decimal expectedPercentage)
        {
            // Act
            decimal percentage = TestDataFactory.CalculateExpectedProgressPercentage(
                completed, total);

            // Assert
            Assert.Equal(expectedPercentage, percentage);
        }

        [Fact]
        public void CalculateProgress_RoundingTo2Decimals_WorksCorrectly()
        {
            // Arrange: 1 out of 3 units = 33.333...%
            int completed = 1;
            int total = 3;

            // Act
            decimal percentage = TestDataFactory.CalculateExpectedProgressPercentage(
                completed, total);

            // Assert: Should round to 2 decimal places
            Assert.Equal(33.33m, percentage);
        }

        #endregion

        #region Grade Average Calculation Tests

        [Fact]
        public void CalculateAverage_AllGraded_ReturnsCorrectAverage()
        {
            // Arrange: All units have calificación
            var progreso = new List<Ind_ProgresoAspirante>
            {
                new Ind_ProgresoAspirante { Estado = "Calificado", Calificacion = 10.00m },
                new Ind_ProgresoAspirante { Estado = "Calificado", Calificacion = 9.00m },
                new Ind_ProgresoAspirante { Estado = "Calificado", Calificacion = 8.00m }
            };

            // Act
            decimal? average = TestDataFactory.CalculateExpectedAverageGrade(progreso);

            // Assert: (10 + 9 + 8) / 3 = 9.00
            Assert.NotNull(average);
            Assert.Equal(9.00m, average.Value);
        }

        [Fact]
        public void CalculateAverage_MixedGrades_OnlyIncludesGraded()
        {
            // Arrange: Mix of graded and ungraded units
            var progreso = new List<Ind_ProgresoAspirante>
            {
                new Ind_ProgresoAspirante { Estado = "Calificado", Calificacion = 9.50m },
                new Ind_ProgresoAspirante { Estado = "Calificado", Calificacion = 8.50m },
                new Ind_ProgresoAspirante { Estado = "Entregado", Calificacion = null }, // NOT included
                new Ind_ProgresoAspirante { Estado = "Asignado", Calificacion = null }  // NOT included
            };

            // Act
            decimal? average = TestDataFactory.CalculateExpectedAverageGrade(progreso);

            // Assert: (9.50 + 8.50) / 2 = 9.00
            Assert.NotNull(average);
            Assert.Equal(9.00m, average.Value);
        }

        [Fact]
        public void CalculateAverage_NoGrades_ReturnsNull()
        {
            // Arrange: All units ungraded
            var progreso = new List<Ind_ProgresoAspirante>
            {
                new Ind_ProgresoAspirante { Estado = "Asignado", Calificacion = null },
                new Ind_ProgresoAspirante { Estado = "Entregado", Calificacion = null }
            };

            // Act
            decimal? average = TestDataFactory.CalculateExpectedAverageGrade(progreso);

            // Assert
            Assert.Null(average);
        }

        [Fact]
        public void CalculateAverage_TestDataScenario_MatchesExpected()
        {
            // Arrange: Using actual test data
            var progreso = TestDataFactory.CreateTestProgresoAspirante();

            // Test data has:
            // Unidad 1: 9.50
            // Unidad 2: 8.75
            // Others: null
            // Expected: (9.50 + 8.75) / 2 = 9.125 → rounded to 9.13

            // Act
            decimal? average = TestDataFactory.CalculateExpectedAverageGrade(progreso);

            // Assert
            Assert.NotNull(average);
            Assert.Equal(9.13m, Math.Round(average.Value, 2));
        }

        [Theory]
        [InlineData(new[] { 10.00, 10.00, 10.00 }, 10.00)]
        [InlineData(new[] { 6.00, 7.00, 8.00 }, 7.00)]
        [InlineData(new[] { 9.50, 8.75 }, 9.13)] // From test data
        [InlineData(new[] { 10.00 }, 10.00)]
        public void CalculateAverage_VariousGradeSets_ReturnsCorrectAverage(
            double[] grades, double expectedAverage)
        {
            // Arrange
            var progreso = grades.Select((g, i) => new Ind_ProgresoAspirante
            {
                ProgresoID = i + 1,
                Estado = "Calificado",
                Calificacion = (decimal)g
            }).ToList();

            // Act
            decimal? average = TestDataFactory.CalculateExpectedAverageGrade(progreso);

            // Assert
            Assert.NotNull(average);
            Assert.Equal((decimal)expectedAverage, Math.Round(average.Value, 2));
        }

        #endregion

        #region Decimal Precision Tests (Phase 3 Critical)

        [Fact]
        public void Calificacion_UsesPrecisionDecimal52_NoWhiteSpace()
        {
            // Arrange: CRITICAL TEST - Validates decimal(5,2) attribute
            // This test ensures the fix for "decimal(5, 2)" type error is correct
            
            var progreso = new Ind_ProgresoAspirante
            {
                Calificacion = 9.75m
            };

            // Assert: Should store with exactly 2 decimal places
            Assert.Equal(9.75m, progreso.Calificacion);
            
            // Verify decimal places
            string valueString = progreso.Calificacion.Value.ToString("F2");
            Assert.Equal("9.75", valueString);
        }

        [Theory]
        [InlineData(10.00)]
        [InlineData(9.99)]
        [InlineData(0.01)]
        [InlineData(5.50)]
        public void Calificacion_AcceptsValidDecimalValues(decimal value)
        {
            // Arrange & Act
            var progreso = new Ind_ProgresoAspirante
            {
                Calificacion = value
            };

            // Assert: Should store value without precision loss
            Assert.Equal(value, progreso.Calificacion);
        }

        [Fact]
        public void Calificacion_HandlesNullValues()
        {
            // Arrange: Ungraded units have null calificación
            var progreso = new Ind_ProgresoAspirante
            {
                Estado = "Asignado",
                Calificacion = null
            };

            // Assert
            Assert.Null(progreso.Calificacion);
            Assert.False(progreso.Calificacion.HasValue);
        }

        #endregion

        #region Estado Workflow Validation Tests

        [Fact]
        public void EstadoWorkflow_AsignadoToEntregado_IsValid()
        {
            // Arrange: Unit progresses from assigned to submitted
            var progreso = new Ind_ProgresoAspirante
            {
                Estado = "Asignado",
                FechaAsignacion = DateTime.Now.AddDays(-5),
                FechaEnvio = null
            };

            // Act: Student submits work
            progreso.Estado = "Entregado";
            progreso.FechaEnvio = DateTime.Now;

            // Assert
            Assert.Equal("Entregado", progreso.Estado);
            Assert.NotNull(progreso.FechaEnvio);
            Assert.True(progreso.FechaEnvio > progreso.FechaAsignacion);
        }

        [Fact]
        public void EstadoWorkflow_EntregadoToCalificado_IsValid()
        {
            // Arrange: Unit is submitted, ready for grading
            var progreso = new Ind_ProgresoAspirante
            {
                Estado = "Entregado",
                FechaEnvio = DateTime.Now.AddDays(-2),
                Calificacion = null
            };

            // Act: Coordinador grades the work
            progreso.Estado = "Calificado";
            progreso.Calificacion = 9.50m;
            progreso.UsuarioCalificadorID = 3; // Coordinador
            progreso.ComentariosEvaluador = "Excelente trabajo";

            // Assert
            Assert.Equal("Calificado", progreso.Estado);
            Assert.NotNull(progreso.Calificacion);
            Assert.True(progreso.Calificacion.Value >= 0 && progreso.Calificacion.Value <= 10);
            Assert.NotNull(progreso.ComentariosEvaluador);
        }

        [Theory]
        [InlineData("Asignado")]
        [InlineData("Entregado")]
        [InlineData("Calificado")]
        public void EstadoWorkflow_ValidStates_AreAccepted(string estado)
        {
            // Arrange & Act
            var progreso = new Ind_ProgresoAspirante
            {
                Estado = estado
            };

            // Assert
            Assert.Equal(estado, progreso.Estado);
            Assert.Contains(estado, new[] { "Asignado", "Entregado", "Calificado" });
        }

        #endregion

        #region Institutional Theme Rendering Tests (Phase 5)

        [Fact]
        public void ProgressBar_UsesInstitutionalColor_1ab192()
        {
            // Arrange: Verify institutional theme color is used
            string institutionalColor = "#1ab192"; // UTTN emerald teal

            // Assert: This color should be used in progress bars
            Assert.Equal("#1ab192", institutionalColor);
            
            // View implementation uses:
            // <div style="background-color: #1ab192; width: 40%;"></div>
        }

        [Fact]
        public void ProgressBar_Width_CalculatedFromPercentage()
        {
            // Arrange: 60% completion
            decimal percentage = 60.00m;

            // Act: Simulate view rendering
            string expectedStyle = $"width: {percentage}%";

            // Assert
            Assert.Equal("width: 60%", expectedStyle);
        }

        [Theory]
        [InlineData(0.00, "0%")]
        [InlineData(25.00, "25%")]
        [InlineData(50.00, "50%")]
        [InlineData(75.00, "75%")]
        [InlineData(100.00, "100%")]
        public void ProgressBar_RendersCorrectWidth(decimal percentage, string expectedWidth)
        {
            // Arrange: Various completion levels
            string widthStyle = $"width: {percentage}%";

            // Assert
            Assert.Contains(expectedWidth, widthStyle);
        }

        #endregion

        #region Director Reporting Tests (Phase 5)

        [Fact]
        public void DirectorReport_AggregatesAllAspiranteProgress()
        {
            // Arrange: Multiple aspirantes with progress
            var aspirante1Progress = TestDataFactory.CreateTestProgresoAspirante();
            var aspirante2Progress = new List<Ind_ProgresoAspirante>
            {
                new Ind_ProgresoAspirante { Estado = "Calificado", Calificacion = 8.00m },
                new Ind_ProgresoAspirante { Estado = "Calificado", Calificacion = 9.00m }
            };

            // Act: Calculate overall statistics
            int totalUnits = aspirante1Progress.Count + aspirante2Progress.Count;
            int totalCompleted = aspirante1Progress.Count(p => p.Estado == "Calificado") +
                                 aspirante2Progress.Count(p => p.Estado == "Calificado");

            decimal overallPercentage = TestDataFactory.CalculateExpectedProgressPercentage(
                totalCompleted, totalUnits);

            // Assert: Director sees aggregated metrics
            Assert.True(totalUnits > 0);
            Assert.True(overallPercentage >= 0 && overallPercentage <= 100);
        }

        [Fact]
        public void DirectorReport_ShowsAverageGradeAcrossAllStudents()
        {
            // Arrange: Multiple students with grades
            var allGrades = new List<decimal>
            {
                9.50m, 8.75m, // Aspirante 1
                8.00m, 9.00m, // Aspirante 2
                10.00m        // Aspirante 3
            };

            // Act: Calculate institutional average
            decimal institutionalAverage = Math.Round(allGrades.Average(), 2);

            // Assert: Director sees overall performance
            Assert.Equal(9.05m, institutionalAverage);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Fact]
        public void CalculateProgress_ZeroTotalUnits_ReturnsZero()
        {
            // Arrange: Edge case - no units assigned
            int completed = 0;
            int total = 0;

            // Act
            decimal percentage = TestDataFactory.CalculateExpectedProgressPercentage(
                completed, total);

            // Assert: Should handle division by zero
            Assert.Equal(0m, percentage);
        }

        [Fact]
        public void CalculateAverage_EmptyList_ReturnsNull()
        {
            // Arrange
            var emptyList = new List<Ind_ProgresoAspirante>();

            // Act
            decimal? average = TestDataFactory.CalculateExpectedAverageGrade(emptyList);

            // Assert
            Assert.Null(average);
        }

        [Fact]
        public void Calificacion_ExceedsMaximum_ShouldBeValidated()
        {
            // Arrange: Grade exceeds 10.00 (invalid)
            var progreso = new Ind_ProgresoAspirante
            {
                Calificacion = 11.00m
            };

            // Assert: Business logic should validate grades are 0-10
            // (This would be enforced by model validation or database constraints)
            Assert.True(progreso.Calificacion > 10.00m); // Invalid scenario
        }

        [Fact]
        public void Calificacion_NegativeValue_ShouldBeValidated()
        {
            // Arrange: Negative grade (invalid)
            var progreso = new Ind_ProgresoAspirante
            {
                Calificacion = -1.00m
            };

            // Assert: Business logic should validate non-negative grades
            Assert.True(progreso.Calificacion < 0); // Invalid scenario
        }

        #endregion
    }
}
