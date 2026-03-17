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
    /// <returns>RequestOtpRequest for testing</returns>
    public static RequestOtpRequest CreateRequestOtpRequest(
        string email = TestConstants.Users.DefaultEmailForActiveUser,
        OtpDeliveryMethod method = OtpDeliveryMethod.Email) => new()
        {
            Email = email,
            DeliveryMethod = method
        };

    /// <summary>
    /// Oppretter en VerifyOtpRequest for bruk i testing
    /// </summary>
    /// <param name="email">Brukerens epost. Default til aktiv brukers epost</param>
    /// <param name="otpCode">6-sifret OTP-kode. Default til PlainTextOtpCode</param>
    /// <returns>VerifyOtpRequest</returns>
    public static VerifyOtpRequest CreateVerifyOtpRequest(string email = TestConstants.Users.DefaultEmailForActiveUser,
        string otpCode = TestConstants.Otp.PlainTextOtpCode) => new()
        {
            Email = email,
            OtpCode = otpCode
        };
}