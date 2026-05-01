# Infrastructure/Email

`Infrastructure/Email/` samler det som har med utsending av e-post å gjøre. I prosjektet brukes dette blant annet til OTP-koder og andre meldinger som sendes ut fra backend.

## Struktur

```text
Infrastructure/Email/
├── IEmailService.cs      <- Interface for e-postoperasjoner
├── EmailService.cs       <- Implementasjon (Resend.NET)
├── Config/
│   └── EmailSettings.cs  <- Konfigurasjon for API-nøkkel og avsenderadresse
├── Models/
│   └── EmailBody.cs      <- Modell for e-postinnhold (subject + html)
└── Templates/
    ├── EmailTemplates.cs         <- Generiske maler (OTP, etc.)
    └── CompetencyEmailTemplates.cs  <- Varsler for kompetanseutløp
```

## Hvordan vi bruker denne mappen

Vi lar resten av applikasjonen forholde seg til `IEmailService`, i stedet for å vite noe om hvordan e-posten faktisk sendes. Det gjør det enklere å bytte leveringsmåte senere uten å måtte rydde opp i mange features samtidig.

Maler ligger separat, slik at service-laget slipper å bygge opp HTML direkte. Det holder ansvaret litt ryddigere, og gjør e-postflyten lettere å lese når man kommer tilbake til den senere.

## Eksempel på bruk

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

## Retningslinjer

- Injiser `IEmailService`, ikke `EmailService` direkte.
- Maler skal returnere innholdet som skal sendes, ikke stå for selve utsendingen.
- Metoder bør ta `CancellationToken ct = default` der det er naturlig.

Konfigurasjon som API-nøkkel og avsenderadresse legges i `appsettings.json` via `EmailSettings`, ikke i kode.
