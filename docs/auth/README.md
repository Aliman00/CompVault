# Autentiseringsmodulen — Moduldokumentasjon

Autentiseringsmodulen tar seg av innlogging, tokens og brukersesjoner i CompVault. Vi gikk for passwordless med engangskoder (OTP) sendt på e-post, uten noe passord i det hele tatt. Målet var å få en flyt som er enkel for brukeren, men som samtidig tar høyde for de vanligste sikkerhetsproblemene.

## 1. Problemstilling og behov

Utgangspunktet for modulen var:
> Hvordan kan vi implementere en sikker og brukervennlig passwordless autentisering som beskytter mot vanlige angrepsvektorer?

Konkrete krav til løsningen:
- Brukeren må kunne logge inn med en engangskode sendt på e-post, helt uten passord.
- OTP-kodene må lagres som hash i databasen — hvis noen får tak i databasen, skal de ikke kunne bruke kodene direkte.
- Refresh tokens må støttes, så brukerne slipper å logge inn hele tiden.
- Responstiden må være lik uansett om e-posten finnes eller ikke, ellers kan man kartlegge brukere.
- Man skal ikke kunne finne ut hvilke e-postadresser som er registrert i systemet.

## 2. Teknisk design

### Datamodell

Modulen har to egne entiteter i tillegg til `ApplicationUser`: `OtpCode` og `RefreshToken`. Begge er koblet direkte til brukeren via `UserId` fremmednøkkel.

**OtpCode:**
| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel |
| `UserId` | Guid | Hvilken bruker koden tilhører |
| `Code` | varchar(64) | SHA-256 hash av den 6-sifrede koden |
| `CreatedAt` | DateTime | Når koden ble laget |
| `ExpiresAt` | DateTime | Når koden utgår (standard: 10 min) |
| `IsUsed` | bool | Om koden allerede er brukt |
| `FailedAttempts` | int | Antall feil forsøk på å taste inn koden |
| `LastAttemptAt` | DateTime? | Når siste forsøk ble gjort |

**RefreshToken:**
| Felt | Type | Hva det er |
|------|------|------------|
| `Id` | Guid | Primærnøkkel |
| `UserId` | Guid | Hvilken bruker tokenet tilhører |
| `Token` | varchar(128) | Selve tokenet (64 tilfeldige bytes, base64-kodet) |
| `CreatedAt` | DateTime | Når tokenet ble laget |
| `ExpiresAt` | DateTime | Når tokenet utgår (standard: 7 dager) |
| `IsRevoked` | bool | Om tokenet er tilbakekalt (logout) |

Begge tabellene har query-filtre som automatisk skjuler rader knyttet til soft-slettede brukere (`User.DeletedAt == null`). Den fullstendige datamodellen med indekser og relasjoner er dokumentert i `auth-er-diagram.pdf`.

**Viktig: Unik delvis indeks på OtpCode.** Det er en unik indeks på `UserId` med filteret `WHERE "IsUsed" = false`. Dette betyr at én bruker ALDRI kan ha to aktive (ubrukte) OTP-koder samtidig, selv om to parallelle requester prøver å opprette samtidig.

### Arkitektur

Flyten er bygd rundt `AuthService`, som er inngangspunktet for alle operasjoner. Samspillet mellom komponentene er 
vist i `auth-arkitektur.png`.

**Oversikt over komponentene:**

| Komponent | Type | Ansvar |
|-----------|------|--------|
| `AuthController` | Controller | Tar imot HTTP-kall og sender videre til AuthService. 4 auth-endepunkter + GET /me. Ingen autorisasjon på auth-endepunktene (public). |
| `AuthService` | Service | Orkestrerer flyten. Bruker transaksjoner for konsistens. Kaller TimingGuard i request/verify for timing-sikkerhet. |
| `OtpCodeService` | Service | Genererer og verifiserer koder. Sjekker cooldown, håndterer race conditions via databasens unike indeks, bruker constant-time sammenligning. |
| `RefreshTokenService` | Service | Oppretter refresh tokens med 64 bytes fra RandomNumberGenerator. |
| `JwtService` | Service | Genererer JWT access tokens med claims for brukerId, e-post, navn, department_id, roller og permissions. Signerer med HMAC-SHA256. |
| `PermissionService` | Service | Slår opp hvilke permissions som hører til et sett med roller. |
| `EmailService` | Service | Sender OTP-kode via e-post (Resend API). |
| `TokenCleanupJob` | BackgroundService | Kjører hver 24. time og sletter utgåtte/revokerte tokens og brukte/utgåtte OTP-koder direkte i databasen. |
| `TimingGuard` | Statisk klasse | Tar en Stopwatch og et minimum antall ms, og delayer hvis operasjonen gikk for fort. |
| `DevAuthController` | Controller | Kun i Development. Har dev-login, dev-create-otp og dev-get-users. Returnerer 404 i produksjon. |

### Designvalg

| Valg | Hvorfor |
|------|--------|
| **SHA-256 av OTP** | Kodene ligger aldri i klartekst i databasen. Hvis noen stjeler databasen, må de brute-force SHA-256 for å finne koden. |
| **Constant-time sammenligning** | `CryptographicOperations.FixedTimeEquals` — tiden det tar å sjekke koden avhenger ikke av hvor mange tegn som er riktige. Umuliggjør timing-angrep. |
| **6 siffer** | God balanse — 1 million kombinasjoner er mer enn nok med 3 forsøk og 10 minutters levetid. Kortere ble for svakt, lengre for tungvint. |
| **Access token: 15 min** | Kort levetid begrenser skaden hvis et token lekker. |
| **Refresh token: 7 dager** | Langt nok til å slippe å logge inn hele tiden, kort nok til å være forsvarlig. Roteres ved hver refresh. |
| **Token-rotasjon** | Hvert refresh token kan bare brukes én gang. Hvis noen stjeler det, oppdages det neste gang den ekte brukeren prøver å refreshe. |
| **Cooldown på OTP** | Hvis brukeren allerede har en aktiv kode, sendes det ikke ut ny. Hindrer spam og ressursbruk. |
| **Maks 3 feilforsøk** | Begrenser brute-force. Etter 3 feil må man be om ny kode. |
| **TimingGuard 500ms** | Både RequestOtp og VerifyOtp tar alltid minst 500ms, uansett utfall. Sammen med at vi alltid returnerer 200 OK på RequestOtp gjør det umulig å kartlegge brukere. |
| **Unik delvis indeks** | `WHERE "IsUsed" = false` på OtpCode.UserId. Stopper race conditions på SQL-nivå uten Redis eller distributed locks. |

## 3. Implementasjon

Hele innloggingsflyten starter i `AuthService`. Den har fire metoder som til sammen dekker hele livssyklusen til en brukersesjon — fra første OTP-forespørsel til logout.

Når noen taster inn e-posten sin og trykker "send kode", går kallet til `RequestOtpAsync`. Det første som skjer er at vi slår opp brukeren. Hvis e-posten ikke finnes, eller brukeren er deaktivert, logger vi bare en warning og returnerer `Success`. Systemet avslører aldri om en e-post er registrert eller ikke. Hvis brukeren finnes og er aktiv, går vi inn i en transaksjon: `OtpCodeService.GenerateOtpCodeAsync` lager en ny 6-sifret kode, hasher den med SHA-256, og lagrer i databasen. Så sendes koden på e-post via `EmailService`. Hvis e-posten feiler, får frontend beskjed — det er den eneste situasjonen der RequestOtp returnerer en feil. Til slutt kjører `TimingGuard`, som sørger for at hele operasjonen har tatt minst 500 ms uansett utfall.

Neste steg er `VerifyOtpAsync`. Her slår vi opp brukeren på nytt og verifiserer koden de har tastet inn — men denne gangen får de en feilmelding hvis noe er galt. `OtpCodeService.VerifyOtpCodeAsync` bruker `CryptographicOperations.FixedTimeEquals` for å sammenligne hashen, så en angriper kan ikke måle seg frem til riktig kode ved å se på responstiden. Hvis koden stemmer, markerer vi den som brukt, oppretter et refresh token, henter brukerens roller og permissions, og genererer en JWT. Alt dette skjer i én transaksjon — enten får brukeren et gyldig token-par, eller så skjer ingenting. `TimingGuard` kjører også her.

`RefreshTokenAsync` brukes når access-tokenet har gått ut og frontend sender inn et refresh token for å få et nytt. Vi henter tokenet fra databasen, sjekker at det ikke er revokert eller utgått, og at brukeren fortsatt er aktiv. Så revokerer vi det gamle tokenet og oppretter et nytt — dette er token-rotasjon. Et stjålet refresh token kan bare brukes én gang, og hvis en angriper bruker det først, vil den ekte brukerens neste refresh-forsøk feile.

`RevokeRefreshTokenAsync` er logout. Vi sjekker at tokenet finnes og faktisk tilhører den innloggede brukeren, og setter `IsRevoked = true`. Det kreves ingen transaksjon her — det er bare én oppdatering.

`OtpCodeService` har to viktige detaljer verdt å nevne. For det første: cooldown. Hvis `GetActiveCodeAsync` finner en aktiv kode fra før, nekter vi å lage en ny. For det andre: race conditions. Hvis to parallelle requests prøver å opprette OTP-kode samtidig, har databasen en unik delvis indeks (`WHERE "IsUsed" = false`) som stopper den andre. Vi fanger `DbUpdateException` og returnerer en cooldown-feil. Det er kanskje ikke den peneste feilhåndteringen, men det fungerer uten Redis eller distributed locks.

`RefreshTokenService` er enklest av dem alle — `CreateRefreshTokenAsync` genererer 64 tilfeldige bytes, base64-koder, og lagrer med utløpstid hentet fra `JwtSettings.RefreshTokenDays` (7 dager).

`JwtService` bygger access-tokenet. Det inneholder sub (userId), email, firstName, lastName, department_id, roller som role-claims, og permissions som permission-claims — signert med HMAC-SHA256 og 15 minutters levetid. `GetPrincipalFromExpiredToken` finnes også, men den brukes ikke aktivt — den er der for fremtidig bruk.

`TokenCleanupJob` er en `BackgroundService` som kjører hver 24. time. Den sletter utgåtte og revokerte refresh tokens, og brukte eller utgåtte OTP-koder — alt via `ExecuteDeleteAsync` direkte mot databasen.

`DevAuthController` er kun tilgjengelig i Development-miljøet. Den har `POST /dev-login` for direkte innlogging med e-post, `POST /dev-create-otp` som oppretter en fast kode (123456, 15 min gyldig) uten å sende e-post, og `GET /dev-get-users` for å liste brukere. I produksjon returnerer alle endepunktene 404.

## 4. Utfordringer og beslutninger

### Timing-sikkerhet uten å lekke informasjon

Hvis systemet svarer på 10 ms for en ukjent e-post og 200 ms for en kjent, kan man kartlegge brukere. Vi løste dette med `TimingGuard` — RequestOtp og VerifyOtp tar alltid minimum 500 ms. Sammen med at RequestOtp alltid returnerer 200 OK (selv for ukjent e-post), får man null informasjon ut av responstiden.

### Race conditions ved parallelle OTP-forespørsler

Det er mulig å sende to RequestOtp samtidig og ende opp med to aktive koder. Vår løsning: en unik delvis indeks i databasen (`WHERE "IsUsed" = false`) som blokkerer dette på SQL-nivå. I koden sletter vi først utgåtte koder, så prøver vi å lagre — hvis `DbUpdateException` fyres, fanger vi den og returnerer cooldown-feil. Ikke superelegant, men det funker uten Redis eller lignende.

### Valg av OTP-lengde

4 siffer = 10 000 kombinasjoner, for lite. 8 siffer = 100 millioner, men tungvint på mobil. 6 siffer = 1 million, perfekt balanse med 3 forsøk og 10 min levetid.

### Rotasjon av refresh tokens

Alternativet var langvarige tokens (30 dager), men da er konsekvensen av lekkasje mye større. Med rotasjon kan et stjålet token bare brukes én gang — brukes det, oppdages det ved neste refresh-forsøk fra den ekte brukeren.

### Cooldown og feilforsøk

Ingen ny kode hvis det allerede finnes en aktiv. Maks 3 forsøk per kode. Enkle regler, enkle å forstå, enkle å teste. Dekker de viktigste angrepene.

## 5. Testing

Tester for auth finnes i `CompVault.Backend.Tests/Backend/Features/Auth/`:
- `AuthServiceRequestOtpAsyncTests.cs` — Tester RequestOtp-flyten (ukjent e-post, inaktiv bruker, vellykket utsending)
- `AuthServiceVerifyOtpAsyncTests.cs` — Tester VerifyOtp-flyten (ukjent e-post, feil kode, korrekt kode, for mange forsøk)
- `AuthServiceRefreshTokenAsyncTests.cs` — Tester refresh-flyten (ugyldig token, gyldig token, rotasjon)
- `AuthServiceRevokeRefreshTokenAsyncTests.cs` — Tester revoke-flyten (ugyldig token, annen brukers token, vellykket revoke)
- `OtpCodeServiceTests.cs` — Tester OTP-generering, cooldown, verifisering
- `RefreshTokenServiceTests.cs` — Tester token-generering og lagring

## 6. Vurdering og refleksjon

*(Denne seksjonen skal fylles ut senere — tanker rundt hva som fungerte bra, hva som kunne vært gjort annerledes, og lærdommer fra implementasjonen.)*

## 7. Relaterte moduler

| Modul | Relasjon |
|-------|----------|
| **Users** | `OtpCode.UserId` og `RefreshToken.UserId` peker på `ApplicationUser` |
| **RBAC** | `JwtService` baker roller og permissions inn i JWT; `PermissionService` brukes under innlogging |
| **Competencies** | Krever autentisering for administrasjon |
| **Department** | Krever autentisering for administrasjon |
| **Documents** | Krever autentisering for opplasting, signering og nedlasting |
