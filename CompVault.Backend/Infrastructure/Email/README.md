# Infrastructure/Email

> SMTP-basert e-posttjeneste brukt til OTP-levering og varsler.

## Struktur

```
Email/
  IEmailService.cs        <- interface som injiseres i services
  EmailService.cs         <- SMTP-implementasjon
  Config/
    EmailSettings.cs      <- konfigurasjonsobjekt, bindes fra appsettings.json
  Models/
    EmailBody.cs          <- modell for e-postinnhold (mottaker, emne, kropp)
  Templates/
    EmailTemplates.cs     <- statiske HTML-maler
```

## Bruk

Injiser `IEmailService` i ønsket service:

```csharp
public class AuthService(IEmailService emailService) : IAuthService
{
    public async Task SendOtpAsync(string toEmail, string otpCode, CancellationToken ct)
    {
        var body = EmailTemplates.OtpEmail(otpCode);
        await emailService.SendAsync(toEmail, "Din engangskode", body, ct);
    }
}
```

## Konfigurasjon

Sett SMTP-verdier i `appsettings.json` (bruk `appsettings.Development.json` lokalt):

```json
"EmailSettings": {
  "Host": "smtp.example.com",
  "Port": 587,
  "Username": "no-reply@example.com",
  "Password": "...",
  "FromAddress": "no-reply@example.com",
  "FromName": "CompVault"
}
```

`EmailSettings` registreres i DI via `Infrastructure/Extensions/ServiceCollectionExtensions.cs`.

## Ny e-postmal? Gjør slik

1. Legg til en statisk metode i `Templates/EmailTemplates.cs` som returnerer en HTML-streng
2. Kall metoden fra aktuell service via `IEmailService`
