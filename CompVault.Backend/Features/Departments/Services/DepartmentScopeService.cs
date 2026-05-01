using System.Security.Claims;

using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Infrastructure.Extensions;
using CompVault.Backend.Infrastructure.Repositories.Departments;

namespace CompVault.Backend.Features.Departments.Services;

/// <inheritdoc />
public sealed class DepartmentScopeService : IDepartmentScopeService
{
    private readonly IHttpContextAccessor _http;
    private readonly IServiceProvider _serviceProvider;

    private readonly Lazy<Task<IReadOnlyList<Guid>>> _idsWithSubDepartments;
    private readonly Lazy<Guid?> _userDepartmentId;

    public DepartmentScopeService(
        IHttpContextAccessor http,
        IServiceProvider serviceProvider)
    {
        _http = http;
        _serviceProvider = serviceProvider;

        // Lazy initialiseres én gang i konstruktøren. Siden servicen er scoped
        // lever denne Lazy-instansen kun for én request.

        // Initialiserer en Lazy-task for å sikre at vi henter tilattele avdelinger kun en gang pr request
        _idsWithSubDepartments = new Lazy<Task<IReadOnlyList<Guid>>>(
            () => ResolveAllowedIdsAsync(CancellationToken.None));

        // Henter avdelingsID kun engang fra innlogget bruker
        _userDepartmentId = new Lazy<Guid?>(() => _http.HttpContext?.User.GetDepartmentId());
    }

    public bool HasBypass(string readAllPermission)
    {
        // Ikke autentisert bruker, returner false for å filtrere bort alt
        ClaimsPrincipal? user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        // Returner true hvis brukeren har tilattelse, false hvis ikke
        return user.HasPermission(readAllPermission);
    }

    public IReadOnlyList<Guid> GetAllowedDepartmentIds(string? readSubPermission = null)
    {
        // Sjekker om brukeren har tilattelse til å hente underavdelinger
        ClaimsPrincipal? user = _http.HttpContext?.User;
        if (readSubPermission != null && user?.HasPermission(readSubPermission) == true)
            return _idsWithSubDepartments.Value.GetAwaiter().GetResult();

        // Returnerer egen avdeling eller tom liste hvis brukere ikke er tildelt avdeling
        Guid? userDepartment = _userDepartmentId.Value;
        return userDepartment.HasValue
            ? [userDepartment.Value]
            : [];
    }

    public bool IsAllowed(Guid departmentId, string readAllPermission, string? readSubPermission = null)
        => HasBypass(readAllPermission) || GetAllowedDepartmentIds(readSubPermission).Contains(departmentId);

    /// <summary>
    /// Finner fram alle avdelingene en bruker har tilattelse til å se entiteter ifra. Følger
    /// avdelings hierarkiet med kun å se entiteter til sin avdeling eller lavere
    /// </summary>
    private async Task<IReadOnlyList<Guid>> ResolveAllowedIdsAsync(CancellationToken ct)
    {
        // Uatentiserte forespørsler ved endepunkter med AllowAnonymous-attributen. Filtrerer bort alt
        ClaimsPrincipal? user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return [];

        // Autentiserte brukere uten avdeling. Filtrerer bort alt
        Guid? departmentId = _userDepartmentId.Value;
        if (departmentId == null)
            return [];

        IDepartmentRepository departmentRepository =
            _serviceProvider.GetRequiredService<IDepartmentRepository>();

        return await BreadthFirstSearchAsync(departmentId.Value, departmentRepository, ct);
    }

    /// <summary>
    /// Vi bruker Breadth-first-search for å søke i bredden på alle avdelinger.
    /// Sjekker alle avdelinger i samme "høyde" før vi går nedover i hierarkiet
    /// </summary>
    private async Task<IReadOnlyList<Guid>> BreadthFirstSearchAsync(Guid departmentId, IDepartmentRepository
            departmentRepository, CancellationToken ct)
    {
        IReadOnlyList<Department> allDepartments = await departmentRepository.GetAllWithHierarchyAsync(ct);

        var result = new List<Guid>();
        var queue = new Queue<Guid>();

        // Legger til brukerens avdeling først
        queue.Enqueue(departmentId);

        while (queue.Count > 0)
        {
            // Henter ut nåværende node og legger den til resultatet. Vi har da besøkt noden og er ferdig med den
            Guid current = queue.Dequeue();
            result.Add(current);

            // Sjekker undernodene til nåværende node og queuer de for senere iterasjoner
            foreach (Department departmentChild in allDepartments.Where(d => d.ParentDepartmentId == current))
            {
                queue.Enqueue(departmentChild.Id);
            }
        }

        return result;
    }

}