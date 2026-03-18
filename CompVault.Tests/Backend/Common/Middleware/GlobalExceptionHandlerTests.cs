using System.Text.Json;
using CompVault.Backend.Common.Middleware;
using CompVault.Shared.Result;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace CompVault.Tests.Backend.Common.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExceptionHandler _sut;

    public GlobalExceptionHandlerTests()
    {   
        // Konstruktøren trenger kun Logger
        var logger = new Mock<ILogger<GlobalExceptionHandler>>().Object;
        _sut = new GlobalExceptionHandler(logger);
    }
    
    /// <summary>
    /// Oppretter en HttpContext - en HttpContext er et objekt som opprettes når det kommer en http-forespørsel
    /// og den inneholder request, response, user og middlertidig data.
    /// </summary>
    /// <returns>En HttpContext med en MemoryStream, slik at vi kan sjekke hva som ble skrevet i forespørselen</returns>
    private static DefaultHttpContext CreateHttpContext() => 
        new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };
    
    /// <summary>
    /// Hjelempetode for å lese egenskapene fra et ProblemDetail-objekt fra responsne til HttpContexten
    /// </summary>
    /// <param name="response">Http-forespørsel responsen</param>
    /// <returns>Deserialisert ProblemDetail-object</returns>
    private static async Task<ProblemDetail?> ReadProblemDetail(HttpResponse response)
    {
        // Spoler streamen tilbake til start (som en VHS)
        response.Body.Seek(0, SeekOrigin.Begin);
        // Deserialiserer til et ProblemDetails-objekt. Med option for å sikre at store/små bokstaver er urelevant
        return await JsonSerializer.DeserializeAsync<ProblemDetail>(response.Body,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
    

    [Theory]
    [InlineData(typeof(ArgumentException), 400, "Validation")]
    public async Task GlobalExceptionHandler_DifferentExceptionType_ReturnsCorrectResponse(
        Type exceptionType, int expectedStatus, string expectedCode)
    {
        // Arrange - Oppretter en HttpContext og vi bruker Activator.CreateInstance til å opprette ønsket Exception-type
        var context = CreateHttpContext();
        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        
        // Act - kaller metoden med HttpContexten og feilen
        var result = await _sut.TryHandleAsync(context, exception, CancellationToken.None);
        
        // Assert
        result.Should().BeTrue(); // Sjekker at den ble satt som håndtert
        context.Response.StatusCode.Should().Be(expectedStatus); // Sjekker korrekt status
        
        // Leser ProblemDetail-objektet og sjekker egenskapene
        var problemDetail = await ReadProblemDetail(context.Response);
        problemDetail.Should().NotBeNull();
        problemDetail.Status.Should().Be(expectedStatus);
        problemDetail.Code.Should().Be(expectedCode);

    }
    
}