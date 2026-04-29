using System.Security.Claims;

using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Repositories.Departments;
using CompVault.Backend.Tests.Common;
using CompVault.Shared.Constants;

using FluentAssertions;

using Microsoft.AspNetCore.Http;

using Moq;

namespace CompVault.Backend.Tests.Backend.Features.Departments.Services;

public class DepartmentScopeServiceTests
{
    // Vi bygger en struktur her for testing ved at Root er toppavdelingen, ChildA og ChildB er under Root igjen, og
    // Grandchild er under ChildA igjen
    
    //   Root
    //   ├── ChildA
    //   │   └── GrandChild
    //   └── ChildB
    
    private static readonly Guid RootId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static readonly Guid ChildAId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    private static readonly Guid ChildBId = Guid.Parse("10000000-0000-0000-0000-000000000003");
    private static readonly Guid GrandChildId = Guid.Parse("10000000-0000-0000-0000-000000000004");
    
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IDepartmentRepository> _departmentRepositoryMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    
    
    public DepartmentScopeServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _departmentRepositoryMock = new Mock<IDepartmentRepository>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        
        // Setter opp til å kunne mocke at vi kaller ServiceProvider i testene
        _serviceProviderMock
            .Setup(sp => sp.GetService(typeof(IDepartmentRepository)))
            .Returns(_departmentRepositoryMock.Object);
        
        // Mocker at vi får en tom liste
        _departmentRepositoryMock
            .Setup(r => r.GetAllWithHierarchyAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Department>());
    }
    
    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Bygger en DepartmentScopeService med en innlogget bruker med avdeling og tilattelser
    /// </summary>
    /// <param name="departmentId">ID til en avdeling</param>
    /// <param name="permissions">Tilattelser</param>
    /// <returns>System under Test av DepartmentScopeService for testing</returns>
    private DepartmentScopeService CreateSut(Guid? departmentId, params string[] permissions)
    {
        _httpContextAccessorMock.Setup(x => x.HttpContext)
            .Returns(BuildHttpContext(departmentId, permissions));

        return new DepartmentScopeService(_httpContextAccessorMock.Object, _serviceProviderMock.Object);
    }
    
    /// <summary>
    /// Bygger en uautentisert sut for å teste uten en innlogget bruker med claim
    /// </summary>
    private DepartmentScopeService CreateUnauthenticatedSut()
    {
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        return new DepartmentScopeService(_httpContextAccessorMock.Object, _serviceProviderMock.Object);
    }
    
    /// <summary>
    /// Bygger en HttpContext med en innlogget bruker med valgrie permissions
    /// Setter kun department-claim hvis det er med en avdeling, for å kunne teste uten en claim
    /// </summary>
    /// <param name="departmentId">ID til en avdeling</param>
    /// <param name="permissions">Tilattelser</param>
    /// <returns>Ferdig bygget HttpContext</returns>
    private static HttpContext BuildHttpContext(Guid? departmentId, string[] permissions)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()) };

        if (departmentId.HasValue)
            claims.Add(new Claim("department_id", departmentId.Value.ToString()));
    
        foreach (string permission in permissions)
        {
            claims.Add(new Claim(Permissions.ClaimType, permission));
        }

        var identity = new ClaimsIdentity(claims, authenticationType: "test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };

        return httpContext;
    }

    /// <summary>
    /// Oppretter et avdelingshierarki
    /// </summary>
    private void SetupHierarchy()
    {
        var departments = new List<Department>
        {
            TestDataFactory.CreateDepartment(id: RootId, name: "Root", parentDepartmentId: null),
            TestDataFactory.CreateDepartment(id: ChildAId, name: "ChildA", parentDepartmentId: RootId),
            TestDataFactory.CreateDepartment(id: ChildBId, name: "ChildB", parentDepartmentId: RootId),
            TestDataFactory.CreateDepartment(id: GrandChildId, name: "Grandchild", parentDepartmentId: ChildAId),
        };
        
        _departmentRepositoryMock
            .Setup(r => r.GetAllWithHierarchyAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(departments);
    }
    
    // -------------------------------------------------------------------------
    // HasBypass
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester at HasBypass gir oss true hvis vi har korrekt bypass tilattelse
    /// </summary>
    [Fact]
    public void HasBypass_WithBypassPermission_ReturnsTrue()
    {
        // Arrange
        DepartmentScopeService sut = CreateSut(ChildAId, Permissions.UsersAll);

        // Act
        bool result = sut.HasBypass(Permissions.UsersAll);
        
        // Assert
        result.Should().BeTrue();
    }
    
    /// <summary>
    /// Tester at vi får false hvis vi ikke har en korrekt bypass permission. Vi har UsersRead, men metoden
    /// vi kaller krever at vi har UsersAll
    /// </summary>
    [Fact]
    public void HasBypass_WithoutBypassPermission_ReturnsFalse()
    {
        // Arrange
        DepartmentScopeService sut = CreateSut(ChildAId, Permissions.UsersRead);

        // Act
        bool result = sut.HasBypass(Permissions.UsersAll);
        
        // Assert
        result.Should().BeFalse();
    }
    
    /// <summary>
    /// Tester at vi får false hvis vi ikke er innlogget
    /// </summary>
    [Fact]
    public void HasBypass_NotAuthenticated_ReturnsFalse()
    {
        // Arrange
        DepartmentScopeService sut = CreateUnauthenticatedSut();

        // Act
        bool result = sut.HasBypass(Permissions.UsersAll);
        
        // Assert
        result.Should().BeFalse();
    }
    
    // -------------------------------------------------------------------------
    // GetAllowedDepartmentIds
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Tester at riktig tilattelse gir oss egen avdeling og underavdeling
    /// </summary>
    [Fact]
    public void GetAllowedDepartmentIds_HaveReadSubPermission_ReturnsOwnAndSubDepartment()
    {
        // Arrange
        SetupHierarchy();
        DepartmentScopeService sut = CreateSut(ChildAId, Permissions.UsersReadSub);

        // Act
        IReadOnlyList<Guid> result = sut.GetAllowedDepartmentIds(Permissions.UsersReadSub);
        
        // Assert - Sjekker at det er to elementer i listen og at det er korrekt avdelinger
        result.Should().BeEquivalentTo([ChildAId, GrandChildId]);
    }
    
    /// <summary>
    /// Tester at hvis vi ikke har korrekt permission så får vi ikke se under avdelinger. Brukeeren har UsersRead,
    /// men metoden krever UsersReadSub. Brukeren får se egen avdeling
    /// </summary>
    [Fact]
    public void GetAllowedDepartmentIds_WithNoSubPermission_ReturnsUsersDepartment()
    {
        // Arrange
        DepartmentScopeService sut = CreateSut(ChildAId, Permissions.UsersRead);

        // Act
        IReadOnlyList<Guid> result = sut.GetAllowedDepartmentIds(Permissions.UsersReadSub);
        
        // Assert - Sjekker at det er kun et element i resultatet listen at det er korrekt avdeling
        result.Should().ContainSingle()
            .Which.Should().Be(ChildAId);
    }
    
    /// <summary>
    /// Tester at uautentisert bruker gir oss en tom liste
    /// </summary>
    [Fact]
    public void GetAllowedDepartmentIds_NotAuthenticated_ReturnsNoDepartments()
    {
        // Arrange
        DepartmentScopeService sut = CreateUnauthenticatedSut();

        // Act
        IReadOnlyList<Guid> result = sut.GetAllowedDepartmentIds(Permissions.UsersReadSub);
        
        // Assert
        result.Should().BeEmpty();
    }
    
    /// <summary>
    /// Sjekker at vi får hele hierarkiet ved å ha korrekt sub-permission
    /// </summary>
    [Fact]
    public void GetAllowedDepartmentIds_WithSubPermission_FromRoot_ReturnsAllDepartments()
    {
        // Arrange
        SetupHierarchy();
        DepartmentScopeService sut = CreateSut(RootId, Permissions.UsersReadSub);

        // Act
        IReadOnlyList<Guid> result = sut.GetAllowedDepartmentIds(Permissions.UsersReadSub);
        
        // Assert
        result.Should().BeEquivalentTo([RootId, ChildAId, ChildBId, GrandChildId]);
    }
    
    /// <summary>
    /// Sjekker at algoritmen vår med BFS ikke returnerer en høyere avdeling når vi har sub-permission
    /// </summary>
    [Fact]
    public void GetAllowedDepartmentIds_WithSubPermission_FromBottom_ReturnsBottomDepartment()
    {
        // Arrange
        SetupHierarchy();
        DepartmentScopeService sut = CreateSut(GrandChildId, Permissions.UsersReadSub);

        // Act
        IReadOnlyList<Guid> result = sut.GetAllowedDepartmentIds(Permissions.UsersReadSub);
        
        // Assert
        result.Should().ContainSingle()
            .Which.Should().Be(GrandChildId);
    }
    
    // -------------------------------------------------------------------------
    // IsAllowed
    // -------------------------------------------------------------------------
    
    /// <summary>
    /// Sjekker at vi får true hvis brukeren har readAll-permission og vi sjekker en underavdeling
    /// </summary>
    [Fact]
    public void IsAllowed_HasReadAllPermission_ReturnsTrue()
    {
        // Arrange
        DepartmentScopeService sut = CreateSut(ChildAId, Permissions.UsersAll);

        // Act
        bool result = sut.IsAllowed(GrandChildId, Permissions.UsersAll, Permissions.UsersReadSub);
        
        // Assert
        result.Should().BeTrue();
    }
    
    /// <summary>
    /// Sjekker at vi får true hvis brukeren har sub-permission og vi sjekker en underavdeling
    /// </summary>
    [Fact]
    public void IsAllowed_HasSubPermission_ReturnsTrue()
    {
        // Arrange
        SetupHierarchy();
        DepartmentScopeService sut = CreateSut(ChildAId, Permissions.UsersReadSub);

        // Act
        bool result = sut.IsAllowed(GrandChildId, Permissions.UsersAll, Permissions.UsersReadSub);
        
        // Assert
        result.Should().BeTrue();
    }
    
    /// <summary>
    /// Sjekker at vi får false hvis vi sjekker en avdeling som ikke er egen avdeling eller under,
    /// selvom vi har tilattelse
    /// </summary>
    [Fact]
    public void IsAllowed_HasSubPermission_DepartmentOutsideScope_ReturnsFalse()
    {
        // Arrange
        SetupHierarchy();
        DepartmentScopeService sut = CreateSut(ChildAId, Permissions.UsersReadSub);

        // Act
        bool result = sut.IsAllowed(ChildBId, Permissions.UsersAll, Permissions.UsersReadSub);
    
        // Assert
        result.Should().BeFalse();
    }
    
    /// <summary>
    /// Sjekker at vi får false hvis brukeren ikke har riktig permission og vi sjekker en annen avdeling
    /// </summary>
    [Fact]
    public void IsAllowed_HasNoPermissionsChecksOtherDepartment_ReturnsFalse()
    {
        // Arrange
        DepartmentScopeService sut = CreateSut(ChildAId, Permissions.AuditRead);

        // Act
        bool result = sut.IsAllowed(ChildBId, Permissions.UsersAll, Permissions.UsersReadSub);
        
        // Assert
        result.Should().BeFalse();
    }
    
    /// <summary>
    /// Sjekker at vi får true hvis brukeren ikke har riktig permission og vi sjekker egen avdeling
    /// </summary>
    [Fact]
    public void IsAllowed_HasNoPermissionsChecksOwnDepartment_ReturnsTrue()
    {
        // Arrange
        DepartmentScopeService sut = CreateSut(ChildAId, Permissions.AuditRead);

        // Act
        bool result = sut.IsAllowed(ChildAId, Permissions.UsersAll, Permissions.UsersReadSub);
        
        // Assert
        result.Should().BeTrue();
    }
    
        
    // -------------------------------------------------------------------------
    // Test av lazy-cache
    // -------------------------------------------------------------------------
    /// <summary>
    /// Tester at vi ikke utfører flere kall selvom DepartmentScopeService blir kalt flere ganger iløpet av en
    /// forespørsel.
    /// </summary>
    [Fact]
    public void GetAllowedDepartmentIds_CalledMultipleTimes_OnlyOneRepositoryCall()
    {
        // Arrange
        SetupHierarchy();
        DepartmentScopeService sut = CreateSut(ChildAId, Permissions.UsersReadSub);

        // Act
        sut.GetAllowedDepartmentIds(Permissions.UsersReadSub);
        sut.GetAllowedDepartmentIds(Permissions.UsersReadSub);
        sut.GetAllowedDepartmentIds(Permissions.UsersReadSub);
        
        // Assert
        _departmentRepositoryMock.Verify(
            r => r.GetAllWithHierarchyAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

}