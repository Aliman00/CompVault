# CompVault — Dokumentasjon

Her ligger all dokumentasjon for CompVault. Mappen er delt i to: oversiktsdiagrammer og modul-dokumentasjon.

---

## Oversiktsdiagrammer

Start med disse fire diagrammene hvis du vil ha et helhetsbilde av systemet. De er laget for presentasjonen, så de viser det viktigste uten å drukne i detaljer.

| Diagram | Fil | Hva det viser |
|---------|-----|---------------|
| Systemoversikt | `system-oversikt.png` | Hva systemet gjør og hvem som bruker det |
| Teknisk arkitektur | `teknisk-arkitektur.png` | Hvilke applikasjoner det består av |
| Modulavhengigheter | `modul-avhengigheter.png` | Hvilke moduler som snakker sammen |
| Intern lag-struktur | `intern-lag-struktur.png` | Hvordan data flyter i backenden |

Rekkefølgen over er egentlig den beste rekkefølgen å presentere dem i. Da bygger man fra "hva er dette" → "hva består det av" → "hvordan er det bygget inni".

---

## Moduldokumentasjon

Hver modul har sin egen mappe med samme struktur:

```
docs/
├── auth/
│   ├── README.md           ← tekst (problemstilling, valg, design)
│   ├── auth-er-diagram.pdf ← tabeller, relasjoner, indekser
│   └── auth-arkitektur.png ← controller → service → repo → DB
├── users/
│   ├── README.md
│   ├── users-er-diagram.pdf
│   └── users-arkitektur.png
└── ... (samme mønster for alle moduler)
```

### Modulene i systemet

- **Auth** — Passwordless innlogging med OTP på e-post. JWT + refresh tokens.
- **Users** — Bruker-CRUD med avdeling, leder, stillingstittel og roller. Soft delete.
- **RBAC** — Roller og 37 permissions. Systemroller kan ikke slettes.
- **Departments** — Avdelingshierarki. Validerer at leder faktisk har `IsLeader`.
- **JobTitles** — Stillingstitler. `IsLeader` avgjør om brukeren kan være avdelingsleder.
- **Competencies** — Kompetanseregistrering med utløpsdatoer. Varsler ved 90, 60, 30, 14, 7 og 0 dager.
- **Documents** — Dokumenter med typer, kategorier, versjonering, signering og målgrupper.
- **Equipment** — Utstyrsutlevering med kategorier, items og utleveringer.
- **Audit** — Logger alle endringer i systemet.

### Sånn leser du en modul

1. Start med `README.md`. Den forklarer hvorfor modulen er bygget som den er — hva var problemet, hva prøvde vi å løse, og hvilke valg tok vi underveis.
2. Se på `*-er-diagram.pdf` for å forstå datamodellen.
3. Se på `*-arkitektur.png` for å forstå kode-strukturen.

Modulene er bygget som *vertical slices* — hver modul har egne controllere, services og repositories. Noen moduler leser fra andre modulers repositories (f.eks. leser Documents fra Departments og JobTitles for målgruppe-filtrering), men ingen modul skriver direkte til en annen moduls tabeller.

---

## Andre filer

- `tekniske-hoydepunkter.md` — Backend-detaljer som CI/CD, Docker, testing, sikkerhet, audit. Dette er ting som ligger i koden men lett går under radaren.
- `frontend-hoydepunkter.md` — Frontend-detaljer: komponenter, services, JWT-håndtering, responsivt UI.
- `backend/` — Oppsett av Program.cs, konfigurasjon, health checks, CORS, seeding.
- `flytdiagram/` — Brukerflyter (innlogging, opprette bruker, etc.) som viser steg-for-steg gjennom frontend og backend.
- `seed-data/` — Hva slags testdata som seedes ved oppstart i Development.
- `images/` — Delte bilder som brukes flere steder i dokumentasjonen.
