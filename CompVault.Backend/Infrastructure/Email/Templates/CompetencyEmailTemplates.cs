using CompVault.Backend.Infrastructure.Email.Models;

namespace CompVault.Backend.Infrastructure.Email.Templates;

/// <summary>
/// E-postmaler for varsling om kompetanseutløp.
/// Inneholder varianter for ansatt og leder, samt egen mal for utløpte bevis.
/// </summary>
public static class CompetencyEmailTemplates
{
    private const string Footer = "<hr><p style=\"color:#888;font-size:12px\"><em>Denne e-posten er sendt automatisk fra CompVault. Ikke svar på denne e-posten.</em></p>";

    /// <summary>
    /// Varsel til ansatt om at et kompetansebevis er i ferd med å utløpe.
    /// </summary>
    public static EmailBody ExpiringSoonToEmployee(
        string employeeName,
        string competencyName,
        DateTime expiryDate,
        int daysUntil) => new(
        Subject: $"Kompetansebeviset «{competencyName}» utløper om {daysUntil} dager",
        Html: $"""
            <h2>Hei {employeeName},</h2>
            <p>Ditt kompetansebevis <strong>{competencyName}</strong> utløper <strong>{expiryDate:dd. MMMM yyyy}</strong> — om <strong>{daysUntil} dager</strong>.</p>
            <p>Vennligst ta kontakt med din leder for å fornye kompetansebeviset i god tid før utløp.</p>
            <p><a href="https://compvault.no">Gå til CompVault</a></p>
            {Footer}
            """
    );

    /// <summary>
    /// Varsel til leder om at en ansatts kompetansebevis er i ferd med å utløpe.
    /// </summary>
    public static EmailBody ExpiringSoonToManager(
        string managerName,
        string employeeName,
        string competencyName,
        DateTime expiryDate,
        int daysUntil) => new(
        Subject: $"{employeeName} sitt kompetansebevis «{competencyName}» utløper om {daysUntil} dager",
        Html: $"""
            <h2>Hei {managerName},</h2>
            <p><strong>{employeeName}</strong> sitt kompetansebevis <strong>{competencyName}</strong> utløper <strong>{expiryDate:dd. MMMM yyyy}</strong> — om <strong>{daysUntil} dager</strong>.</p>
            <p>Vennligst sørg for at kompetansebeviset fornyes i god tid før utløp.</p>
            <p><a href="https://compvault.no">Gå til CompVault</a></p>
            {Footer}
            """
    );

    /// <summary>
    /// Varsel til ansatt om at et kompetansebevis har utløpt.
    /// </summary>
    public static EmailBody ExpiredToEmployee(
        string employeeName,
        string competencyName,
        DateTime expiryDate) => new(
        Subject: $"Kompetansebeviset «{competencyName}» har utløpt",
        Html: $"""
            <h2>Hei {employeeName},</h2>
            <p>Ditt kompetansebevis <strong>{competencyName}</strong> utløpte <strong>{expiryDate:dd. MMMM yyyy}</strong>.</p>
            <p>Beviset er ikke lenger gyldig. Ta kontakt med din leder for å planlegge fornyelse.</p>
            <p><a href="https://compvault.no">Gå til CompVault</a></p>
            {Footer}
            """
    );

    /// <summary>
    /// Varsel til leder om at en ansatts kompetansebevis har utløpt.
    /// </summary>
    public static EmailBody ExpiredToManager(
        string managerName,
        string employeeName,
        string competencyName,
        DateTime expiryDate) => new(
        Subject: $"{employeeName} sitt kompetansebevis «{competencyName}» har utløpt",
        Html: $"""
            <h2>Hei {managerName},</h2>
            <p><strong>{employeeName}</strong> sitt kompetansebevis <strong>{competencyName}</strong> utløpte <strong>{expiryDate:dd. MMMM yyyy}</strong>.</p>
            <p>Beviset er ikke lenger gyldig. Vennligst planlegg fornyelse snarest.</p>
            <p><a href="https://compvault.no">Gå til CompVault</a></p>
            {Footer}
            """
    );
}