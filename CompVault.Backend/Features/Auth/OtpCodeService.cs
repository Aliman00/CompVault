using System.Security.Cryptography;
using System.Text;
using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Features.Auth.Configuration;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Auth;
using CompVault.Shared.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CompVault.Backend.Features.Auth;

public class OtpCodeService(
    ILogger<OtpCodeService> logger, 
    IOptions<OtpOptions> otpOptions,
    IOtpCodeRepository otpCodeRepository,
    IUnitOfWork unitOfWork) : IOtpCodeService
{
    private readonly OtpOptions _otp = otpOptions.Value;
    
    /// <inheritdoc />
    public async Task<Result<string>> GenerateOtpCodeAsync(Guid userId, CancellationToken ct = default)
    {
        // Henter eksisterende Otp hvis den eksisterer
        var existingOtp = await otpCodeRepository.GetActiveCodeAsync(userId, ct);
        
        // Hvis den er fortsatt gyldig så logger vi antall minutter igjen
        if (existingOtp != null)
        {
            double minutesLeft = (existingOtp.ExpiresAt - DateTime.UtcNow).TotalMinutes;
            logger.LogInformation("A valid OTP code already exists for User {UserId}. " +
                                  "Expires in {MinutesLeft:F1} minutes", userId, minutesLeft);
            
            return Result<string>.Failure(
                AppError.Create(ErrorCode.OtpCooldown, "An existing OTP is already valid"));
        }
        
        // Oppretter koden
        var code = GenerateSecureCode();
        
        // Oppretter OtpCode-entiteten og lagrer i databasen
        var otpCode = new OtpCode
        {
            UserId = userId,
            Code = HashCode(code), // Hasher koden for lagring
            ExpiresAt = DateTime.UtcNow.AddMinutes(_otp.ExpirationMinutes),
        };
        
        // Try-catch for å stoppe race condition. Hvis en bruker kaller metoden samtidig,
        // så vil det ende opp 2 stk gyldige OtpKoder. Vi har et SQL-filter for som sikrer at dette ikke skjer
        try
        {
            await otpCodeRepository.AddAsync(otpCode, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // En annen request rakk å opprette koden først
            return Result<string>.Failure(
                AppError.Create(ErrorCode.OtpCooldown, "An existing OTP is already valid"));
        }
        
        return Result<string>.Success(code);
    }
    
    /// <inheritdoc />
    public async Task<Result> VerifyOtpCodeAsync(Guid userId, string userOtpCode, CancellationToken ct = default)
    {
        // Henter den aktive koden
        OtpCode? otpCode = await otpCodeRepository.GetActiveCodeAsync(userId, ct);
        
        // Hasher koden 
        string hashedInput = HashCode(userOtpCode);
        
        // Ikke korrekt epost - enten skrevet feil eller ondsinnet angrep
        if (otpCode == null)
        {
            logger.LogWarning("User {UserId} is attempting to verify Otp without an active code", userId);
            // Lik feilmelding som hvis koden ikke er korrekt
            return Result.Failure(AppError.Create(ErrorCode.OtpInvalidOrExpired, "Invalid or expired code"));
        }
        
        // Sjekker om det er flere forsøk igjen
        if (otpCode.FailedAttempts >= _otp.MaxFailedAttempts)
        {
            logger.LogWarning("User {UserId} has exceeded max OTP attempts", userId);
            return Result.Failure(AppError.Create(ErrorCode.OtpMaxAttemptsExceeded, 
                "Too many failed attempts. Try login again"));
        }
        
        // Sjekker at koden er korrekt
        bool codeMatches = CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hashedInput),
            Encoding.UTF8.GetBytes(otpCode.Code));
        
        // Koden eksisterer, men er ikke korrekt
        if (!codeMatches)
        {
            // Oppdaterer koden med feilet forsøk
            otpCode.FailedAttempts++;
            otpCode.LastAttemptAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync(ct);
            
            // Lik feilmelding som ikke-eksisterende bruker
            return Result.Failure(AppError.Create(ErrorCode.OtpInvalidOrExpired, "Invalid or expired code")); 
        }
        
        // Korrekt kode
        otpCode.IsUsed = true;
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ======================== Hjelpemetoder ========================
    
    /// <summary>
    /// Genererer en tilfeldig 6-sifret kode med RandomNumberGenerator
    /// </summary>
    private static string GenerateSecureCode() => RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    
    /// <summary>
    /// Hasher en kode for å slippe å lagre koden i klartekst med tanke på databasebrudd
    /// </summary>
    /// <param name="code">String med en kode</param>
    /// <returns>En hashet string med 64-tegn</returns>
    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }
}