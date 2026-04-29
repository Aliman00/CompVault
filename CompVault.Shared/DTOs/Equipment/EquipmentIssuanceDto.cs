namespace CompVault.Shared.DTOs.Equipment;

/// <summary>
/// Det klienten ser når de spør etter en utlevering.
/// Inkluderer navigasjonsdata (brukernavn, utstyrsnavn, kategorinavn).
/// </summary>
public sealed class EquipmentIssuanceDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    // ---- Bruker (hvem fikk utstyret) ----

    /// <summary>ID til brukeren som har fått utstyret.</summary>
    public Guid UserId { get; set; }

    /// <summary>Brukernavn.</summary>
    public string? UserName { get; set; }

    /// <summary>Brukerens fornavn.</summary>
    public string? UserFirstName { get; set; }

    /// <summary>Brukerens etternavn.</summary>
    public string? UserLastName { get; set; }

    /// <summary>Fullt navn — satt sammen automatisk.</summary>
    public string UserFullName => $"{UserFirstName} {UserLastName}".Trim();

    // ---- Utstyr (hva er utlevert) ----

    /// <summary>ID til utstyret.</summary>
    public Guid ItemId { get; set; }

    /// <summary>Navn på utstyret.</summary>
    public string? ItemName { get; set; }

    /// <summary>ID til kategorien utstyret tilhører.</summary>
    public Guid? CategoryId { get; set; }

    /// <summary>Navn på kategorien.</summary>
    public string? CategoryName { get; set; }

    // ---- Utleveringsdetaljer ----

    /// <summary>Antall utlevert.</summary>
    public int Quantity { get; set; }
    
    /// <summary>Om utstyret har størrelse.</summary>
    public bool HasSize { get; set; }

    /// <summary>Størrelse (f.eks. "XL", "43").</summary>
    public string? Size { get; set; }

    /// <summary>ID til brukeren som delte ut utstyret.</summary>
    public Guid IssuedById { get; set; }

    /// <summary>Navn på brukeren som delte ut.</summary>
    public string? IssuedByName { get; set; }

    /// <summary>Når utstyret ble utlevert.</summary>
    public DateTime IssuedDate { get; set; }

    /// <summary>Valgfrie notater.</summary>
    public string? Notes { get; set; }

    /// <summary>Når utleveringen ble opprettet i systemet (UTC).</summary>
    public DateTime CreatedAt { get; set; }
}