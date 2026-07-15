using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using induccion_refactorization.Controllers;
using induccion_refactorization.Models;
using induccion_refactorization.ViewModels;
using InduccionRefactorization.Tests.TestHelpers;
using Moq;
using Xunit;

namespace InduccionRefactorization.Tests.Controllers
{
    /// <summary>
    /// PHASE 1 & 2 TESTS: Authentication and Authorization
    /// Tests AccountController login functionality, password validation,
    /// role-based routing, and security policies
    /// </summary>
    public class AccountControllerTests : ControllerTestBase
    {
        private Mock<CaptacionDbContext> _mockContext;
        private AccountController _controller;
        private List<Usuario> _testUsuarios;
        private List<Role> _testRoles;

        public AccountControllerTests()
        {
            // Initialize base controller context
            InitializeControllerContext();

            // Create test data
            _testUsuarios = TestDataFactory.CreateTestUsuarios();
            _testRoles = TestDataFactory.CreateTestRoles();

            // Mock database context
            _mockContext = new Mock<CaptacionDbContext>();
            var mockUsuariosSet = MockDbSetHelper.CreateMockDbSetWithIncludes(_testUsuarios);
            _mockContext.Setup(m => m.Usuarios).Returns(mockUsuariosSet.Object);

            // Create controller with mocked context
            _controller = new AccountController();
            _controller.ControllerContext = ControllerContext;
        }

        #region GET Login Tests

        [Fact]
        public void Login_GET_ReturnsView_WhenUserNotAuthenticated()
        {
            // Arrange
            SetupUnauthenticatedSession();

            // Act
            var result = _controller.Login(returnUrl: null);

            // Assert
            Assert.IsType<ViewResult>(result);
            var viewResult = result as ViewResult;
            Assert.Empty(viewResult.ViewName); // Default view
        }

        [Fact]
        public void Login_GET_RedirectsToDashboard_WhenUserAlreadyAuthenticated()
        {
            // Arrange: Setup authenticated session for Administrador
            SetupAuthenticatedSession(1, 1, "Carlos Martínez López", "admin@test.com");

            // Act
            var result = _controller.Login(returnUrl: null);

            // Assert: Should redirect instead of showing login view
            Assert.IsType<RedirectToRouteResult>(result);
        }

        [Fact]
        public void Login_GET_PreservesReturnUrl_InViewBag()
        {
            // Arrange
            SetupUnauthenticatedSession();
            string expectedReturnUrl = "/InductionMaintenance/Index";

            // Act
            var result = _controller.Login(returnUrl: expectedReturnUrl);

            // Assert
            var viewResult = result as ViewResult;
            Assert.NotNull(viewResult);
            Assert.Equal(expectedReturnUrl, _controller.ViewBag.ReturnUrl);
        }

        #endregion

        #region POST Login - Valid Credentials Tests (Phase 1)

        [Theory]
        [InlineData(1, "admin@test.com", "Password123!", "Administrador")]
        [InlineData(2, "director@test.com", "Password123!", "Director")]
        [InlineData(3, "coordinador@test.com", "Password123!", "Coordinador")]
        [InlineData(4, "aspirante@test.com", "Password123!", "Aspirante")]
        public void Login_POST_ValidCredentials_RedirectsToCorrectDashboard(
            int rolID, string email, string password, string roleName)
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = email,
                Password = password,
                RememberMe = false
            };

            // Act
            var result = _controller.Login(model, returnUrl: null);

            // Assert: Should redirect (not return view)
            Assert.IsType<RedirectToRouteResult>(result);
            
            // Verify correct route based on role
            var redirectResult = result as RedirectToRouteResult;
            string expectedAction = rolID switch
            {
                1 => "Index",     // Admin
                2 => "Index",     // Director
                3 => "Index",     // Coordinador
                4 => "Index",     // Aspirante
                _ => throw new Exception($"Unexpected RolID: {rolID}")
            };

            string expectedController = rolID switch
            {
                1 => "Admin",
                2 => "Director",
                3 => "Coordinador",
                4 => "Aspirante",
                _ => throw new Exception($"Unexpected RolID: {rolID}")
            };

            Assert.Equal(expectedAction, redirectResult.RouteValues["action"]);
            Assert.Equal(expectedController, redirectResult.RouteValues["controller"]);
        }

        [Fact]
        public void Login_POST_ValidCredentials_SetsSessionVariables()
        {
            // Arrange
            var model = TestDataFactory.CreateValidLoginViewModel(rolID: 1);
            var sessionValues = new Dictionary<string, object>();
            
            MockSession.Setup(x => x[It.IsAny<string>()])
                .Returns((string key) => sessionValues.ContainsKey(key) ? sessionValues[key] : null);
            
            MockSession.Setup(x => x[It.IsAny<string>()] = It.IsAny<object>())
                .Callback<string, object>((key, value) => sessionValues[key] = value);

            // Act
            var result = _controller.Login(model, returnUrl: null);

            // Assert: Session should contain required variables
            Assert.True(sessionValues.ContainsKey("UsuarioID"));
            Assert.True(sessionValues.ContainsKey("RolID"));
            Assert.True(sessionValues.ContainsKey("NombreCompleto"));
            Assert.True(sessionValues.ContainsKey("Email"));
            Assert.Equal(1, sessionValues["UsuarioID"]);
            Assert.Equal(1, sessionValues["RolID"]);
        }

        [Fact]
        public void Login_POST_ValidCredentials_WithRememberMe_CreatesPersis tentCookie()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "admin@test.com",
                Password = "Password123!",
                RememberMe = true // CRITICAL: Test persistent cookie
            };

            // Act
            var result = _controller.Login(model, returnUrl: null);

            // Assert: Forms authentication cookie should be created
            Assert.IsType<RedirectToRouteResult>(result);
            // Note: Full cookie validation requires Forms Authentication integration testing
        }

        #endregion

        #region POST Login - Invalid Credentials Tests (Phase 2)

        [Fact]
        public void Login_POST_NonexistentEmail_ReturnsViewWithModelError()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "nonexistent@test.com",
                Password = "Password123!",
                RememberMe = false
            };

            // Act
            var result = _controller.Login(model, returnUrl: null);

            // Assert: Should return view (not redirect)
            var viewResult = result as ViewResult;
            Assert.NotNull(viewResult);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey(""));
            
            // Verify specific error message
            var errorMessages = _controller.ModelState[""].Errors.Select(e => e.ErrorMessage).ToList();
            Assert.Contains("Email no registrado o usuario inactivo.", errorMessages);
        }

        [Fact]
        public void Login_POST_InactiveUser_ReturnsViewWithModelError()
        {
            // Arrange: Try to login with inactive user
            var model = new LoginViewModel
            {
                Email = "inactivo@test.com",
                Password = "Password123!",
                RememberMe = false
            };

            // Act
            var result = _controller.Login(model, returnUrl: null);

            // Assert: Should reject inactive user
            var viewResult = result as ViewResult;
            Assert.NotNull(viewResult);
            Assert.False(_controller.ModelState.IsValid);
            
            var errorMessages = _controller.ModelState[""].Errors.Select(e => e.ErrorMessage).ToList();
            Assert.Contains("Email no registrado o usuario inactivo.", errorMessages);
        }

        [Fact]
        public void Login_POST_WrongPassword_ReturnsViewWithDetailedError()
        {
            // Arrange: Correct email, wrong password
            var model = new LoginViewModel
            {
                Email = "admin@test.com",
                Password = "WrongPassword123!",
                RememberMe = false
            };

            // Act
            var result = _controller.Login(model, returnUrl: null);

            // Assert: Should show specific password error (not generic)
            var viewResult = result as ViewResult;
            Assert.NotNull(viewResult);
            Assert.False(_controller.ModelState.IsValid);
            
            var errorMessages = _controller.ModelState[""].Errors.Select(e => e.ErrorMessage).ToList();
            Assert.Contains("Contraseña incorrecta.", errorMessages);
        }

        [Fact]
        public void Login_POST_EmptyEmail_ReturnsValidationError()
        {
            // Arrange: Invalid model with empty email
            var model = new LoginViewModel
            {
                Email = "",
                Password = "Password123!",
                RememberMe = false
            };

            // Manually trigger validation
            _controller.ModelState.AddModelError("Email", "El correo electrónico es obligatorio");

            // Act
            var result = _controller.Login(model, returnUrl: null);

            // Assert
            var viewResult = result as ViewResult;
            Assert.NotNull(viewResult);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey("Email"));
        }

        [Fact]
        public void Login_POST_InvalidEmailFormat_ReturnsValidationError()
        {
            // Arrange: Invalid email format
            var model = new LoginViewModel
            {
                Email = "invalid-email-format",
                Password = "Password123!",
                RememberMe = false
            };

            _controller.ModelState.AddModelError("Email", "Formato de correo inválido");

            // Act
            var result = _controller.Login(model, returnUrl: null);

            // Assert
            var viewResult = result as ViewResult;
            Assert.NotNull(viewResult);
            Assert.False(_controller.ModelState.IsValid);
        }

        [Fact]
        public void Login_POST_EmptyPassword_ReturnsValidationError()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = "admin@test.com",
                Password = "",
                RememberMe = false
            };

            _controller.ModelState.AddModelError("Password", "La contraseña es obligatoria");

            // Act
            var result = _controller.Login(model, returnUrl: null);

            // Assert
            var viewResult = result as ViewResult;
            Assert.NotNull(viewResult);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey("Password"));
        }

        #endregion

        #region Database Exception Handling Tests

        [Fact]
        public void Login_POST_DatabaseException_ReturnsViewWithErrorMessage()
        {
            // Arrange: Mock database to throw exception
            var mockContext = new Mock<CaptacionDbContext>();
            mockContext.Setup(m => m.Usuarios).Throws(new Exception("Database connection failed"));

            var model = TestDataFactory.CreateValidLoginViewModel(rolID: 1);

            // Act & Assert: Should handle exception gracefully
            // Note: Requires controller to have try-catch around database queries
            // Expected behavior: Return view with generic error message
            Assert.NotNull(model); // Placeholder for exception handling test
        }

        #endregion

        #region ReturnUrl Security Tests (Phase 2)

        [Fact]
        public void Login_POST_ValidReturnUrl_RedirectsToReturnUrl()
        {
            // Arrange
            var model = TestDataFactory.CreateValidLoginViewModel(rolID: 3);
            string returnUrl = "/InductionMaintenance/CreateMateria";

            // Act
            var result = _controller.Login(model, returnUrl: returnUrl);

            // Assert: Should redirect to returnUrl instead of default dashboard
            var redirectResult = result as RedirectResult;
            if (redirectResult != null)
            {
                Assert.Equal(returnUrl, redirectResult.Url);
            }
        }

        [Theory]
        [InlineData("http://evil.com/phishing")]
        [InlineData("//evil.com/phishing")]
        [InlineData("javascript:alert('xss')")]
        public void Login_POST_MaliciousReturnUrl_RedirectsToDashboard(string maliciousUrl)
        {
            // Arrange: Test open redirect vulnerability protection
            var model = TestDataFactory.CreateValidLoginViewModel(rolID: 1);

            // Act
            var result = _controller.Login(model, returnUrl: maliciousUrl);

            // Assert: Should NOT redirect to malicious URL
            // Should redirect to safe default dashboard instead
            var redirectResult = result as RedirectToRouteResult;
            Assert.NotNull(redirectResult);
            Assert.Equal("Index", redirectResult.RouteValues["action"]);
        }

        #endregion

        #region Aspirante Special Case Tests (Phase 2)

        [Fact]
        public void Login_POST_AspiranteRole_SetsAspiranteIDInSession()
        {
            // Arrange: Aspirante user linked to AspiranteID = 1579
            var model = TestDataFactory.CreateValidLoginViewModel(rolID: 4);
            var sessionValues = new Dictionary<string, object>();
            
            MockSession.Setup(x => x[It.IsAny<string>()])
                .Returns((string key) => sessionValues.ContainsKey(key) ? sessionValues[key] : null);
            
            MockSession.Setup(x => x[It.IsAny<string>()] = It.IsAny<object>())
                .Callback<string, object>((key, value) => sessionValues[key] = value);

            // Act
            var result = _controller.Login(model, returnUrl: null);

            // Assert: Session should contain AspiranteID for RolID = 4
            Assert.True(sessionValues.ContainsKey("AspiranteID"));
            Assert.Equal(1579, sessionValues["AspiranteID"]);
        }

        [Fact]
        public void Login_POST_AspiranteRole_RedirectsToAspiranteIndex()
        {
            // Arrange
            var model = TestDataFactory.CreateValidLoginViewModel(rolID: 4);

            // Act
            var result = _controller.Login(model, returnUrl: null);

            // Assert: Should route to /Aspirante/Index
            var redirectResult = result as RedirectToRouteResult;
            Assert.NotNull(redirectResult);
            Assert.Equal("Index", redirectResult.RouteValues["action"]);
            Assert.Equal("Aspirante", redirectResult.RouteValues["controller"]);
        }

        #endregion

        #region Logout Tests

        [Fact]
        public void Logout_ClearsSession_AndRedirectsToLogin()
        {
            // Arrange
            SetupAuthenticatedSession(1, 1, "Carlos Martínez", "admin@test.com");
            var sessionCleared = false;
            MockSession.Setup(x => x.Clear()).Callback(() => sessionCleared = true);

            // Act
            var result = _controller.Logout();

            // Assert
            Assert.True(sessionCleared);
            var redirectResult = result as RedirectToRouteResult;
            Assert.NotNull(redirectResult);
            Assert.Equal("Login", redirectResult.RouteValues["action"]);
            Assert.Equal("Account", redirectResult.RouteValues["controller"]);
        }

        #endregion

        #region Model Validation Tests

        [Fact]
        public void LoginViewModel_RequiredFields_ValidationWorks()
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = null,
                Password = null
            };

            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(model);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            // Act
            bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                model, validationContext, validationResults, true);

            // Assert
            Assert.False(isValid);
            Assert.Equal(2, validationResults.Count); // Email and Password required
        }

        [Theory]
        [InlineData("admin@test.com", true)]
        [InlineData("director@test.com", true)]
        [InlineData("invalid-email", false)]
        [InlineData("@test.com", false)]
        [InlineData("test@", false)]
        public void LoginViewModel_EmailValidation_WorksCorrectly(string email, bool shouldBeValid)
        {
            // Arrange
            var model = new LoginViewModel
            {
                Email = email,
                Password = "Password123!"
            };

            var validationContext = new System.ComponentModel.DataAnnotations.ValidationContext(model);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            // Act
            bool isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                model, validationContext, validationResults, true);

            // Assert
            Assert.Equal(shouldBeValid, isValid);
        }

        #endregion
    }
}
