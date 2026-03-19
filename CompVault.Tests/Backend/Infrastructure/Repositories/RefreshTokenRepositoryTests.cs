using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Tests.Common;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Tests.Backend.Infrastructure.Repositories;

public class RefreshTokenRepositoryTests : InMemoryRepositoryBase
{
    private readonly RefreshTokenRepository _sut;

    public RefreshTokenRepositoryTests()
    {
        _sut = new RefreshTokenRepository(Context);
    }
    
    
}