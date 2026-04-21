using CompVault.Frontend.Common.Models;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Tests.Common;

public static class TestDataFactory
{
    /// <summary>
    /// Bygger et ProbelemDetails-objekt som brukes av flere av tester
    /// </summary>
    /// <param name="statusCode">HTTP-status koden</param>
    /// <param name="errorCode">AppError-kode som string</param>
    /// <param name="message">Feilmeldingen</param>
    public static ProblemDetail BuildProblemDetail(int statusCode, string errorCode, string message) =>
        new()
        {
            Status = statusCode,
            Code = errorCode,
            Message = message
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
    
    /// <summary>
    /// Bygger en CreateDocumentRequest med defaulte verdier
    /// </summary>
    /// <param name="title">Tittel på dokumentet, default til "HMS-sjekkliste"</param>
    /// <param name="description">Beskrivelse, default null</param>
    /// <param name="typeCategory">DocumentTypeCategoryId, default null</param>
    /// <param name="externalUrl">URl til dokumentet, default null</param>
    /// <param name="requiresSignature">Påkrevd signature, defualt true</param>
    /// <param name="targetDepartmentIds">Avdelinger som målgruppe, defaut tom liste</param>
    /// <param name="targetJobTitleIds">Stillingstitteler, default tom liste</param>
    /// <returns>Ferdig CreateDocumentRequest</returns>
    public static CreateDocumentRequest BuildCreateDocumentRequest(
        string title = "HMS-sjekkliste", 
        string? description = null,
        Guid? typeCategory = null,
        string? externalUrl = null, 
        bool requiresSignature = true,
        List<Guid>? targetDepartmentIds = null,
        List<Guid>? targetJobTitleIds = null) => new CreateDocumentRequest()
    {
          Title = title,
          Description = description,
          DocumentTypeCategoryId = typeCategory,
          ExternalUrl = externalUrl,
          RequiresSignature = requiresSignature,
          TargetDepartmentIds = targetDepartmentIds ?? [],
          TargetJobTitleIds = targetJobTitleIds ?? []
    };
    
    /// <summary>
    /// Bygger en FileAttachment med stream og defaulte verdier
    /// </summary>
    /// <param name="fileName">Navnet på filen, default sjekkliste.pdf</param>
    /// <param name="contentType">Type fil, default application/pdf</param>
    /// <returns>En FileAttachment med stream</returns>
    public static FileAttachment BuildFileAttachment(
        string fileName = "sjekkliste.pdf",
        string contentType = "application/pdf")
    {
        var stream = new MemoryStream("filinnhold"u8.ToArray());
        return new FileAttachment(stream, fileName, contentType);
    }
}