using CompVault.Backend.Common.Security;
using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Equipment;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Tests.Common.Constants;
using CompVault.Shared.Enums;

namespace CompVault.Backend.Tests.Common;

/// <summary>
/// For opprettelse av database modell-objekter for testing
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// Oppretter en ApplicationUser for testing. Brukes i de fleste testene.
    /// Hvis deletedAt har en verdi, så er brukeren inaktive/slettet
    /// Guid er optional. Bruker ActiveUserId som default hvis ingen annen informasjon er oppgitt
    /// </summary>
    /// <param name="id">ID til en bruker hvis man trenger å slå opp ID for testing</param>
    /// <param name="email">Optional string med Epost for å opprette forskjellige brukere</param>
    ///  <param name="firstName">Optional first name som er satt til TestConstant.Users</param>
    /// <param name="lastName">Optional last name som er satt til TestConstant.Users</param>
    /// <param name="deletedAt">DateTime som bestemmer om brukeren er aktive/slettet</param>
    /// <param name="departmentId">ID til avdeling</param>
    /// <returns>En ferdig opprettet ApplicationUser for testing</returns>
    public static ApplicationUser CreateApplicationUser(Guid? id = null,
        string email = TestConstants.Users.DefaultEmailForActiveUser,
        string firstName = TestConstants.Users.FirstName,
        string lastName = TestConstants.Users.LastName,
        DateTime? deletedAt = null,
        Guid? departmentId = null) => new()
        {
            Id = id ?? Guid.NewGuid(),
            Email = email,
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = deletedAt == null,
            DeletedAt = deletedAt,
            DepartmentId = departmentId ?? TestConstants.Departments.DefaultDepartmentId
        };

    /// <summary>
    /// Oppretter en Otp-kode tilhørende en bruker
    /// </summary>
    /// <param name="userId">Brukeren som Otp-koden tilhører. Default til ActiveUserId</param>
    /// <param name="plainTextCode">Koden i plaintext som blir hashet i metoden. Default konstant</param>
    /// <param name="createdAt">Når OTP-koden er opprettet. Defauklt UtcNop</param>
    /// <param name="expiresAt">DateTime-objekt som spesifiserer når den går ut. Default om 10 min</param>
    /// <param name="failedAttempts">Antall feilede forsøk. Default = 0</param>
    /// <param name="isUsed">Setter om OTP-koden er brukt eller ikke. Default = false</param>
    /// <returns>En opprettet OtpCode</returns>
    public static OtpCode CreateOtpCode(Guid? userId = null, string plainTextCode = TestConstants.Otp.PlainTextOtpCode,
        DateTime? createdAt = null, DateTime? expiresAt = null, int failedAttempts = 0, bool isUsed = false) => new()
        {
            UserId = userId ?? TestConstants.Users.ActiveUserId,
            Code = OtpHasher.HashCode(plainTextCode),
            CreatedAt = createdAt ?? DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(10),
            FailedAttempts = failedAttempts,
            IsUsed = isUsed,
        };

    /// <summary>
    /// Oppretter en RefreshToken tilhørende en bruker
    /// </summary>
    /// <param name="userId">Brukeren som Token tilhører. Default ActiveUserId</param>
    /// <param name="token">Selve token, kun en enkel string i testene. Default token-konstant</param>
    /// <param name="createdAt">Når den er opprettet. Default UtcNow</param>
    /// <param name="expiresAt">Når den utgår. Default om 15 min fra opprettelse</param>
    /// <param name="isRevoked">Bool på om koden er gyldig eller revoked</param>
    /// <returns>En opprettet RefreshToken</returns>
    public static RefreshToken CreateRefreshToken(Guid? userId = null, string? token = null,
        DateTime? createdAt = null,
        DateTime? expiresAt = null, bool isRevoked = false) => new()
        {
            UserId = userId ?? TestConstants.Users.ActiveUserId,
            Token = token ?? Guid.NewGuid().ToString(),
            CreatedAt = createdAt ?? DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(15),
            IsRevoked = isRevoked
        };

    /// <summary>
    /// Oppretter en Department for testing
    /// </summary>
    public static Department CreateDepartment(
        Guid? id = null,
        string name = "Test Department",
        string? description = null,
        Guid? parentDepartmentId = null,
        bool isActive = true,
        DateTime? createdAt = null,
        DateTime? deletedAt = null) => new()
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            Description = description ?? string.Empty,
            ParentDepartmentId = parentDepartmentId,
            IsActive = isActive,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            DeletedAt = deletedAt
        };
    
    
    /// <summary>
    /// Oppretter en kompetansetype for testing
    /// </summary>
    /// <param name="id">ID-en til kompetansetypen</param>
    /// <param name="name">Navnet på kompetansetypen. Default Dykkekurs</param>
    /// <param name="category">Valgfri kategori. Default null</param>
    /// <param name="requiresExpiration">Utgår kompetansetypen. Default false</param>
    /// <returns>Ferdig bygget CompetencyType for testing</returns>
    public static CompetencyType CreateCompetencyType(
        Guid? id = null,
        string name = "Dykkekurs",
        string? category = null,
        bool requiresExpiration = false) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Name = name,
        Category = category,
        RequiresExpiration = requiresExpiration,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };
    
    /// <summary>
    /// Oppretter en kompetanse for å teste mot. Egenskaper for revoke og soft delete er utelatt
    /// </summary>
    /// <param name="id">ID-en til kompetansen. Defualt new Guid</param>
    /// <param name="userId">Brukeren som blir tildelt kompetansen. Defualt new Guid</param>
    /// <param name="competencyTypeId">Kompetansetypen. Defualt new Guid</param>
    /// <param name="status">CompetencyStatus. Defualt Valid</param>
    /// <param name="expiryDate">Når den går ut hvis den går ut (Se typen). Default null</param>
    /// <param name="issuedDate">Når den er utlevert. Defualt UtcNow</param>
    /// <returns>Ferdig bygget Competency for testing</returns>
    public static Competency CreateCompetency(
        Guid? id = null,
        Guid? userId = null,
        Guid? competencyTypeId = null,
        CompetencyStatus status = CompetencyStatus.Valid,
        DateTime? expiryDate = null,
        DateTime? issuedDate = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        UserId = userId ?? Guid.NewGuid(),
        CompetencyTypeId = competencyTypeId ?? Guid.NewGuid(),
        Status = status,
        ExpiryDate = expiryDate,
        IssuedDate = issuedDate ?? DateTime.UtcNow,
        IsActive = true
    };
    
    /// <summary>
    /// Oppretter en utstyrskategori for testing
    /// </summary>
    /// <param name="id">ID-en tit kategorien. Default oppretter egen Guid</param>
    /// <param name="name">Navn. Default er Test kategori</param>
    /// <returns>Ferdig bygget EquipmentCategory klar til testing</returns>
    public static EquipmentCategory CreateEquipmentCategory(Guid? id = null, string name = "Test kategori") => new()
    {
        Id = id ?? Guid.NewGuid(), 
        Name = name, 
        IsActive = true, 
        CreatedAt = DateTime.UtcNow
    };
    
    /// <summary>
    /// Oppretter et utstyr
    /// </summary>
    /// <param name="id">ID-til utsyret. Default new Guid</param>
    /// <param name="categoryId">ID-til EquipmentCateogry. Default new Guid</param>
    /// <param name="name">Navn. Default er Test utstyr</param>
    /// <param name="hasSize">Har item størrelse. Default false</param>
    /// <returns>EquipmentItem for testing</returns>
    public static EquipmentItem CreateEquipmentItem(
        Guid? id = null, 
        Guid? categoryId = null, 
        string name = "Test utstyr",
        bool hasSize = false) => new()
    {
        Id = id ?? Guid.NewGuid(), 
        CategoryId = categoryId ?? Guid.NewGuid(),
        Name = name, 
        HasSize = hasSize,
        IsActive = true, 
        CreatedAt = DateTime.UtcNow
    };
    
    /// <summary>
    /// Tilknytter et utstyr en bruker
    /// </summary>
    /// <param name="id">ID til tilknyttingen. Default new Guid</param>
    /// <param name="userId">Brukerens ID. Default new Guid</param>
    /// <param name="itemId">Utstyrets ID. Default new Guid</param>
    /// <param name="issuedById">Brukeren som har utlevert. Default new Guid</param>
    /// <param name="quantity">Antall utlevert. Default 1</param>
    /// <param name="size">Størrelse hvis satt. Default null</param>
    /// <returns>EquipmentIssuance for testing</returns>
    public static EquipmentIssuance CreateEquipmentIssuance(
        Guid? id = null, 
        Guid? userId = null, 
        Guid? itemId = null, 
        Guid? issuedById = null,
        int quantity = 1,
        string? size = null) => new()
    {
        Id = id ?? Guid.NewGuid(), 
        UserId = userId ?? Guid.NewGuid(),
        ItemId = itemId ?? Guid.NewGuid(),
        IssuedById = issuedById ?? Guid.NewGuid(),
        Quantity = quantity,
        IssuedDate = DateTime.UtcNow,
        Size = size,
        IsActive = true, 
        CreatedAt = DateTime.UtcNow
    };

}