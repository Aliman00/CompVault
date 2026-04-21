using CompVault.Frontend.Common.Http;
using CompVault.Frontend.Common.Models;
using CompVault.Frontend.Tests.Common;
using CompVault.Shared.DTOs.Documents;
using FluentAssertions;
namespace CompVault.Frontend.Tests.Frontend.Common.Http;

public class MultipartFormBuilderTests
{
    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------
    /// <summary>
    /// Leser egenskapene fra MultipartFormDataContent inn i en ordbok. Navnet på egenskap er Key, og verdien
    /// til egenskapen er Value. Akkurat slik før vi bygget MutlipartFormDataContent
    /// </summary>
    /// <param name="content">MultipartFormDataContent</param>
    /// <returns>En ordbok med egenskapene</returns>
    private static async Task<Dictionary<string, string>> ReadFormFieldsAsync(MultipartFormDataContent content)
    {
        var fields = new Dictionary<string, string>();

        foreach (HttpContent part in content)
        {
            // Henter ut egenskap navnet som da blir Key
            string propertyName = part.Headers.ContentDisposition!.Name!.Trim('"');
            
            // Egenskaper relevant til filen hopper vi over
            if (part.Headers.ContentDisposition?.FileName != null)
                continue;
            
            // Leser verdien og gir oss en string som vi bruker som Value
            string value = await part.ReadAsStringAsync();
            fields[propertyName] = value;
        }

        return fields;
    }
    
    // -------------------------------------------------------------------------
    // String-felter
    // -------------------------------------------------------------------------
    /// <summary>
    /// Tester at egenskaper med string som verdi blir bygget korrekt
    /// </summary>
    [Fact]
    public async Task Build_RequestWithTitle_AddsCorrectField()
    {
        // Arrange - Bygger request og fil med defaulte verdier
        string titleKey = "Title";
        string titlevalue = "HMS-sjekkliste";
        CreateDocumentRequest request = TestDataFactory.BuildCreateDocumentRequest(title: titlevalue);
        FileAttachment file = TestDataFactory.BuildFileAttachment();

        // Act - Henter ut verdiene i en ordbok
        using MultipartFormDataContent content = MultipartFormBuilder.Build(request, file);
        Dictionary<string, string> fieldsFromContent = await ReadFormFieldsAsync(content);
        
        // Assert 
        fieldsFromContent.Should().ContainKey(titleKey);
        fieldsFromContent[titleKey].Should().Be(titlevalue);
    }
    
    // -------------------------------------------------------------------------
    // Bool
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester at både true og false blir gjort om til liten bokstav som MultipartFormDataContent krever
    /// </summary>
    /// <param name="value">Boolverdi true eller false</param>
    /// <param name="expectedValue">Forventet verdi i string med liten bokstav</param>
    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public async Task Build_RequestWithBool_AddsCorrectField(bool value, string expectedValue)
    {
        // Arrange - Bygger request og fil med defaulte verdier
        string requresSignatureKey = "RequiresSignature";
        CreateDocumentRequest request = TestDataFactory.BuildCreateDocumentRequest(requiresSignature: value);
        FileAttachment file = TestDataFactory.BuildFileAttachment();

        // Act - Henter ut verdiene i en ordbok
        using MultipartFormDataContent content = MultipartFormBuilder.Build(request, file);
        Dictionary<string, string> fieldsFromContent = await ReadFormFieldsAsync(content);
        
        // Assert 
        fieldsFromContent.Should().ContainKey(requresSignatureKey);
        fieldsFromContent[requresSignatureKey].Should().Be(expectedValue);
    }
    
    // -------------------------------------------------------------------------
    // Skipper null-felter
    // -------------------------------------------------------------------------
    /// <summary>
    /// Tester at nullable egenskaper blir hoppet over. Description er default null
    /// </summary>
    [Fact]
    public async Task Build_RequestWithNullProperty_SkipsField()
    {
        // Arrange - Bygger en tittle i requeste, mens descrption er null
        string descriptionKey = "Description";
        string titlevalue = "HMS-sjekkliste";
        CreateDocumentRequest request = TestDataFactory.BuildCreateDocumentRequest(title: titlevalue);
        FileAttachment file = TestDataFactory.BuildFileAttachment();

        // Act 
        using MultipartFormDataContent content = MultipartFormBuilder.Build(request, file);
        Dictionary<string, string> fieldsFromContent = await ReadFormFieldsAsync(content);
        
        // Assert 
        fieldsFromContent.Should().NotContainKey(descriptionKey);
    }
    
    // -------------------------------------------------------------------------
    // Lise-felt
    // -------------------------------------------------------------------------
    /// <summary>
    /// Tester at en liste med flere egenskaper blir satt korrekt med indeks
    /// </summary>
    [Fact]
    public async Task Build_RequestWithList_AddCorrectField()
    {
        // Arrange - Bygger en liste med Guids
        var deptId1 = Guid.NewGuid();
        var deptId2 = Guid.NewGuid();
        CreateDocumentRequest request = 
            TestDataFactory.BuildCreateDocumentRequest(targetDepartmentIds: [deptId1, deptId2]);
        FileAttachment file = TestDataFactory.BuildFileAttachment();

        // Act 
        using MultipartFormDataContent content = MultipartFormBuilder.Build(request, file);
        Dictionary<string, string> fieldsFromContent = await ReadFormFieldsAsync(content);
        
        // Assert 
        fieldsFromContent.Should().ContainKey("TargetDepartmentIds[0]");
        fieldsFromContent.Should().ContainKey("TargetDepartmentIds[1]");
        fieldsFromContent["TargetDepartmentIds[0]"].Should().Be(deptId1.ToString());
        fieldsFromContent["TargetDepartmentIds[1]"].Should().Be(deptId2.ToString());

    }
    
    /// <summary>
    /// Tester at en tom liste ikke gir oss tomme felt i content
    /// </summary>
    [Fact]
    public async Task Build_RequestWithEmptyList_SkipsField()
    {
        // Arrange
        CreateDocumentRequest request = 
            TestDataFactory.BuildCreateDocumentRequest(targetDepartmentIds: []);
        FileAttachment file = TestDataFactory.BuildFileAttachment();

        // Act 
        using MultipartFormDataContent content = MultipartFormBuilder.Build(request, file);
        Dictionary<string, string> fieldsFromContent = await ReadFormFieldsAsync(content);
        
        // Assert 
        fieldsFromContent.Should().NotContainKey("TargetDepartmentIds[0]");
    }
    
    // -------------------------------------------------------------------------
    // Fil-opplastning
    // -------------------------------------------------------------------------
    /// <summary>
    /// Tester at med vedlagt fil så blir metadataene til filen algt korrekt i headeren
    /// </summary>
    [Fact]
    public void Build_RequestWithFile_AddsCorrectMetadata()
    {
        // Arrange - Bygger request og fil med defaulte verdier
        CreateDocumentRequest request = TestDataFactory.BuildCreateDocumentRequest();
        FileAttachment file = TestDataFactory.BuildFileAttachment();

        // Act - Henter ut verdiene i en ordbok
        using MultipartFormDataContent content = MultipartFormBuilder.Build(request, file);
        
        // Assert 
        HttpContent? filePart =
            content.FirstOrDefault(p =>
                p.Headers.ContentDisposition?.FileName?.Trim('"') == "sjekkliste.pdf");
        filePart.Should().NotBeNull();
        filePart.Headers.ContentDisposition?.Name?.Trim('"').Should().Be("file");
        filePart.Headers.ContentType!.MediaType.Should().Be("application/pdf");
    }
    
    /// <summary>
    /// Tester at det ikke er noen metadata i headeren hvis vi ikke har noen vedlagt fil
    /// </summary>
    [Fact]
    public void Build_RequestWithoutFile_DoesNotAddFileMetadata()
    {
        // Arrange - Bygger request og fil med defaulte verdier
        CreateDocumentRequest request = TestDataFactory.BuildCreateDocumentRequest();

        // Act - Henter ut verdiene i en ordbok
        using MultipartFormDataContent content = MultipartFormBuilder.Build(request);
        
        // Assert 
        HttpContent? filePart =
            content.FirstOrDefault(p => p.Headers.ContentDisposition?.FileName != null);
        filePart.Should().BeNull();
    }
    
    
}