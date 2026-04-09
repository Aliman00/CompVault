namespace CompVault.Frontend.Common.Http.Models;

/// <summary>
/// Enkel record for å sikre at alle requester får samme AccessToken, RefreshToken
/// og klokkeslettet når de var oppdatert
/// </summary>
/// <param name="AccessToken">Access-token fra backend</param>
/// <param name="RefreshToken">Refresh token fra backend</param>
/// <param name="RefreshedAt">Klokkeslettet når det var oppdatert</param>
public record RefreshRecord(string AccessToken, string RefreshToken, DateTimeOffset RefreshedAt);