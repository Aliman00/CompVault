namespace CompVault.Tests.Backend.Common.Middleware;

public class GlobalExceptionHandlerTests
{
    private readonly GlobalExce
    
    [Theory]
    public async Task GlobalExceptionHandler_DifferentExceptionType_ReturnsCorrectResponse(
        Type exceptionType, int expectedStatus, string expectedCode)
    {
        // Arrange
        var logger = Substitute.
    }
}