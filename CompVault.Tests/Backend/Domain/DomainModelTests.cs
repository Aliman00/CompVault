using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Shared.Enums;

namespace CompVault.Tests.Backend.Domain;

public class DomainModelTests
{
    // ======================== ApplicationUser ========================

    /// <summary>
    /// Tester at en ny ApplicationUser har IsActive = true og DeletedAt = null som standard
    /// </summary>
    [Fact]
    public void ApplicationUser_NewInstance_HasCorrectDefaults()
    {
        // Act
        var user = new ApplicationUser();

        // Assert
        Assert.True(user.IsActive);
        Assert.Null(user.DeletedAt);
        Assert.Equal(EmploymentType.Permanent, user.EmploymentType);
        Assert.Empty(user.DirectReports);
        Assert.Empty(user.OtpCodes);
    }

    // ======================== OtpCode ========================

    /// <summary>
    /// Tester at IsValid er true når koden ikke er brukt og ikke er utgått
    /// </summary>
    [Fact]
    public void OtpCode_WhenNotUsedAndNotExpired_IsValid()
    {
        // Arrange
        var otp = new OtpCode
        {
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        // Assert
        Assert.True(otp.IsValid);
    }

    /// <summary>
    /// Tester at IsValid er false når koden er brukt
    /// </summary>
    [Fact]
    public void OtpCode_WhenUsed_IsNotValid()
    {
        // Arrange
        var otp = new OtpCode
        {
            IsUsed = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };

        // Assert
        Assert.False(otp.IsValid);
    }

    /// <summary>
    /// Tester at IsValid er false når koden er utgått
    /// </summary>
    [Fact]
    public void OtpCode_WhenExpired_IsNotValid()
    {
        // Arrange
        var otp = new OtpCode
        {
            IsUsed = false,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1) // Allerede utgått
        };

        // Assert
        Assert.False(otp.IsValid);
    }

    // ======================== Department ========================

    /// <summary>
    /// Tester at en ny Department får Guid-ID og tomme collections som standard
    /// </summary>
    [Fact]
    public void Department_NewInstance_HasCorrectDefaults()
    {
        // Act
        var department = new Department();

        // Assert
        Assert.NotEqual(Guid.Empty, department.Id);
        Assert.Null(department.ParentDepartmentId);
        Assert.Empty(department.SubDepartments);
        Assert.Empty(department.Members);
    }
}
