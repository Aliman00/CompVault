using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Enums;
using CompVault.Tests.Common.Constants;

namespace CompVault.Tests.Backend.Features.Auth.Builders;

public static class AuthRequestBuilder
{
    /// <summary>
    /// Oppretter en RequestOtpRequest for bruk i testing
    /// </summary>
    /// <param name="email">Brukerens epost, default til aktiv bruker epost</param>
    /// <param name="method">Ønsket leveringsmetode. Default til Email</param>
    /// <returns></returns>
    public static RequestOtpRequest CreateRequestOtpRequest(
        string email = TestConstants.Users.DefaultEmailForActiveUser,
        OtpDeliveryMethod method = OtpDeliveryMethod.Email) => new()
    {
        Email = email,
        DeliveryMethod = method
    };
}