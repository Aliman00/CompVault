using System.Net.Http.Headers;
using System.Reflection;

using CompVault.Frontend.Common.Models;
namespace CompVault.Frontend.Common.Http;

internal static class MultipartFormBuilder
{
    /// <summary>
    /// MultipartFormDataContent krever at vi legger til feltene og filen fra en request som
    /// string med innhold og feltnavn. Vi iterer igjennom hver egenskap, sjekker verdien og legger til verdien
    /// i riktig format. Verdier i lister skal legges til indeksert
    /// </summary>
    /// <param name="request">Generisk request. Eks: CreateDocumentRequest</param>
    /// <param name="file">Valgfri fil sendes som FileAttachment</param>
    /// <typeparam name="TRequest">Navnet på Request-klassen</typeparam>
    /// <returns></returns>
    public static MultipartFormDataContent Build<TRequest>(TRequest request, FileAttachment? file = null)
        where TRequest : class
    {
        var content = new MultipartFormDataContent();

        // Serialiserer alle egenskapene fra DTO-en til en string for å sende med requesten i MultipartFormDataContent
        foreach (PropertyInfo property in typeof(TRequest).GetProperties())
        {
            // Henter ut verdien til hver egenskap - hvis null så sender vi den ikke med, og hopper over egenskapen
            object? value = property.GetValue(request);
            if (value is null)
                continue;

            // Lister som er List<Guid> - lister må sendes som seprate form-felter med indeks i navnet
            if (value is IReadOnlyList<Guid> guids)
            {
                AddIndexedList(content, property.Name, guids, g => g.ToString());
                continue;
            }

            // String er allerede string, så her sender vi inn g uten ToString()
            if (value is IReadOnlyList<string> strings)
            {
                AddIndexedList(content, property.Name, strings, g => g);
                continue;
            }

            // Bool gjort om til string med ToString() blir store bokstaver, må sikre at det er små bokstaver
            string stringValue = value is bool boolValue
                ? boolValue.ToString().ToLowerInvariant()
                : value.ToString()!; // Verdien er sjekket for null over

            content.Add(new StringContent(stringValue), property.Name);
        }

        // Legger til fil hvis fil er vedlagt
        AddFile(content, file);

        return content;
    }

    /// <summary>
    /// Iterer igjennom en liste med forskjellige verdier, gjør det om til en string med indeks i navnet
    /// slik MutlipartFormDataContent krever. Eks: TargetDepartmentIds[0] = "a1b2c3..."
    /// </summary>
    /// <param name="content">MultipartFormDataContent vi legger til</param>
    /// <param name="propertyName">Navnet på egenskapen, eks: TargetDepartmentIds</param>
    /// <param name="list">Liste med generiske verdier</param>
    /// <param name="toString">Vi må kalle to String på verdiene vi sender inn inne i metoden</param>
    /// <typeparam name="T">Generisk Type som Guid, string etc.</typeparam>
    private static void AddIndexedList<T>(MultipartFormDataContent content, string propertyName, IReadOnlyList<T> list,
        Func<T, string> toString)
    {
        for (int i = 0; i < list.Count; i++) // Iterer over hvert Guid i listen og legger til verdien med index
        {
            content.Add(new StringContent(toString(list[i])), $"{propertyName}[{i}]");
        }
    }

    /// <summary>
    /// Legger til en fil inn i MutlipartFormDataContent hvis fil er vedlagt.
    /// </summary>
    /// <param name="content"></param>
    /// <param name="file"></param>
    internal static void AddFile(MultipartFormDataContent content, FileAttachment? file)
    {
        if (file == null)
            return;

        var fileContent = new StreamContent(file.Stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        // MutlipartFormDataContent krever innhold, feltnavn og filnavn
        content.Add(fileContent, "file", file.FileName);
    }
}