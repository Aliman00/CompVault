using System.Net;
using System.Net.Http.Json;

using CompVault.Frontend.Common.Extensions;
using CompVault.Frontend.Tests.Common;
using CompVault.Shared.Result;

using FluentAssertions;

namespace CompVault.Frontend.Tests.Frontend.Common.Extensions;

public class HttpClientExtensionsTests
{
    /// <summary>
    /// Tester happy path - at bodyen til responsen returneres i value-feltet til Result med Success
    /// </summary>
    [Fact]
    public async Task ParseResponseAsync_ResponseIsSuccess_ReturnsResultWithBody()
    {
        // Arrange - Bygger en response backend sender
        TestDataFactory.TestResponse response = TestDataFactory.BuildTestResponse();

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(response)
        };

        // Act
        Result<TestDataFactory.TestResponse> result =
            await HttpClientExtensions.ParseResponseAsync<TestDataFactory.TestResponse>(httpResponseMessage,
                CancellationToken.None);

        // Assert - Sjekker at Result-objektet og backend-responsen fra body er i value
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be(response.Name);
        result.Value!.Value.Should().Be(response.Value);
    }

    /// <summary>
    /// Tester at vi leser et ProblemDetails-korrekt med ReadProblemDetailASync og at vi får korrekt Error i
    /// Result
    /// </summary>
    [Fact]
    public async Task ParseResponseAsync_ResponseIsFailure_ReturnsCorrectErrorCode()
    {
        // Arrange - Bygger ProblemDetail og HttpResponseMessage som metoden krever
        ProblemDetail problemDetail = TestDataFactory.BuildProblemDetail(
            403, nameof(ErrorCode.Validation), "Name must be between 1-60 characters");

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(problemDetail)
        };

        // Act
        Result<TestDataFactory.TestResponse> result =
            await HttpClientExtensions.ParseResponseAsync<TestDataFactory.TestResponse>(httpResponseMessage,
                CancellationToken.None);

        // Assert - Sjekker at result-feltene er bygget korrekt ifra ProblemDetail-objektet
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Validation);
        result.Error.Message.Should().Be(problemDetail.Message);
    }

    /// <summary>
    /// Tester at vi error-koder som ikke eksisterer blir til ErrorCode.Unkown
    /// </summary>
    [Fact]
    public async Task ParseResponseAsync_ResponseIsFailureAndErrorCodeDoesNotExist_ReturnsErrorCodeUnknown()
    {
        // Arrange - Bygger et ProblemDetail med en ikke-eksisterende feilmelding
        ProblemDetail problemDetail = TestDataFactory.BuildProblemDetail(
            403, "TilfeldigUkjentErrorKode", "Name must be between 1-60 characters");

        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = JsonContent.Create(problemDetail)
        };

        // Act
        Result<TestDataFactory.TestResponse> result =
            await HttpClientExtensions.ParseResponseAsync<TestDataFactory.TestResponse>(httpResponseMessage,
                CancellationToken.None);

        // Assert - Sjekker at ErrorCode er Unknown
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Unknown);
    }

    /// <summary>
    /// Tester at ingen body gir ErrorCode Unknown selvom det er en vellykket Status kode.
    /// ParseResponseAsync forventer et objekt i bodyen, og ParseEmptyResponseAsync skal brukes uten body
    /// </summary>
    [Fact]
    public async Task ParseResponseAsync_ResponseLacksBody_ReturnsErrorCodeUnknown()
    {
        // Arrange - Setter ingen body på responsen
        var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        Result<TestDataFactory.TestResponse> result =
            await HttpClientExtensions.ParseResponseAsync<TestDataFactory.TestResponse>(httpResponseMessage,
                CancellationToken.None);

        // Assert - Sjekker at ErrorCode.Unkown har blitt satt korrekt
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be(ErrorCode.Unknown);
    }
}