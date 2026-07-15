using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.SessionState;
using induccion_refactorization.Filters;
using Moq;
using Xunit;

namespace InduccionRefactorization.Tests.Filters
{
    /// <summary>
    /// PHASE 2 TESTS: Role-Based Authorization Filter
    /// Tests RoleAuthorizeAttribute functionality for restricting
    /// access based on role IDs and preventing URL tampering
    /// </summary>
    public class RoleAuthorizeAttributeTests
    {
        private Mock<HttpContextBase> _mockHttpContext;
        private Mock<HttpSessionStateBase> _mockSession;
        private Mock<HttpRequestBase> _mockRequest;

        public RoleAuthorizeAttributeTests()
        {
            _mockHttpContext = new Mock<HttpContextBase>();
            _mockSession = new Mock<HttpSessionStateBase>();
            _mockRequest = new Mock<HttpRequestBase>();

            _mockHttpContext.Setup(x => x.Session).Returns(_mockSession.Object);
            _mockHttpContext.Setup(x => x.Request).Returns(_mockRequest.Object);
        }

        #region Authorization Success Tests

        [Fact]
        public void AuthorizeCore_AdminRole_AllowedForAdminOnly()
        {
            // Arrange: RoleAuthorize(1) - Admin only
            var attribute = new RoleAuthorizeAttribute(1);
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockSession.Setup(x => x["RolID"]).Returns(1); // Admin

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AuthorizeCore_CoordinadorRole_AllowedForAdminAndCoordinador()
        {
            // Arrange: RoleAuthorize(1, 3) - Admin and Coordinador
            var attribute = new RoleAuthorizeAttribute(1, 3);
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockSession.Setup(x => x["RolID"]).Returns(3); // Coordinador

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(1)] // Admin
        [InlineData(3)] // Coordinador
        public void AuthorizeCore_MultipleRoles_AllowsBothAdminAndCoordinador(int rolID)
        {
            // Arrange: InductionMaintenanceController uses [RoleAuthorize(1, 3)]
            var attribute = new RoleAuthorizeAttribute(1, 3);
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockSession.Setup(x => x["RolID"]).Returns(rolID);

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AuthorizeCore_AspiranteRole_AllowedForAspiranteController()
        {
            // Arrange: AspiranteController uses [RoleAuthorize(4)]
            var attribute = new RoleAuthorizeAttribute(4);
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockSession.Setup(x => x["RolID"]).Returns(4); // Aspirante

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert
            Assert.True(result);
        }

        #endregion

        #region Authorization Failure Tests (URL Tampering Protection)

        [Fact]
        public void AuthorizeCore_Unauthenticated_Denies()
        {
            // Arrange: User not authenticated
            var attribute = new RoleAuthorizeAttribute(1, 3);
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(false);

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AuthorizeCore_NoSession_Denies()
        {
            // Arrange: Session is null
            var attribute = new RoleAuthorizeAttribute(1, 3);
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockHttpContext.Setup(x => x.Session).Returns((HttpSessionStateBase)null);

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AuthorizeCore_NoRolIDInSession_Denies()
        {
            // Arrange: RolID not set in session
            var attribute = new RoleAuthorizeAttribute(1, 3);
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockSession.Setup(x => x["RolID"]).Returns(null);

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AuthorizeCore_WrongRole_Denies()
        {
            // Arrange: Aspirante trying to access InductionMaintenance
            var attribute = new RoleAuthorizeAttribute(1, 3); // Admin and Coordinador only
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockSession.Setup(x => x["RolID"]).Returns(4); // Aspirante (not allowed)

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert: Should deny access
            Assert.False(result);
        }

        [Fact]
        public void AuthorizeCore_DirectorTryingAccessCoordinatorArea_Denies()
        {
            // Arrange: Director (RolID=2) trying to access Coordinador-only area
            var attribute = new RoleAuthorizeAttribute(3); // Coordinador only
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockSession.Setup(x => x["RolID"]).Returns(2); // Director

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AuthorizeCore_AspiranteTryingAccessAdminArea_Denies()
        {
            // Arrange: CRITICAL TEST - Prevent URL tampering
            // Aspirante manually navigating to /Admin/Index
            var attribute = new RoleAuthorizeAttribute(1); // Admin only
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockSession.Setup(x => x["RolID"]).Returns(4); // Aspirante

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert: Must deny to prevent privilege escalation
            Assert.False(result);
        }

        #endregion

        #region Invalid Session Data Tests

        [Fact]
        public void AuthorizeCore_InvalidRolIDType_Denies()
        {
            // Arrange: RolID is string instead of int
            var attribute = new RoleAuthorizeAttribute(1, 3);
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockSession.Setup(x => x["RolID"]).Returns("invalid");

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AuthorizeCore_NegativeRolID_Denies()
        {
            // Arrange: Invalid negative role ID
            var attribute = new RoleAuthorizeAttribute(1, 3);
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockSession.Setup(x => x["RolID"]).Returns(-1);

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AuthorizeCore_RolIDZero_Denies()
        {
            // Arrange: Invalid zero role ID
            var attribute = new RoleAuthorizeAttribute(1, 3);
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockSession.Setup(x => x["RolID"]).Returns(0);

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Role Combination Tests

        [Theory]
        [InlineData(1, new[] { 1, 2, 3, 4 }, true)]  // Admin in all roles
        [InlineData(2, new[] { 1, 2, 3, 4 }, true)]  // Director in all roles
        [InlineData(3, new[] { 1, 2, 3, 4 }, true)]  // Coordinador in all roles
        [InlineData(4, new[] { 1, 2, 3, 4 }, true)]  // Aspirante in all roles
        [InlineData(1, new[] { 2, 3, 4 }, false)]    // Admin NOT in Director/Coord/Asp
        [InlineData(4, new[] { 1, 2, 3 }, false)]    // Aspirante NOT in Admin/Director/Coord
        public void AuthorizeCore_VariousRoleCombinations_WorksCorrectly(
            int userRolID, int[] allowedRoles, bool expectedResult)
        {
            // Arrange
            var attribute = new RoleAuthorizeAttribute(allowedRoles);
            
            _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            _mockSession.Setup(x => x["RolID"]).Returns(userRolID);

            // Act
            bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        #endregion

        #region Production Scenario Tests

        [Fact]
        public void AuthorizeCore_InductionMaintenanceController_OnlyAdminAndCoordinador()
        {
            // Arrange: Simulates [RoleAuthorize(1, 3)] on InductionMaintenanceController
            var attribute = new RoleAuthorizeAttribute(1, 3);

            // Test all 4 roles
            var testCases = new[]
            {
                new { RolID = 1, ExpectedResult = true },  // Admin ✓
                new { RolID = 2, ExpectedResult = false }, // Director ✗
                new { RolID = 3, ExpectedResult = true },  // Coordinador ✓
                new { RolID = 4, ExpectedResult = false }  // Aspirante ✗
            };

            foreach (var testCase in testCases)
            {
                // Arrange
                _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
                _mockSession.Setup(x => x["RolID"]).Returns(testCase.RolID);

                // Act
                bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

                // Assert
                Assert.Equal(testCase.ExpectedResult, result);
            }
        }

        [Fact]
        public void AuthorizeCore_AspiranteController_OnlyAspirante()
        {
            // Arrange: Simulates [RoleAuthorize(4)] on AspiranteController
            var attribute = new RoleAuthorizeAttribute(4);

            // Test all 4 roles
            var testCases = new[]
            {
                new { RolID = 1, ExpectedResult = false }, // Admin ✗
                new { RolID = 2, ExpectedResult = false }, // Director ✗
                new { RolID = 3, ExpectedResult = false }, // Coordinador ✗
                new { RolID = 4, ExpectedResult = true }   // Aspirante ✓
            };

            foreach (var testCase in testCases)
            {
                // Arrange
                _mockRequest.Setup(x => x.IsAuthenticated).Returns(true);
                _mockSession.Setup(x => x["RolID"]).Returns(testCase.RolID);

                // Act
                bool result = attribute.AuthorizeCore(_mockHttpContext.Object);

                // Assert
                Assert.Equal(testCase.ExpectedResult, result);
            }
        }

        #endregion
    }
}
