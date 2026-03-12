using CompVault.Backend.Controllers;
using CompVault.Shared.Result;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Tests.Backend.Controllers;

/// <summary>
/// Tester til basecontrolleren
/// </summary>
public class BaseControllerTests
{
    private readonly TestableBaseController _sut = new();
    
    // -------------------------------------------------------------------------
    // HandleFailure<T>
    // -------------------------------------------------------------------------
    /// <summary>
    /// Tester generisk HandleFailure sin exception-throwing ved et Result som er vellykket
    /// </summary>
    [Fact]
    public void HandleFailureT_WhenResultIsSuccess_ThrowsInvalidOperationException()
    {
        // Arrange
        Result<string> result = Result<string>.Success("test");
        
        // Act - Bruker Action for å kalle eventen InvokeHandleFailure med Result-objektet
        Action act = () => _sut.InvokeHandleFailure(result);
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*resultat*");
    }
    
    
    /// <summary>
    /// Tester at generisk HandleFailure returnerer ObjectResult med korrekt statuskode ved et mislykket Result
    /// </summary>
    [Fact]
    public void HandleFailureT_WhenResultIsFailed_ReturnsObjectResult()
    {
        // Arrange - tar en ekte AppError fra AuthService
        Result<string> result = Result<string>.Failure(
            AppError.Create(ErrorCode.InvalidToken, "Ugyldig access token."));
        
        // Act - Vi får et ActionResult objekt fra InvokeHandleFailure
        ActionResult actionResult = _sut.InvokeHandleFailure(result);
        
        // Assert - ActionResult er et ObjectResult-objekt. Tester egenskapene
        var objectResult = actionResult as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult.StatusCode.Should().Be(401);
        objectResult.Value.Should().BeOfType<ProblemDetail>();
    }
    
    // -------------------------------------------------------------------------
    // HandleFailure
    // -------------------------------------------------------------------------
    /// <summary>
    /// Tester ikke-generisk HandleFailure sin exception-throwing ved et Result som er vellykket
    /// </summary>
    [Fact]
    public void HandleFailure_WhenResultIsSuccess_ThrowsInvalidOperationException()
    {
        // Arrange
        Result result = Result.Success();
        
        // Act - Bruker Action for å kalle eventen InvokeHandleFailure med Result-objektet
        Action act = () => _sut.InvokeHandleFailure(result);
        
        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*resultat*");
    }
    
    
    /// <summary>
    /// Tester at ikke-generisk HandleFailure returnerer ObjectResult med korrekt statuskode ved et mislykket Result
    /// </summary>
    [Fact]
    public void HandleFailure_WhenResultIsFailed_ReturnsObjectResult()
    {
        // Arrange - tar en ekte AppError fra AuthService
        Result result = Result.Failure(
            AppError.Create(ErrorCode.InvalidToken, "Ugyldig access token."));
        
        // Act - Vi får et ActionResult objekt fra InvokeHandleFailure
        ActionResult actionResult = _sut.InvokeHandleFailure(result);
        
        // Assert - ActionResult er et ObjectResult-objekt. Tester egenskapene
        var objectResult = actionResult as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult.StatusCode.Should().Be(401);
        objectResult.Value.Should().BeOfType<ProblemDetail>();
    }
    
    // -------------------------------------------------------------------------
    // BuildErrorResponse — HTTP-statuskoder
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester at alle ErrorCode i BuildErrorReponse (oppdatert 12.03) gir riktig StatusCode
    /// </summary>
    /// <param name="code">Appens egne ErrorCodes</param>
    /// <param name="expectedStatusCode">Forventet StatusCode</param>
    [Theory]
    [InlineData(ErrorCode.NotFound,404)]
    [InlineData(ErrorCode.UserNotFound,404)]
    [InlineData(ErrorCode.Conflict,409)]
    [InlineData(ErrorCode.UserAlreadyExists,409)]
    [InlineData(ErrorCode.Unauthorized,401)]
    [InlineData(ErrorCode.InvalidCredentials,401)]
    [InlineData(ErrorCode.TokenExpired,401)]
    [InlineData(ErrorCode.InvalidToken,401)]
    [InlineData(ErrorCode.Forbidden,403)]
    [InlineData(ErrorCode.AccountLocked,403)]
    [InlineData(ErrorCode.AccountInactive,403)]
    [InlineData(ErrorCode.EmailNotConfirmed,403)]
    [InlineData(ErrorCode.EmailSendFailed,500)]
    [InlineData(ErrorCode.Validation,422)]
    [InlineData(ErrorCode.PasswordTooWeak,422)]
    public void HandleFailure_ReturnsCorrectStatusCode(ErrorCode code, int expectedStatusCode)
    {
        // Arrange
        Result result = Result.Failure(AppError.Create(code, "test"));
        
        // Act
        ActionResult actionResult = _sut.InvokeHandleFailure(result);
        
        // Assert - Sjekker at objektet er korrekt og at StatusCode er forventet
        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatusCode);
    }
    
    /// <summary>
    /// Tester en ErrorCode (som ikke eksisterer) som bruker defualt-stien i Switchen
    /// </summary>
    [Fact]
    public void HandleFailure_UnknownErrorCode_Returns400()
    {
        // Arrange - bruker en error som ikke eksisterer
        Result result = Result.Failure(AppError.Create((ErrorCode)9999, "unknown error"));
 
        // Act
        ActionResult actionResult = _sut.InvokeHandleFailure(result);
 
        // Assert
        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }
    
    // -------------------------------------------------------------------------
    // BuildErrorResponse — ProblemDetail-innhold
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Sjekker at egenskapene til ProblemDetails-objektet som blir laget i BuildErrorReponse er korrekt
    /// </summary>
    [Fact]
    public void HandleFailure_ResponseBody_ContainsCorrectProblemDetail()
    {
        // Arrange
        const ErrorCode code = ErrorCode.NotFound;
        const string message = "Resource not found";
        Result result = Result.Failure(AppError.Create(code, message));
        
        // Act
        ActionResult actionResult = _sut.InvokeHandleFailure(result);
        
        // Assert
        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        var problem = objectResult.Value.Should().BeOfType<ProblemDetail>().Subject;
        
        problem.Status.Should().Be(404);
        problem.Status.Should().Be(objectResult.StatusCode);
        problem.Code.Should().Be(nameof(ErrorCode.NotFound));
        problem.Message.Should().Be(message);
    }
}

/// <summary>
/// BaseController er abstract, så vi må opprette en controller vi kan teste på. Vi gjør det
/// med å gjøre den public og arve fra BaseController
/// </summary>
public class TestableBaseController : BaseController
{
    // Wrapper metodene fra protected til Public
    public ActionResult InvokeHandleFailure<T>(Result<T> result)
        => HandleFailure(result);

    public ActionResult InvokeHandleFailure(Result result)
        => HandleFailure(result);
}