# CompVault — Dokumentasjon

Dette er dokumentasjonen for CompVault, et system for digital personal- og kompetansehåndtering bygget som en fordypningsoppgave.

## Hva finner du her?

Mappen er delt i to deler: **oversiktsdiagrammer** og **moduldokumentasjon**.

---

## Oversiktsdiagrammer

Disse fire diagrammene gir et helhetsbilde av systemet. De er ment til å brukes i presentasjonen eller rapporten — start her for å forstå arkitekturen, og gå så inn i de individuelle modulene for detaljer.

| Diagram | Fil | Hva det viser |
|---------|-----|---------------|
| **Systemoversikt** | `system-oversikt.png` | Hva systemet gjør, og hvem som bruker det (medarbeider, leder, administrator) |
| **Teknisk arkitektur** | `teknisk-arkitektur.png` | Hvilke applikasjoner systemet består av: Blazor-frontend, ASP.NET-backend, PostgreSQL, fil-lagring, Resend API |
| **Modulavhengigheter** | `modul-avhengigheter.png` | Hvilke av de 8 backend-modulene som snakker med hverandre, og hvorfor |
| **Intern lag-struktur** | `intern-lag-struktur.png` | Hvordan data flyter gjennom lagene i backenden: Controller → Service → Repository → Database |

> **Tips til presentasjon:** Gå gjennom diagrammene i rekkefølgen over. Det bygger en naturlig progresjon fra "hva er dette?" → "hva består det av?" → "hvordan er det bygget inni?"

---

## Moduldokumentasjon

Hver modul har sin egen mappe med en struktur som følges over hele linja:

```
docs/
├── auth/
│   ├── README.md           ← Tekstdokumentasjon (problemstilling, teknisk design, valg)
│   ├── auth-er-diagram.pdf ← ER-diagram (tabeller, relasjoner, indekser)
│   └── auth-arkitektur.png ← Arkitekturdiagram (controller → service → repo → DB)
├── users/
│   ├── README.md
│   ├── users-er-diagram.pdf
│   └── users-arkitektur.png
├── ... (samme mønster for alle 9 moduler)
```

### Moduler i systemet

| Modul | Beskrivelse |
|-------|-------------|
| **Auth** | Passwordless innlogging med OTP på e-post. JWT access tokens + refresh tokens. |
| **Users** | Bruker-CRUD med avdeling, leder, stillingstittel og roller. Soft delete. |
| **RBAC** | Rollebasert tilgangskontroll med 37 permissions. Systemroller er beskyttet. |
| **Departments** | Avdelingshierarki med selvrefererende foreldre-avdeling. Validerer leder via `IsLeader`. |
| **JobTitles** | Stillingstitler. `IsLeader`-flagget avgjør om brukeren kan være avdelingsleder. |
| **Competencies** | Kompetanseregistrering med utløpsdatoer. Varsler ved 90, 60, 30, 14, 7 og 0 dager. |
| **Documents** | Dokumentstyring med typer, kategorier, versjonering, signering og målgruppe-filtrering. |
| **Equipment** | Utstyrsutlevering med kategorier, utstyrs-items og utleveringer med størrelse/quantity. |
| **Audit** | Logging av alle endringer i systemet. |

### Hvordan lese en modul

1. **Start med `README.md`** — den forklarer *hvorfor* modulen er bygget som den er (problemstilling, krav, tekniske valg).
2. **Se på `*-er-diagram.pdf`** — for å forstå datamodellen (tabeller, kolonner, relasjoner, indekser).
3. **Se på `*-arkitektur.png`** — for å forstå kode-strukturen (hvilke klasser som snakker med hvilke).

> **Merk:** Modulene er bygget som *vertical slices* — hver modul har sine egne controllere, services og repositories. Noen moduler leser fra andre modulers repositories (f.eks. leser `Documents` fra `Departments` og `JobTitles` for målgruppe-filtrering), men ingen modul skriver direkte til en annen moduls tabeller.

---

## Andre mapper

- **`seed-data/`** — Beskrivelse av testdata som seedes ved oppstart i Development-miljøet.
- **`images/`** — Delte bilder som brukes på tvers av dokumentasjonen.
