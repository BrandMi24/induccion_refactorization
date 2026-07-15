using System;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.SessionState;
using Moq;

namespace InduccionRefactorization.Tests.TestHelpers
{
    /// <summary>
    /// Base class for controller tests providing common setup and mocking utilities
    /// Handles HttpContext, Session, Request, and Response mocking
    /// </summary>
    public abstract class ControllerTestBase
    {
        protected Mock<HttpContextBase> MockHttpContext { get; set; }
        protected Mock<HttpRequestBase> MockRequest { get; set; }
        protected Mock<HttpResponseBase> MockResponse { get; set; }
        protected Mock<HttpSessionStateBase> MockSession { get; set; }
        protected Mock<IPrincipal> MockPrincipal { get; set; }
        protected Mock<IIdentity> MockIdentity { get; set; }
        protected ControllerContext ControllerContext { get; set; }

        /// <summary>
        /// Initialize base mocks for controller testing
        /// Call this in test class constructor or setup method
        /// </summary>
        protected void InitializeControllerContext()
        {
            // Mock HttpContext
            MockHttpContext = new Mock<HttpContextBase>();
            MockRequest = new Mock<HttpRequestBase>();
            MockResponse = new Mock<HttpResponseBase>();
            MockSession = new Mock<HttpSessionStateBase>();
            MockPrincipal = new Mock<IPrincipal>();
            MockIdentity = new Mock<IIdentity>();

            // Setup HttpContext relationships
            MockHttpContext.Setup(x => x.Request).Returns(MockRequest.Object);
            MockHttpContext.Setup(x => x.Response).Returns(MockResponse.Object);
            MockHttpContext.Setup(x => x.Session).Returns(MockSession.Object);
            MockHttpContext.Setup(x => x.User).Returns(MockPrincipal.Object);

            // Setup Identity
            MockPrincipal.Setup(x => x.Identity).Returns(MockIdentity.Object);
            MockIdentity.Setup(x => x.IsAuthenticated).Returns(true);

            // Setup default request properties
            MockRequest.Setup(x => x.IsAuthenticated).Returns(true);
            MockRequest.Setup(x => x.Cookies).Returns(new HttpCookieCollection());
            MockRequest.Setup(x => x.ServerVariables).Returns(new NameValueCollection());

            // Setup response cookies
            var responseCookies = new HttpCookieCollection();
            MockResponse.Setup(x => x.Cookies).Returns(responseCookies);

            // Create ControllerContext
            ControllerContext = new ControllerContext(MockHttpContext.Object, new RouteData(), new Mock<ControllerBase>().Object);
        }

        /// <summary>
        /// Setup authenticated session for a specific role
        /// Matches the role-based routing in AccountController
        /// </summary>
        /// <param name="usuarioID">User ID</param>
        /// <param name="rolID">Role ID (1=Admin, 2=Director, 3=Coordinador, 4=Aspirante)</param>
        /// <param name="nombreCompleto">Full name</param>
        /// <param name="email">Email address</param>
        /// <param name="aspiranteID">Aspirante ID (required for RolID=4)</param>
        protected void SetupAuthenticatedSession(int usuarioID, int rolID, string nombreCompleto, string email, int? aspiranteID = null)
        {
            MockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
            MockIdentity.Setup(x => x.Name).Returns(email);
            MockRequest.Setup(x => x.IsAuthenticated).Returns(true);

            // Setup session variables matching AccountController.Login
            MockSession.Setup(x => x["UsuarioID"]).Returns(usuarioID);
            MockSession.Setup(x => x["RolID"]).Returns(rolID);
            MockSession.Setup(x => x["NombreCompleto"]).Returns(nombreCompleto);
            MockSession.Setup(x => x["Email"]).Returns(email);

            if (rolID == 4 && aspiranteID.HasValue)
            {
                MockSession.Setup(x => x["AspiranteID"]).Returns(aspiranteID.Value);
            }
        }

        /// <summary>
        /// Setup unauthenticated session for negative testing
        /// </summary>
        protected void SetupUnauthenticatedSession()
        {
            MockIdentity.Setup(x => x.IsAuthenticated).Returns(false);
            MockRequest.Setup(x => x.IsAuthenticated).Returns(false);
            MockSession.Setup(x => x["UsuarioID"]).Returns(null);
            MockSession.Setup(x => x["RolID"]).Returns(null);
        }

        /// <summary>
        /// Verify that a redirect result goes to the expected action/controller
        /// </summary>
        protected void AssertRedirectToAction(ActionResult result, string expectedAction, string expectedController = null)
        {
            var redirectResult = result as RedirectToRouteResult;
            if (redirectResult == null)
            {
                throw new Exception($"Expected RedirectToRouteResult but got {result?.GetType().Name ?? "null"}");
            }

            var actualAction = redirectResult.RouteValues["action"]?.ToString();
            if (actualAction != expectedAction)
            {
                throw new Exception($"Expected redirect to action '{expectedAction}' but got '{actualAction}'");
            }

            if (expectedController != null)
            {
                var actualController = redirectResult.RouteValues["controller"]?.ToString();
                if (actualController != expectedController)
                {
                    throw new Exception($"Expected redirect to controller '{expectedController}' but got '{actualController}'");
                }
            }
        }

        /// <summary>
        /// Verify that a view result has the expected view name
        /// </summary>
        protected void AssertViewResult(ActionResult result, string expectedViewName = "")
        {
            var viewResult = result as ViewResult;
            if (viewResult == null)
            {
                throw new Exception($"Expected ViewResult but got {result?.GetType().Name ?? "null"}");
            }

            if (!string.IsNullOrEmpty(expectedViewName) && viewResult.ViewName != expectedViewName)
            {
                throw new Exception($"Expected view '{expectedViewName}' but got '{viewResult.ViewName}'");
            }
        }

        /// <summary>
        /// Verify that TempData contains a specific key with expected value
        /// </summary>
        protected void AssertTempDataContains(TempDataDictionary tempData, string key, string expectedValue = null)
        {
            if (!tempData.ContainsKey(key))
            {
                throw new Exception($"TempData does not contain key '{key}'");
            }

            if (expectedValue != null)
            {
                var actualValue = tempData[key]?.ToString();
                if (actualValue != expectedValue)
                {
                    throw new Exception($"TempData['{key}'] expected '{expectedValue}' but got '{actualValue}'");
                }
            }
        }

        /// <summary>
        /// Verify that ModelState has errors
        /// </summary>
        protected void AssertModelStateHasErrors(ModelStateDictionary modelState, string expectedErrorKey = null)
        {
            if (modelState.IsValid)
            {
                throw new Exception("Expected ModelState to have errors but it was valid");
            }

            if (expectedErrorKey != null && !modelState.ContainsKey(expectedErrorKey))
            {
                throw new Exception($"ModelState does not contain expected error key '{expectedErrorKey}'");
            }
        }

        /// <summary>
        /// Verify that ModelState is valid
        /// </summary>
        protected void AssertModelStateIsValid(ModelStateDictionary modelState)
        {
            if (!modelState.IsValid)
            {
                var errors = string.Join(", ", modelState.Values);
                throw new Exception($"Expected ModelState to be valid but found errors: {errors}");
            }
        }
    }
}
