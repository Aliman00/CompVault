using CompVault.Backend.Controllers;
using CompVault.Shared.Result;
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