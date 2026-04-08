# RequestOtpAsync

Håndterer forespørsler om engangskode (OTP) for passordløs innlogging.

Returnerer alltid suksess til klienten uavhengig av utfall, 
slik at angripere ikke kan kartlegge hvilke e-poster som er registrert i systemet. Et unntak er hvis epost-tjenesten
er nede, så returner da en egen feilmelding for det.

### Flyt

1. Slår opp brukeren på e-post
2. Logger hvis e-posten er ukjent eller kontoen er deaktivert. Vi avslører ikke dette til frontend
3. Hvis brukeren eksisterer og er aktiv: genereres en 6-sifret kode, hashes med SHA-256 og lagres i databasen med utløpstid
4. Koden sendes til brukeren via valgt kanal (e-post eller SMS) - Kun Epost er implementert enda
5. OTP-generering og e-postsending skjer i en transaksjon, slik at hvis epost sending feiler så rulles OTP-koden tilbake

### Beskyttelse mot misbruk

- **Per-konto cooldown** — ny kode kan ikke forespørres før eksisterende kode er utgått
- **Race condition-beskyttelse** — filtrert unik indeks i databasen sikrer at en bruker kan kun ha en aktiv kode omgangen
- **Delay med TimingGuard** - delayer metoden slik at den alltid bruker en minimum tid. Gir ingen ondsinnete brukere sjansje til å gjette om brukeren eksisterer eller ikke

---

# VerifyOtpAsync

Verifiserer en engangskode (OTP) og returnerer et JWT token-par ved suksess.

Returnerer samme feilmelding uavhengig av om brukeren ikke eksisterer, kontoen er deaktivert,
koden er feil, eller koden er utgått. Slik at angripere ikke kan utlede informasjon om systemet. Frontend skal
håndtere antall forsøk, men backend teller og har en maksgrense den også.

### Flyt

1. Slår opp brukeren på e-post
2. Logger hvis e-posten er ukjent eller kontoen er deaktivert. Vi avslører ikke dette til frontend/brukeren
3. Returnerer generisk feilmelding hvis brukeren ikke eksisterer, er deaktivert eller slettet
4. Verifiserer OTP-koden mot lagret hash. Vi oppdaterer OTP-koden med antall feilede forsøk alltid - dette skal ikke være med i transaksjonen som ruller tilbake ved Failure
5. Returnerer generisk feilmelding hvis ingen aktiv kode finnes, eller koden ikke stemmer
6. Ved suksess: `IsUsed = true` og Refresh Token lagres samtidig i en transaksjon. Lykkes IsUsed = true, og Refresh Token feiler, så er ikke OTP-koden satt som brukt
7. Returnerer access token og refresh token i `RefreshTokenResponse`

### Beskyttelse mot misbruk

- **Samme feilmelding for alle feilscenarioer** — angripere kan ikke skille mellom ukjent bruker, feil kode eller utgått kode
- **Constant-time sammenligning** — `CryptographicOperations.FixedTimeEquals` forhindrer timing-angrep ved kodesammenligning
- **Maks antall forsøk** — koden låses etter et konfigurerbart antall feilede forsøk, med egen feilmelding for dette tilfellet
- **Delay med TimingGuard** — metoden bruker alltid en minimumstid via `finally`-blokk, uavhengig av utfall

---

# RefreshTokenAsync

Roterer et eksisterende refresh token og returnerer et nytt Access Token (Jwt-token) og et nytt Refresh Token.
Refresh Token lagres i databasen.

### Flyt

1. Henter og validerer refresh token fra databasen
2. Returnerer feilmelding hvis tokenet er ugyldig, utgått eller revokert
3. Henter brukeren og validerer at den er aktiv
4. Revoker det gamle tokenet og oppretter et nytt i en transaksjon, med mulighet for rollback
5. Returnerer nytt access token og refresh token i `RefreshTokenResponse`

### Token rotation

Hvert refresh token kan kun brukes en gang. Ved fornyelse revokers det gamle tokenet
og et nytt utstedes — alt i en transaksjon. Feiler opprettelsen av nytt token forblir
det gamle tokenet gyldig og brukeren kan prøve igjen.
Gamle tokens blir ryddet opp i en bakgrunnjob som kjører hver natt.

---

# RevokeRefreshTokenAsync

Revoker et aktivt refresh token, og logger brukeren effektivt ut.

### Flyt

1. Henter tokenet fra databasen — kun gyldige tokens kan revokers
2. Returnerer feilmelding hvis tokenet er ugyldig eller allerede revokert
3. Setter `IsRevoked = true` og lagrer

---

TODO:
- **IP-basert rate limiting** — maks antall forespørsler per IP per tidsenhet (Implementere senere?)
- **SMS-tjeneste** — Implementere en SMSService og tjeneste for å sende SMS