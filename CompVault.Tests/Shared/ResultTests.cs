using CompVault.Shared.Result;

namespace CompVault.Tests.Shared;

public class ResultTests
{
    // ============================ Generic Result<T> ============================ 
    /// <summary>
    /// Sjekker at Result ved Success gir riktig datatype, IsSuccess er true, IsFailure er false og ingen error
    /// </summary>
    [Fact]
    public void ResultT_Success_ShouldBeSuccess()
    {
        // Arrange
        var message = "hello";

        // Act
        var result = Result<string>.Success(message);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(message, result.Value);
        Assert.Null(result.Error);
    }

    /// <summary>
    /// Sjekker at Result ved Success gir ingen datatype, IsSuccess er false, IsFailure er true og riktig error
    /// </summary>
    [Fact]
    public void ResultT_Failure_ShouldBeFailure()
    {
        // Arrange
        var error = AppError.Conflict("User already exist in DB");

        // Act
        var result = Result<string>.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Null(result.Value);
    }

    // ============================ Non-generic Result<T> ============================ 
    /// <summary>
    /// Sjekker at Result ved Success så er IsSuccess true, IsFailure er false og ingen error
    /// </summary>
    [Fact]
    public void Result_Success_ShouldBeSuccess()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    /// <summary>
    /// Sjekker at Result ved Failure så er IsSuccess er false, IsFailure er true og riktig error
    /// </summary>
    [Fact]
    public void Result_Failure_ShouldBeFailure()
    {
        // Arrange
        var error = AppError.Conflict("User already exist in DB");

        // Act
        var result = Result.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }
}
