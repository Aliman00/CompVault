using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Shared.Enums;

namespace CompVault.Backend.Tests.Backend.Domain;

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