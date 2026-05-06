# Sensorveiledning — Oppstart og testing

## Oversikt

CompVault er et lukket system: brukere kan **ikke** registrere seg selv. En administrator må opprette kontoen. Innlogging skjer med engangskode (OTP) på e-post — ikke passord.

Denne guiden forklarer hvordan du får systemet i gang og oppretter en testbruker for deg selv.

---

## Hurtigstart (anbefalt)

Den enkleste måten er å kjøre alt i Docker. Naviger inn i root-mappen til prosjektet (der hvor docker-compose.yml ligger), eksempelvis C:\Users\user\CompVault\ og deretter kjør kommandoen:

```bash
docker compose up -d
```

Dette starter database, backend og frontend samtidig.

---

## Alternativ: manuell oppsett

Hvis du foretrekker å teste prosjektene seperat eller uten å kjøre alle samtidig, følg rekkefølgen nedenfor.

### 1. Database

Backend krever at databasen kjører, da den prøver å seede inn eksempeldata ved oppstart. Start databasen først:

```bash
# Fra rotmappen — starter kun databasen
docker compose up postgres -d
```

> **Merk:** Kjører du databasen på en annen måte, oppdater tilkoblingsstrengen i `.env`-filen i rotmappen (`CompVault/`).

### 2. Backend

Viktig å kjøre migrering manuelt før applikasjonen prøver å seede inn eksempeldata.

```bash
cd CompVault.Backend
dotnet ef database update   # Kjører migreringer og seed-data
dotnet run                  # Starter API på http://localhost:5010
```

### 3. Frontend

```bash
cd CompVault.Frontend
dotnet run                  # Starter Blazor Server
```

---

## Testing i frontend

For å teste i frontend, så må både frontend, database og backend kjøre eller alt med docker-compose. Frontend kjører normalt på http://localhost:5020.

1. Naviger til http://localhost:5020
2. Uten å være innlogget så blir man sendt til innloggings-sidene.
3. Naviger til http://localhost:5020/Dev
4. Velg en ønsket bruker for å automatisk logge inn med valgt bruker. Du blir navigert til http://localhost:5020/dashboard etter innlogging.
5. Gå tilbake til http://localhost:5020/Dev for å se tilattelsene til brukeren du er innlogget med.
6. Hvis du ønsker å teste innloggingsflyten som brukes i produksjon, må du være logget inn med en bruker med users:write tilattelse (rollen Admin har alltid denne tilattelsen).
7. Naviger via menyen: Brukere og roller -> Brukere -> "Opprett bruker" øverst i høyre, eller med URL http://localhost:5020/users/create.
8. Skriv inn en ekte e-post for å motta OTP-kode på epost, og fyll inn minimum feltene for fornavn, etternavn og avdeling.
9. Trykk opprett
10. Du blir spurt om å tildele rolle etter opprettelse. Det er valgfritt om du ønsker å gjøre det.
11. Log ut ved å trykke på ikonet oppe i høyre hjørnet, og nederst i sidebaren trykk "Logg ut". 
12. Du blir sendt tilbake til http://localhost:5020/. Skriv inn e-post og trykk på "Send engangskode"
13. Du vil straks motta en e-post og kan lime inn OTP-koden fra e-posten. Trykk "Logg inn" og du er nå innlogget.
14. Test videre slik du ønsker. Rolle og tilattelser kan hindre deg fra å se admin/leder-valgene i sidebaren til venstre. Bytt eventuelt til en admin hvis du ønsker å teste alle funksjonaliteter.

---

## Opprette bruker og teste OTP

Når backend kjører, bruk filen [`otp-quickstart.http`](./CompVault.Backend/otp-quickstart.http) i Visual Studio Code (krever **REST Client**-utvidelsen) eller i Rider.

Filen inneholder steg-for-steg instruksjoner for hele flyten:

1. Logg inn som forhåndsseedet admin (Lisa).
2. Hent avdelinger og roller.
3. Opprett din egen brukerkonto.
4. Be om OTP-kode på e-post.
5. Verifiser koden og motta JWT-token.


---

## Testing med swagger

Swagger er konfigurert i applikasjonen vår og alle endepunktene kan testes der. Naviger til http://localhost:5010/swagger og følg stegene for å sette en autorisert bruker med token i Bearer. Krever at database og backend kjører.

1. Naviger til http://localhost:5010/swagger/index.html
2. Bla ned til DevAuth
3. Kjør GET-endepunktet /api/auth/dev-get-users
4. Velg en en bruker og kopier eposten til en bruker fra Response Body. Lise Hansen er en administrator med alle tilattelser, bruk gjerne hennes. E-post: lise.hansen@lekestua.no
5. Gå til endepunktet over kalt /api/auth/dev-create-otp. Lim inn e-posten til valgt bruker. Dette lager en OTP-kode på brukeren som er satt til 123456.
6. Gå til endepunktet /api/Auth/verify-otp under Auth
7. Lim inn e-mailen til brukeren og koden 123456.
Eksempel:
{
  "email": "lise.hansen@lekestua.no",
  "otpCode": "123456"
}
8. Kopier access token fra response body. Kan se noe slikt ut, bare mye lengre: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
9. Bla helt øverst i høyre hjørne og trykk på Authorize.
10. Lim inn access token og trykk Authorize. Viktig å ikke skrive Bearer forann token.
11. Du er nå logget inn. Test alle endepunktene slik du måtte ønske.



## Alternativ testing med Dev-endepunkter

Hvis du heller vil bruke Postman, curl eller lignende, finnes det tre dev-verktøy som **kun er tilgjengelige i Development-miljøet**:

| Endepunkt | Beskrivelse |
|-----------|-------------|
| `POST /api/auth/dev-login` | Logg inn som seedet bruker (returnerer access + refresh token) |
| `POST /api/auth/dev-create-otp` | Oppretter en fast OTP-kode (`123456`) i databasen — slipper å sjekke e-post |
| `GET /api/auth/dev-get-users` | List alle brukere i systemet |

### Rask OTP-flyt med dev-endepunkter

I stedet for å vente på e-post i steg 5–6, kan du bruke dev-flyten:

```bash
# 1. Opprett fast OTP-kode for brukeren
curl -X POST 'http://localhost:5010/api/auth/dev-create-otp' \
  -H 'Content-Type: application/json' \
  -d '{"email": "din@epost.no"}'

# 2. Verifiser med koden 123456 — får tilbake accessToken + refreshToken
curl -X POST 'http://localhost:5010/api/auth/verify-otp' \
  -H 'Content-Type: application/json' \
  -d '{
    "email": "din@epost.no",
    "otpCode": "123456"
  }'
```

### Eksempel: dev-login som admin

```bash
curl -X POST 'http://localhost:5010/api/auth/dev-login' \
  -H 'Content-Type: application/json' \
  -d '{
    "email": "lise.hansen@lekestua.no",
    "password": "TempPass123!"
  }'
```

Responsen inneholder `accessToken` som du bruker som `Authorization: Bearer <token>` på beskyttede endepunkter.

---

## Viktige merknader

| | |
|---|---|
| **Miljø** | Applikasjonen kjører alltid i `Development`. Dette gir seed-data (avdelinger, roller, brukere) og aktiverer dev-endepunktene ovenfor. |
| **E-post** | OTP-koder sendes til den e-postadressen du bruker ved opprettelse. Sjekk innboksen — og eventuelt spam — etter steg 5 i `.http`-flyten. |
| `.env` | Ved manuell oppsett: sjekk at `.env` i `CompVault.Backend` peker til riktig database. |

---

**Spørsmål?** Alt du trenger for å teste OTP-flyten ligger i `.http`-filen med inline-kommentarer.
