# Barnehage – Roller og tilganger

Denne filen beskriver de fire rollene som finnes i seed-data for den fiktive barnehagen **"Lekestua"**, og hvilke tilganger (permissions) hver rolle har.

## Roller

| Rolle | Hvem | Skop |
|---|---|---|
| **Admin** | Almin, Majlinda, Fredrik, Lise (daglig leder) | Alle avdelinger – full tilgang |
| **Avdelingsleder** | Anne (Storebarn), Bente (Småbarn) | Egen avdeling + alle underavdelinger |
| **Gruppeleder** | Sofie, Sara, Nora, Hans, Kari, Grete | Kun egen underavdeling/gruppe |
| **Ansatt** | Pedagoger, assistenter, rådgivere | Kun egen data – **ingen** tilgang til brukerlisten |

---

## Permission-tabell

| Tilgang | Admin | Avdelingsleder | Gruppeleder | Ansatt |
|---|---|---|---|---|
| `users:read` | ✅ | ✅ | ✅ | ❌ |
| `users:read:sub` | ✅ | ✅ | ❌ | ❌ |
| `users:read:all` | ✅ | ❌ | ❌ | ❌ |
| `users:write` | ✅ | ✅ | ❌ | ❌ |
| `users:delete` | ✅ | ✅ | ❌ | ❌ |
| `departments:read` | ✅ | ✅ | ✅ | ❌ |
| `departments:read:sub` | ✅ | ✅ | ❌ | ❌ |
| `departments:read:all` | ✅ | ❌ | ❌ | ❌ |
| `departments:write` | ✅ | ❌ | ❌ | ❌ |
| `departments:delete` | ✅ | ❌ | ❌ | ❌ |
| `competencies:read` | ✅ | ✅ | ✅ | ✅ |
| `competencies:read:sub` | ✅ | ✅ | ❌ | ❌ |
| `competencies:write` | ✅ | ✅ | ✅ | ❌ |
| `competencies:delete` | ✅ | ❌ | ❌ | ❌ |
| `documents:read` | ✅ | ✅ | ✅ | ✅ |
| `documents:read:sub` | ✅ | ✅ | ❌ | ❌ |
| `documents:write` | ✅ | ✅ | ✅ | ❌ |
| `documents:delete` | ✅ | ❌ | ❌ | ❌ |
| `documents:sign` | ✅ | ✅ | ✅ | ✅ |
| `equipment:read` | ✅ | ✅ | ✅ | ✅ |
| `equipment:read:sub` | ✅ | ✅ | ❌ | ❌ |
| `equipment:write` | ✅ | ✅ | ✅ | ❌ |
| `equipment:delete` | ✅ | ❌ | ❌ | ❌ |
| `job_titles:read` | ✅ | ✅ | ✅ | ✅ |
| `job_titles:write` | ✅ | ❌ | ❌ | ❌ |
| `job_titles:delete` | ✅ | ❌ | ❌ | ❌ |
| `document_types:read` | ✅ | ✅ | ✅ | ✅ |
| `document_types:write` | ✅ | ❌ | ❌ | ❌ |
| `document_types:delete` | ✅ | ❌ | ❌ | ❌ |
| `admin:access` | ✅ | ❌ | ❌ | ❌ |
| `audit:read` | ✅ | ❌ | ❌ | ❌ |

---

## Praktisk betydning

| Handling | Admin | Avdelingsleder | Gruppeleder | Ansatt |
|---|---|---|---|---|
| **Se brukerliste** | Alle i systemet | Egen avd. + underavd. | Kun egen gruppe | ❌ Nei |
| **Se egen profil** | ✅ | ✅ | ✅ | ✅ (via `GET /api/auth/me`) |
| **Se annen brukers profil** | ✅ | ✅ | ✅ | ❌ Nei |
| **Opprette/endre bruker** | ✅ | ✅ (innen sin gren) | ❌ Nei | ❌ Nei |
| **Slette bruker** | ✅ | ✅ (innen sin gren) | ❌ Nei | ❌ Nei |
| **Se avdelingshierarki** | Hele treet | Egen gren | Egen gren | ❌ Nei |
| **Registrere kompetanse** | ✅ | ✅ | ✅ | ❌ Nei |
| **Signere dokument** | ✅ | ✅ | ✅ | ✅ |
| **Se utstyrsutlevering** | Alle | Egen + under | Egen gruppe | Kun egen |
| **Tilgå adminpanel** | ✅ | ❌ | ❌ | ❌ |

---

## Merknader

1. **Ansatt har `users:read` fjernet.**
   – `/users`-siden gir 403.
   – Egen profil henter de via `GET /api/auth/me` (nytt endepunkt).

2. **Avdelingsleder har fullt HR-ansvar i sin gren.**
   – Kan opprette, endre og slette ansatte i egen avdeling og alle underliggende grupper.
   – Scoping sikrer at de kun ser og administrerer brukere innenfor sitt hierarki.

3. **Gruppeleder kan IKKE administrere brukere.**
   – Kan se ansatte i sin egen gruppe, men ikke opprette, endre eller slette dem.
   – Personalsaker (ansettelser, oppsigelser) går via avdelingsleder.

4. **Rådgivere (Ola, Kari, Tobias)** er satt som `Ansatt` inntil videre.
   – Daglig leder vurderer om de skal ha egen rolle med tilgang til brukerlisten.

5. **Scoping skjer automatisk** gjennom `DepartmentScopeService`.
   – `users:read` uten `users:read:sub` → kun egen avdeling.
   – `users:read` + `users:read:sub` → egen avdeling + alle underavdelinger.
   – `users:read:all` → bypass scoping (alle avdelinger).

---

## Organisasjonskart

```
System                    Ledelse
├── Almin (Admin)         ├── Lise (Admin — daglig leder)
├── Majlinda (Admin)      ├── Ola (Ansatt — rådgiver)
├── Fredrik (Admin)       ├── Kari (Ansatt — rådgiver)
                          └── Tobias (Ansatt — rådgiver)

Storebarns avdeling                    Småbarns avdeling
└── Anne (Avdelingsleder)             └── Bente (Avdelingsleder)
    ├── Sol                               ├── Gresshoppe
    │   ├── Sofie (Gruppeleder)       │   ├── Hans (Gruppeleder)
    │   ├── Lars (Pedagog)            │   ├── Eva (Pedagog)
    │   ├── Ingrid (Pedagog)          │   ├── Knut (Pedagog)
    │   └── Erik (Assistent)          │   └── Pia (Assistent)
    ├── Måne                              ├── Sommerfugl
    │   ├── Sara (Gruppeleder)        │   ├── Kari (Gruppeleder)
    │   ├── Per (Pedagog)             │   ├── Ole (Pedagog)
    │   ├── Emma (Pedagog)            │   ├── Liv (Pedagog)
    │   └── Noah (Assistent)          │   └── Tom (Assistent)
    └── Stjerne                           └── Humle
        ├── Nora (Gruppeleder)              ├── Grete (Gruppeleder)
        ├── Erik (Pedagog)                  ├── Arne (Pedagog)
        ├── Mia (Pedagog)                   ├── Ruth (Pedagog)
        └── Leo (Assistent)                 └── Finn (Assistent)
```
