using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GateKeeper.Server.Middleware;
using GateKeeper.Server.Models.Common;
using GateKeeper.Server.Exceptions;
using System.Diagnostics;

namespace GateKeeper.Server.Test.Middleware
{
    [TestClass]
    public class GlobalExceptionHandlerMiddlewareTests
    {
        private Mock<ILogger<GlobalExceptionHandlerMiddleware>> _mockLogger;
        private Mock<IHostEnvironment> _mockHostEnvironment;
        private DefaultHttpContext _httpContext;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockLogger = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
            _mockHostEnvironment = new Mock<IHostEnvironment>();
            _httpContext = new DefaultHttpContext();
            _httpContext.Response.Body = new MemoryStream(); // Important for capturing response
        }

        private async Task<ErrorResponse?> GetErrorResponseAsync(HttpResponse response)
        {
            response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(response.Body);
            var responseBody = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(responseBody))
            {
                return null;
            }
            try
            {
                 return JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize ErrorResponse: {ex.Message}. Response body: {responseBody}");
                return null;
            }
        }

        [TestMethod]
        public async Task InvokeAsync_NoExceptionOccurs_CallsNextAndReturnsOk()
        {
            // Arrange
            var mockRequestDelegate = new Mock<RequestDelegate>();
            mockRequestDelegate.Setup(next => next(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

            var middleware = new GlobalExceptionHandlerMiddleware(
                next: mockRequestDelegate.Object,
                logger: _mockLogger.Object,
                env: _mockHostEnvironment.Object
            );

            var initialStatusCode = _httpContext.Response.StatusCode; // Usually 200 by default

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            mockRequestDelegate.Verify(next => next(It.IsAny<HttpContext>()), Times.Once);
            Assert.AreEqual(initialStatusCode, _httpContext.Response.StatusCode); // Status code should not be changed

            // Verify no error logging occurred
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error || l == LogLevel.Warning), // Check for Error or Warning
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Never);
        }

        [TestMethod]
        public async Task InvokeAsync_GenericExceptionInDevelopment_Returns500WithDetails()
        {
            // Arrange
            _mockHostEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            var exceptionMessage = "Test generic exception in Development";
            RequestDelegate nextDelegate = (ctx) => throw new Exception(exceptionMessage);

            var middleware = new GlobalExceptionHandlerMiddleware(
                next: nextDelegate,
                logger: _mockLogger.Object,
                env: _mockHostEnvironment.Object
            );

            var activity = new Activity("TestActivity").Start(); // To ensure Activity.Current.Id is not null
            _httpContext.TraceIdentifier = "TestTraceId";


            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.AreEqual(StatusCodes.Status500InternalServerError, _httpContext.Response.StatusCode);
            Assert.AreEqual("application/json", _httpContext.Response.ContentType);

            var errorResponse = await GetErrorResponseAsync(_httpContext.Response);
            Assert.IsNotNull(errorResponse);
            Assert.AreEqual(StatusCodes.Status500InternalServerError, errorResponse.StatusCode);
            Assert.AreEqual("An unexpected internal server error occurred. Please try again later.", errorResponse.Message);
            Assert.IsNotNull(errorResponse.Details);
            Assert.IsTrue(errorResponse.Details.Contains(exceptionMessage));
            Assert.IsNotNull(errorResponse.TraceId);
            Assert.IsTrue(errorResponse.TraceId.Contains(activity.Id!) || errorResponse.TraceId == "TestTraceId");


            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An unhandled exception has occurred.")),
                   It.IsAny<Exception>(),
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);

            activity.Stop();
        }

        [TestMethod]
        public async Task InvokeAsync_GenericExceptionInProduction_Returns500WithoutDetails()
        {
            // Arrange
            _mockHostEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Production);
            var exceptionMessage = "Test generic exception in Production";
            RequestDelegate nextDelegate = (ctx) => throw new Exception(exceptionMessage);

            var middleware = new GlobalExceptionHandlerMiddleware(
                next: nextDelegate,
                logger: _mockLogger.Object,
                env: _mockHostEnvironment.Object
            );

            var activity = new Activity("TestActivity").Start();
            _httpContext.TraceIdentifier = "TestTraceId";

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.AreEqual(StatusCodes.Status500InternalServerError, _httpContext.Response.StatusCode);
            Assert.AreEqual("application/json", _httpContext.Response.ContentType);

            var errorResponse = await GetErrorResponseAsync(_httpContext.Response);
            Assert.IsNotNull(errorResponse);
            Assert.AreEqual(StatusCodes.Status500InternalServerError, errorResponse.StatusCode);
            Assert.AreEqual("An unexpected internal server error occurred. Please try again later.", errorResponse.Message);
            Assert.IsNull(errorResponse.Details, "Details should be null in Production environment.");
            Assert.IsNotNull(errorResponse.TraceId);
            Assert.IsTrue(errorResponse.TraceId.Contains(activity.Id!) || errorResponse.TraceId == "TestTraceId");

            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Error,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An unhandled exception has occurred.")),
                   It.IsAny<Exception>(),
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
            activity.Stop();
        }

        [TestMethod]
        public async Task InvokeAsync_ValidationExceptionOccurs_Returns400WithExceptionMessage()
        {
            // Arrange
            _mockHostEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development); // Env shouldn't matter for details here
            var exceptionMessage = "Test validation exception";
            RequestDelegate nextDelegate = (ctx) => throw new ValidationException(exceptionMessage);

            var middleware = new GlobalExceptionHandlerMiddleware(
                next: nextDelegate,
                logger: _mockLogger.Object,
                env: _mockHostEnvironment.Object
            );
            
            var activity = new Activity("TestActivity").Start();
            _httpContext.TraceIdentifier = "TestTraceId";

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.AreEqual(StatusCodes.Status400BadRequest, _httpContext.Response.StatusCode);
            Assert.AreEqual("application/json", _httpContext.Response.ContentType);

            var errorResponse = await GetErrorResponseAsync(_httpContext.Response);
            Assert.IsNotNull(errorResponse);
            Assert.AreEqual(StatusCodes.Status400BadRequest, errorResponse.StatusCode);
            Assert.AreEqual(exceptionMessage, errorResponse.Message); // Custom message
            Assert.IsNotNull(errorResponse.Details, "Details should be present in Development for custom exceptions too if desired.");
            Assert.IsTrue(errorResponse.Details.Contains(exceptionMessage));
            Assert.IsNotNull(errorResponse.TraceId);
            Assert.IsTrue(errorResponse.TraceId.Contains(activity.Id!) || errorResponse.TraceId == "TestTraceId");

             _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning, // Should be LogWarning for ValidationException
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Validation error.")),
                    It.IsAny<ValidationException>(), // Specific exception type
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
            activity.Stop();
        }

        [TestMethod]
        public async Task InvokeAsync_ResourceNotFoundExceptionOccurs_Returns404WithExceptionMessage()
        {
            // Arrange
            _mockHostEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            var exceptionMessage = "Test resource not found exception";
            RequestDelegate nextDelegate = (ctx) => throw new ResourceNotFoundException(exceptionMessage);

            var middleware = new GlobalExceptionHandlerMiddleware(
                next: nextDelegate,
                logger: _mockLogger.Object,
                env: _mockHostEnvironment.Object
            );

            var activity = new Activity("TestActivity").Start();
            _httpContext.TraceIdentifier = "TestTraceId";

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.AreEqual(StatusCodes.Status404NotFound, _httpContext.Response.StatusCode);
            Assert.AreEqual("application/json", _httpContext.Response.ContentType);

            var errorResponse = await GetErrorResponseAsync(_httpContext.Response);
            Assert.IsNotNull(errorResponse);
            Assert.AreEqual(StatusCodes.Status404NotFound, errorResponse.StatusCode);
            Assert.AreEqual(exceptionMessage, errorResponse.Message);
            Assert.IsNotNull(errorResponse.Details);
            Assert.IsTrue(errorResponse.Details.Contains(exceptionMessage));
            Assert.IsNotNull(errorResponse.TraceId);
            Assert.IsTrue(errorResponse.TraceId.Contains(activity.Id!) || errorResponse.TraceId == "TestTraceId");

            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Warning, 
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Resource not found.")),
                   It.IsAny<ResourceNotFoundException>(), 
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
            activity.Stop();
        }

        [TestMethod]
        public async Task InvokeAsync_BusinessRuleExceptionOccurs_Returns400WithExceptionMessage()
        {
            // Arrange
            _mockHostEnvironment.Setup(e => e.EnvironmentName).Returns(Environments.Development);
            var exceptionMessage = "Test business rule exception";
            RequestDelegate nextDelegate = (ctx) => throw new BusinessRuleException(exceptionMessage);

            var middleware = new GlobalExceptionHandlerMiddleware(
                next: nextDelegate,
                logger: _mockLogger.Object,
                env: _mockHostEnvironment.Object
            );

            var activity = new Activity("TestActivity").Start();
            _httpContext.TraceIdentifier = "TestTraceId";

            // Act
            await middleware.InvokeAsync(_httpContext);

            // Assert
            Assert.AreEqual(StatusCodes.Status400BadRequest, _httpContext.Response.StatusCode);
            Assert.AreEqual("application/json", _httpContext.Response.ContentType);

            var errorResponse = await GetErrorResponseAsync(_httpContext.Response);
            Assert.IsNotNull(errorResponse);
            Assert.AreEqual(StatusCodes.Status400BadRequest, errorResponse.StatusCode);
            Assert.AreEqual(exceptionMessage, errorResponse.Message);
            Assert.IsNotNull(errorResponse.Details);
            Assert.IsTrue(errorResponse.Details.Contains(exceptionMessage));
            Assert.IsNotNull(errorResponse.TraceId);
            Assert.IsTrue(errorResponse.TraceId.Contains(activity.Id!) || errorResponse.TraceId == "TestTraceId");

            _mockLogger.Verify(
               x => x.Log(
                   LogLevel.Warning,
                   It.IsAny<EventId>(),
                   It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Business rule violation.")),
                   It.IsAny<BusinessRuleException>(),
                   It.IsAny<Func<It.IsAnyType, Exception, string>>()),
               Times.Once);
            activity.Stop();
        }
    }
}
