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

### Beskyttelse mot misbruk

- **Per-konto cooldown** — ny kode kan ikke forespørres før eksisterende kode er utgått
- **Delay med TimingGuard** - delayer metoden slik at den alltid bruker en minimum tid. Gir ingen ondsinnete brukere sjansje til å gjette om brukeren eksisterer eller ikke

# VerifyOtpAsync

Verifiserer en engangskode (OTP) og returnerer et JWT token-par ved suksess.

Returnerer samme feilmelding uavhengig av om brukeren ikke eksisterer, kontoen er deaktivert,
koden er feil, eller koden er utgått. Slik at angripere ikke kan utlede informasjon om systemet. Frontend skal
håndtere antall forsøk, men backend teller og har en maksgrense den også.

### Flyt

1. Slår opp brukeren på e-post
2. Logger hvis e-posten er ukjent eller kontoen er deaktivert. Vi avslører ikke dette til frontend/brukeren
3. Returnerer generisk feilmelding hvis brukeren ikke eksisterer, er deaktivert eller slettet
4. Verifiserer OTP-koden mot lagret hash
5. Returnerer generisk feilmelding hvis ingen aktiv kode finnes, eller koden ikke stemmer
6. Ved suksess: markeres koden som brukt, og et JWT access token + refresh token returneres i LoginResponse

### Beskyttelse mot misbruk

- **Samme feilmelding for alle feilscenarioer** — angripere kan ikke skille mellom ukjent bruker, feil kode eller utgått kode
- **Constant-time sammenligning** — `CryptographicOperations.FixedTimeEquals` forhindrer timing-angrep ved kodesammenligning
- **Maks antall forsøk** — koden låses etter et konfigurerbart antall feilede forsøk, med egen feilmelding for dette tilfellet
- **Delay med TimingGuard** — metoden bruker alltid en minimumstid via `finally`-blokk, uavhengig av utfall

TODO:
- **Refresh token lagring** — refresh token genereres men lagres ikke ennå; må persisteres i databasen for å kunne valideres ved neste bruk

TODO:
- **IP-basert rate limiting** — maks antall forespørsler per IP per tidsenhet (Implementere senere?)
- **SMS-tjeneste** — Implementere en SMSService og tjeneste for å sende SMS