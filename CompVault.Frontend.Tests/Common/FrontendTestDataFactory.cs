using CompVault.Shared.Result;

namespace CompVault.Frontend.Tests.Common;

public static class FrontendTestDataFactory
{
    /// <summary>
    /// Bygger et ProbelemDetails-objekt som brukes av flere av tester
    /// </summary>
    /// <param name="statusCode">HTTP-status koden</param>
    /// <param name="errorCode">AppError-kode som string</param>
    /// <param name="message">Feilmeldingen</param>
    public static ProblemDetail BuildProblemDetail(int statusCode, string errorCode, string message) => 
        new ()
        {
            Status = statusCode, Code = errorCode, Message = message
        };
    
    /// <summary>
    /// Bygger em TestResponse (DTO) for å bruke i frontend metoder. Kan endres fritt
    /// </summary>
    /// <param name="name">Enkelt string med navn, ingen tilknytning til noe annet</param>
    /// <param name="value">En verdi uten tilknytning til noe annet</param>
    public static TestResponse BuildTestResponse(string name = "Test", int value = 1) =>
        new() { Name = name, Value = value };
    
    /// <summary>
    /// Hjelperesponse som brukes kun til testing
    /// </summary>
    public class TestResponse
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}