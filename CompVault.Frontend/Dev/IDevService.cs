using CompVault.Shared.DTOs.Auth;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Dev;

public interface IDevService
{
    /// <summary>
    /// Oppretter en OTP-kode med verdi 123456 slik at frontend kan kalle VerifyOtp uten at vi sender epost
    /// </summary>
    Task<Result> RequestOtpAsync(RequestOtpRequest request);
}