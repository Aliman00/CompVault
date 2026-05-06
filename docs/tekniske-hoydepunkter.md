# Tekniske ting vi er stolte av

Dette er ikke en del av hoveddokumentasjonen, men en liste over ting vi har gjort i backenden som vi synes er verdt å nevne. Mye av dette er ting man typisk ikke gjør i studentprosjekter, men som vi valgte å ta med likevel.

---

## 1. CI/CD med GitHub Actions

Vi har to workflows som kjører automatisk:

- **build-and-test.yml** — kjører ved hver push og PR. Bygger prosjektet og kjører alle tester. Hvis testene feiler, blokkeres merge.
- **docker-build-push.yml** — bygger Docker-image og pusher til GitHub Container Registry.

Dette betyr at vi aldri manuelt bygger eller deployer — det skjer automatisk. Vi vet også med én gang om noe er ødelagt.

---

## 2. Docker Compose med health checks

Hele stacken spinnes opp med `docker compose up`. Det starter:
- PostgreSQL 16
- ASP.NET-backend
- Blazor-frontend
- Adminer (database-GUI)

Det som er kult her er at backend **venter** på at Postgres faktisk er klar (`pg_isready`), og frontend venter igjen på at backendens `/health` svarer. Så det blir ikke race conditions der frontend prøver å snakke med backend før backend er oppe.

---

## 3. Dockerfile med non-root user

Backendens Dockerfile bruker multi-stage build, som betyr at produksjons-imaget bare inneholder det absolutt nødvendige. I tillegg kjører containeren som en non-root user (`appuser`), som er standard praksis i produksjon.

---

## 4. Testing på flere nivåer

Vi har ikke bare enkle unit-tester. Vi har:

- **Unit-tester** — isolerte klasser (f.eks. `CompetencyStatusCalculator`)
- **Integrasjonstester** — bruker `BackendWebApplicationFactory` som spinner opp hele backenden i minnet med ekte database. Tester autorisasjon, audit, avdelings-scoping.
- **Interceptor-tester** — tester EF Core-interceptoren som skriver audit-logg

Testene kjører automatisk i CI ved hver commit.

---

## 5. Sikkerhet i auth-flyten

Vi gikk for passwordless OTP i stedet for tradisjonelle passord. Noen av sikkerhetsdetaljene:

- OTP-koden hashes med SHA-256 før den lagres. Databasen ser aldri klartekst.
- **Constant-time sammenligning** — angriperen kan ikke måle responstid for å gjette koden.
- **TimingGuard** — `RequestOtp` og `VerifyOtp` har minimum 500ms responstid. Selv om operasjonen går raskere, injectes det en delay. Dette forhindrer timing-attacks og bruker-kartlegging.
- **Unik delvis indeks** — én bruker kan aldri ha to aktive OTP-koder samtidig, selv ved race conditions.

---

## 6. Soft delete med globale query filters

Ingenting slettes "hardt". Alle entiteter har `DeletedAt` og `IsActive`. EF Core har globale query filters som automatisk filtrerer bort soft-slettede rader. Så utvikleren trenger ikke huske `WHERE DeletedAt IS NULL` i hver spørring.

Unntaket er repositories som bevisst bruker `IgnoreQueryFilters()` når de trenger historiske data.

---

## 7. Audit-logging

Alle endringer logges automatisk via en EF Core-interceptor. Den fanger opp:
- Hvilken entitet som ble endret
- Hvilke felter som ble endret (gammel verdi → ny verdi)
- Hvem som gjorde det (fra JWT)
- Når det skjedde
- Hvilken HTTP-action som utløste det

Så vi har full sporbarhet uten å manuelt skrive logg-linjer i hver service.

---

## 8. RBAC med refleksjon

Systemet har 37 permissions i format `resource:action`. I stedet for å manuelt registrere 37 policies i `Program.cs`, itererer koden over `Permissions`-klassen via refleksjon og registrerer dem dynamisk. Nye permissions fungerer automatisk.

---

## 9. Background services

Tre tjenester kjører i bakgrunnen:

- **TokenCleanupJob** — sletter utgåtte tokens og brukte OTP-koder hver 24. time
- **CompetencyStatusJob** — bulk-oppdaterer kompetanse-status ved oppstart + hver 24. time
- **ExpiryNotificationJob** — sender e-postvarsler ved 90, 60, 30, 14, 7 og 0 dager. Har deduplisering så brukeren ikke blir spamma.

---

## 10. Avdelings-scoping

Mange moduler har avdelings-begrensning: en leder ser kun data fra egen avdeling og underavdelinger. Dette er implementert som en generisk tjeneste (`DepartmentScopeService`) som brukes på tvers av moduler, ikke duplisert i hver repository.

---

Sånn sett er dette et studentprosjekt, men vi har prøvd å bygge det med tanke på at det skulle vært produksjonsklart. Automatisert bygg, containerisering, testing, sikkerhet — det er ting som tar tid å sette opp, men som viser at man har tenkt lenger enn "det funker på min maskin".
