using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Auth;
using CompVault.Backend.Features.Auth.Configuration;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace CompVault.Tests.Backend.Features.Auth;

public class AuthServiceRequestOtpAsyncTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ILogger<IAuthService>> _loggerMock;
    private readonly Mock<IJwtService> _jwtServiceMock;
    private readonly Mock<IOtpCodeService> _otpCodeServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;

    public AuthServiceRequestOtpAsyncTests()
    {
        // UserManager krever IUserStore i konstruktøren
        var storeMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            storeMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

}